from __future__ import annotations

from dataclasses import dataclass
from datetime import date, timedelta
from math import cos, pi, sin
from typing import Iterable

import numpy as np
import pandas as pd


@dataclass(slots=True)
class FeaturePayload:
    row: dict[str, float | int]
    warnings: list[str]
    debug: dict[str, float | int | str]


def parse_and_sort_history(sales_history: Iterable[dict | object]) -> pd.Series:
    records: list[tuple[pd.Timestamp, float]] = []
    for point in sales_history:
        point_date = getattr(point, "date", None) or point["date"]
        quantity = getattr(point, "quantity", None)
        if quantity is None:
            quantity = point["quantity"]
        records.append((pd.Timestamp(point_date), float(quantity)))

    frame = pd.DataFrame(records, columns=["date", "quantity"]).sort_values("date")
    frame = frame.groupby("date", as_index=False)["quantity"].sum()
    full_range = pd.date_range(frame["date"].min(), frame["date"].max(), freq="D")
    frame = frame.set_index("date").reindex(full_range, fill_value=0.0)
    return frame["quantity"].astype(float)


def build_base_time_features(forecast_date: date) -> dict[str, float | int]:
    timestamp = pd.Timestamp(forecast_date)
    iso_calendar = timestamp.isocalendar()
    day_of_week = int(timestamp.dayofweek)
    month = int(timestamp.month)
    return {
        "day_of_week": day_of_week,
        "day_of_month": int(timestamp.day),
        "month": month,
        "year": int(timestamp.year),
        "week_of_year": int(iso_calendar.week),
        "quarter": int(timestamp.quarter),
        "is_weekend": int(day_of_week >= 5),
        "is_month_start": int(timestamp.is_month_start),
        "is_month_end": int(timestamp.is_month_end),
        "month_sin": sin((2 * pi * month) / 12),
        "month_cos": cos((2 * pi * month) / 12),
        "day_of_week_sin": sin((2 * pi * day_of_week) / 7),
        "day_of_week_cos": cos((2 * pi * day_of_week) / 7),
    }


def compute_lag_features(history_quantities: pd.Series) -> tuple[dict[str, float], list[str]]:
    warnings: list[str] = []
    if history_quantities.empty:
        return {f"lag_{window}": 0.0 for window in (1, 2, 3, 7, 14, 21, 28)}, ["History was empty, so lag features defaulted to zero."]

    lags: dict[str, float] = {}
    fallback_value = float(history_quantities.iloc[-1])
    average_value = float(history_quantities.mean())
    for window in (1, 2, 3, 7, 14, 21, 28):
        if len(history_quantities) >= window:
            lags[f"lag_{window}"] = float(history_quantities.iloc[-window])
        else:
            lags[f"lag_{window}"] = fallback_value if len(history_quantities) > 0 else average_value
            warnings.append(f"Not enough history for lag_{window}; used fallback quantity {lags[f'lag_{window}']:.2f}.")

    return lags, warnings


def compute_rolling_features(history_quantities: pd.Series) -> tuple[dict[str, float], list[str]]:
    warnings: list[str] = []
    features: dict[str, float] = {}
    for window in (7, 14, 30):
        available = history_quantities.tail(window)
        if available.empty:
            features[f"rolling_mean_{window}"] = 0.0
            features[f"rolling_std_{window}"] = 0.0
            warnings.append(f"No history available for rolling window {window}; defaults were used.")
            continue

        if len(available) < window:
            warnings.append(f"Only {len(available)} days available for rolling window {window}.")

        features[f"rolling_mean_{window}"] = float(available.mean())
        features[f"rolling_std_{window}"] = float(available.std(ddof=0)) if len(available) > 1 else 0.0

    return features, warnings


def compute_expanding_mean(history_quantities: pd.Series) -> float:
    return float(history_quantities.mean()) if not history_quantities.empty else 0.0


def compute_trend_features(lag_values: dict[str, float]) -> dict[str, float]:
    return {
        "trend_1_7": lag_values["lag_1"] - lag_values["lag_7"],
        "trend_7_14": lag_values["lag_7"] - lag_values["lag_14"],
    }


def build_feature_row(
    store_id: int,
    item_id: int,
    forecast_date: date,
    sales_history: Iterable[dict | object],
    feature_columns: list[str],
) -> FeaturePayload:
    """
    Build one feature row for prediction.

    The model was trained on engineered daily-sales features, not raw invoice data. The POS layer must
    aggregate invoice line quantities to daily totals first and then this function recreates the training-time
    feature shape expected by xgboost_feature_columns.pkl.
    """

    series = parse_and_sort_history(sales_history)
    warnings: list[str] = []
    lags, lag_warnings = compute_lag_features(series)
    rolling, rolling_warnings = compute_rolling_features(series)
    warnings.extend(lag_warnings)
    warnings.extend(rolling_warnings)

    base_features = build_base_time_features(forecast_date)
    expanding_mean = compute_expanding_mean(series)
    trend_features = compute_trend_features(lags)
    raw_row: dict[str, float | int] = {
        "store": int(store_id),
        "item": int(item_id),
        **base_features,
        **lags,
        **rolling,
        "expanding_mean": expanding_mean,
        **trend_features,
    }

    row: dict[str, float | int] = {}
    for column in feature_columns:
        row[column] = raw_row.get(column, 0.0)

    debug_payload: dict[str, float | int | str] = {
        "history_days": int(len(series)),
        "latest_history_date": str(series.index.max().date()) if not series.empty else "",
        **{key: row[key] for key in row}
    }
    return FeaturePayload(row=row, warnings=warnings, debug=debug_payload)


def append_prediction(history: pd.Series, future_date: date, predicted_quantity: float) -> pd.Series:
    next_series = history.copy()
    next_series.loc[pd.Timestamp(future_date)] = float(predicted_quantity)
    next_series = next_series.sort_index()
    return next_series
