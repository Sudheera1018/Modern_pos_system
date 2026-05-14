from __future__ import annotations

import os
from dataclasses import dataclass
from pathlib import Path


@dataclass(slots=True)
class Settings:
    """Runtime configuration for the FastAPI forecasting service."""

    model_path: Path
    feature_columns_path: Path
    host: str
    port: int
    log_level: str


def get_settings() -> Settings:
    root = Path(__file__).resolve().parent
    return Settings(
        model_path=Path(os.getenv("MODEL_PATH", root.parent / "ML Model" / "xgboost_pos_forecasting_model.pkl")),
        feature_columns_path=Path(os.getenv("FEATURE_COLUMNS_PATH", root.parent / "ML Model" / "xgboost_feature_columns.pkl")),
        host=os.getenv("HOST", "0.0.0.0"),
        port=int(os.getenv("PORT", "8000")),
        log_level=os.getenv("LOG_LEVEL", "info"),
    )
