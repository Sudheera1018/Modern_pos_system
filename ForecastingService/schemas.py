from __future__ import annotations

from datetime import date
from typing import Any

from pydantic import BaseModel, Field, field_validator


class SalesHistoryPoint(BaseModel):
    date: date
    quantity: float = Field(ge=0)


class DailyForecastPoint(BaseModel):
    date: date
    predicted_quantity: float = Field(ge=0)


class HealthResponse(BaseModel):
    status: str
    model_loaded: bool
    feature_columns_loaded: bool


class NextDayPredictionRequest(BaseModel):
    store_id: int = Field(gt=0)
    item_id: int = Field(gt=0)
    forecast_date: date
    sales_history: list[SalesHistoryPoint]
    debug: bool = False

    @field_validator("sales_history")
    @classmethod
    def ensure_history_present(cls, value: list[SalesHistoryPoint]) -> list[SalesHistoryPoint]:
        if not value:
            raise ValueError("sales_history must contain at least one daily record.")
        return value


class NextDayPredictionResponse(BaseModel):
    store_id: int
    item_id: int
    forecast_date: date
    predicted_next_day_sales: float
    input_history_days: int
    confidence_label: str
    warnings: list[str] = []
    features_used: dict[str, Any] | None = None


class Next7DaysPredictionRequest(BaseModel):
    store_id: int = Field(gt=0)
    item_id: int = Field(gt=0)
    start_date: date
    sales_history: list[SalesHistoryPoint]
    debug: bool = False

    @field_validator("sales_history")
    @classmethod
    def ensure_history_present(cls, value: list[SalesHistoryPoint]) -> list[SalesHistoryPoint]:
        if not value:
            raise ValueError("sales_history must contain at least one daily record.")
        return value


class Next7DaysPredictionResponse(BaseModel):
    store_id: int
    item_id: int
    start_date: date
    daily_forecasts: list[DailyForecastPoint]
    predicted_next_7_days_total: float
    input_history_days: int
    confidence_label: str
    warnings: list[str] = []
    debug_rows: list[dict[str, Any]] | None = None


class BulkProductPredictionRequest(BaseModel):
    store_id: int = Field(gt=0)
    item_id: int = Field(gt=0)
    start_date: date
    sales_history: list[SalesHistoryPoint]
    debug: bool = False


class BulkPredictionRequest(BaseModel):
    products: list[BulkProductPredictionRequest]

    @field_validator("products")
    @classmethod
    def ensure_products_present(cls, value: list[BulkProductPredictionRequest]) -> list[BulkProductPredictionRequest]:
        if not value:
            raise ValueError("products must contain at least one forecast request.")
        return value


class BulkProductPredictionResponse(BaseModel):
    store_id: int
    item_id: int
    predicted_next_day_sales: float
    daily_forecasts: list[DailyForecastPoint]
    predicted_next_7_days_total: float
    input_history_days: int
    confidence_label: str
    warnings: list[str] = []


class BulkPredictionResponse(BaseModel):
    results: list[BulkProductPredictionResponse]
