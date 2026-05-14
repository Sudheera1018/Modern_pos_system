from __future__ import annotations

import logging
from dataclasses import dataclass
from datetime import date, timedelta
from pathlib import Path
from typing import Any

import joblib
import numpy as np
import pandas as pd

from feature_engineering import append_prediction, build_feature_row, parse_and_sort_history
from schemas import (
    BulkProductPredictionRequest,
    BulkProductPredictionResponse,
    DailyForecastPoint,
    Next7DaysPredictionRequest,
    Next7DaysPredictionResponse,
    NextDayPredictionRequest,
    NextDayPredictionResponse,
)
from utils import get_confidence_label


logger = logging.getLogger(__name__)


@dataclass(slots=True)
class ModelArtifacts:
    model: Any
    feature_columns: list[str]


class ForecastPredictor:
    """
    Loads the trained XGBoost regressor and its feature-column contract once at startup.

    xgboost_pos_forecasting_model.pkl stores the learned regression model.
    xgboost_feature_columns.pkl stores the exact input feature names and ordering used during training.
    The prediction DataFrame must match that list exactly before calling model.predict().
    """

    def __init__(self, model_path: Path, feature_columns_path: Path) -> None:
        self._artifacts = self._load_artifacts(model_path, feature_columns_path)

    @property
    def feature_columns(self) -> list[str]:
        return self._artifacts.feature_columns

    @property
    def model_loaded(self) -> bool:
        return self._artifacts.model is not None

    @property
    def feature_columns_loaded(self) -> bool:
        return bool(self._artifacts.feature_columns)

    def predict_next_day(self, request: NextDayPredictionRequest) -> NextDayPredictionResponse:
        feature_payload = build_feature_row(
            store_id=request.store_id,
            item_id=request.item_id,
            forecast_date=request.forecast_date,
            sales_history=request.sales_history,
            feature_columns=self.feature_columns,
        )
        predicted_sales = self._predict_from_row(feature_payload.row)
        history_days = len(parse_and_sort_history(request.sales_history))
        return NextDayPredictionResponse(
            store_id=request.store_id,
            item_id=request.item_id,
            forecast_date=request.forecast_date,
            predicted_next_day_sales=predicted_sales,
            input_history_days=history_days,
            confidence_label=get_confidence_label(history_days),
            warnings=feature_payload.warnings,
            features_used=feature_payload.row if request.debug else None,
        )

    def predict_next_7_days(self, request: Next7DaysPredictionRequest) -> Next7DaysPredictionResponse:
        history_series = parse_and_sort_history(request.sales_history)
        history_days = len(history_series)
        daily_forecasts: list[DailyForecastPoint] = []
        warnings: list[str] = []
        debug_rows: list[dict[str, Any]] = []

        for day_offset in range(7):
            current_date = request.start_date + timedelta(days=day_offset)
            feature_payload = build_feature_row(
                store_id=request.store_id,
                item_id=request.item_id,
                forecast_date=current_date,
                sales_history=[
                    {"date": index.date(), "quantity": quantity}
                    for index, quantity in history_series.items()
                ],
                feature_columns=self.feature_columns,
            )
            predicted_sales = self._predict_from_row(feature_payload.row)
            history_series = append_prediction(history_series, current_date, predicted_sales)
            daily_forecasts.append(DailyForecastPoint(date=current_date, predicted_quantity=predicted_sales))
            warnings.extend(feature_payload.warnings)
            if request.debug:
                debug_rows.append({"forecast_date": str(current_date), "features": feature_payload.row})

        return Next7DaysPredictionResponse(
            store_id=request.store_id,
            item_id=request.item_id,
            start_date=request.start_date,
            daily_forecasts=daily_forecasts,
            predicted_next_7_days_total=round(sum(point.predicted_quantity for point in daily_forecasts), 2),
            input_history_days=history_days,
            confidence_label=get_confidence_label(history_days),
            warnings=list(dict.fromkeys(warnings)),
            debug_rows=debug_rows if request.debug else None,
        )

    def predict_bulk(self, products: list[BulkProductPredictionRequest]) -> list[BulkProductPredictionResponse]:
        results: list[BulkProductPredictionResponse] = []
        for product in products:
            next_day = self.predict_next_day(
                NextDayPredictionRequest(
                    store_id=product.store_id,
                    item_id=product.item_id,
                    forecast_date=product.start_date,
                    sales_history=product.sales_history,
                    debug=product.debug,
                )
            )
            next_week = self.predict_next_7_days(
                Next7DaysPredictionRequest(
                    store_id=product.store_id,
                    item_id=product.item_id,
                    start_date=product.start_date,
                    sales_history=product.sales_history,
                    debug=product.debug,
                )
            )
            results.append(BulkProductPredictionResponse(
                store_id=product.store_id,
                item_id=product.item_id,
                predicted_next_day_sales=next_day.predicted_next_day_sales,
                daily_forecasts=next_week.daily_forecasts,
                predicted_next_7_days_total=next_week.predicted_next_7_days_total,
                input_history_days=next_week.input_history_days,
                confidence_label=next_week.confidence_label,
                warnings=list(dict.fromkeys(next_day.warnings + next_week.warnings)),
            ))
        return results

    def _predict_from_row(self, row: dict[str, float | int]) -> float:
        frame = pd.DataFrame([row], columns=self.feature_columns)
        predicted_log_sales = float(self._artifacts.model.predict(frame)[0])
        predicted_sales = max(0.0, float(np.expm1(predicted_log_sales)))
        return round(predicted_sales, 2)

    @staticmethod
    def _load_artifacts(model_path: Path, feature_columns_path: Path) -> ModelArtifacts:
        if not model_path.exists():
            raise FileNotFoundError(f"Model file was not found: {model_path}")
        if not feature_columns_path.exists():
            raise FileNotFoundError(f"Feature-column file was not found: {feature_columns_path}")

        logger.info("Loading trained XGBoost model from %s", model_path)
        model = joblib.load(model_path)
        logger.info("Loading training feature columns from %s", feature_columns_path)
        feature_columns = joblib.load(feature_columns_path)
        if not isinstance(feature_columns, (list, tuple)):
            raise ValueError("Feature-column artifact must contain a list-like sequence of column names.")
        return ModelArtifacts(model=model, feature_columns=[str(column) for column in feature_columns])
