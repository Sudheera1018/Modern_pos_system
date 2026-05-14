from __future__ import annotations

import logging

from fastapi import FastAPI, HTTPException
from fastapi.responses import JSONResponse
import uvicorn

from config import get_settings
from predictor import ForecastPredictor
from schemas import (
    BulkPredictionRequest,
    BulkPredictionResponse,
    HealthResponse,
    Next7DaysPredictionRequest,
    Next7DaysPredictionResponse,
    NextDayPredictionRequest,
    NextDayPredictionResponse,
)
from utils import configure_logging


settings = get_settings()
configure_logging(settings.log_level)
logger = logging.getLogger("forecasting-service")

app = FastAPI(
    title="Modern POS Forecasting Service",
    version="1.0.0",
    description="FastAPI microservice that loads a trained XGBoost model and recreates the feature engineering used during training for POS demand forecasting.",
)


@app.on_event("startup")
def startup() -> None:
    try:
        app.state.predictor = ForecastPredictor(settings.model_path, settings.feature_columns_path)
        logger.info("Forecasting service started successfully.")
    except Exception as exc:  # pragma: no cover - startup failure path
        logger.exception("Unable to start forecasting service.")
        raise RuntimeError(str(exc)) from exc


@app.exception_handler(FileNotFoundError)
async def file_not_found_handler(_, exc: FileNotFoundError) -> JSONResponse:
    return JSONResponse(status_code=500, content={"detail": str(exc)})


@app.exception_handler(ValueError)
async def value_error_handler(_, exc: ValueError) -> JSONResponse:
    return JSONResponse(status_code=400, content={"detail": str(exc)})


@app.get("/health", response_model=HealthResponse)
def health() -> HealthResponse:
    predictor = app.state.predictor
    return HealthResponse(
        status="ok",
        model_loaded=predictor.model_loaded,
        feature_columns_loaded=predictor.feature_columns_loaded,
    )


@app.post("/predict/next-day", response_model=NextDayPredictionResponse)
def predict_next_day(request: NextDayPredictionRequest) -> NextDayPredictionResponse:
    try:
        return app.state.predictor.predict_next_day(request)
    except Exception as exc:
        logger.exception("Next-day prediction failed.")
        raise HTTPException(status_code=500, detail=f"Next-day prediction failed: {exc}") from exc


@app.post("/predict/next-7-days", response_model=Next7DaysPredictionResponse)
def predict_next_7_days(request: Next7DaysPredictionRequest) -> Next7DaysPredictionResponse:
    try:
        return app.state.predictor.predict_next_7_days(request)
    except Exception as exc:
        logger.exception("Next-7-days prediction failed.")
        raise HTTPException(status_code=500, detail=f"Next-7-days prediction failed: {exc}") from exc


@app.post("/predict/bulk", response_model=BulkPredictionResponse)
def predict_bulk(request: BulkPredictionRequest) -> BulkPredictionResponse:
    try:
        return BulkPredictionResponse(results=app.state.predictor.predict_bulk(request.products))
    except Exception as exc:
        logger.exception("Bulk prediction failed.")
        raise HTTPException(status_code=500, detail=f"Bulk prediction failed: {exc}") from exc


if __name__ == "__main__":  # pragma: no cover
    uvicorn.run("main:app", host=settings.host, port=settings.port, reload=True)
