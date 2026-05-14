# Modern POS Forecasting Service

This FastAPI microservice is the ML forecasting layer for the Modern POS system. It loads a trained XGBoost regression model and the exact training feature-column contract from PKL files, recreates the engineered daily-sales features used during training, and returns business-friendly demand predictions as JSON.

The file `xgboost_pos_forecasting_model.pkl` contains the trained XGBoost model. It learned from historical daily retail sales using engineered features such as time features, lag features, rolling statistics, expanding mean, and trend values. The model target was `log1p(sales)`, so the raw output must always be converted back with `expm1` before the POS uses it in reorder calculations.

The file `xgboost_feature_columns.pkl` contains the exact feature names and order used during model training. This file is critical because prediction input must match the same column order the model saw during training. The service treats that PKL file as the source of truth and always builds the prediction DataFrame using that ordered column list.

The POS system should not send raw invoice lines directly to the model. Invoice data must first be aggregated into daily product sales history, for example:

```json
[
  { "date": "2026-03-01", "quantity": 12 },
  { "date": "2026-03-02", "quantity": 15 },
  { "date": "2026-03-03", "quantity": 10 }
]
```

The service recreates engineered features from that daily history, predicts next-day demand, or iteratively predicts the next 7 days by appending each predicted day back into the working history.

## Local setup

Create a local virtual environment:

```bash
python -m venv .venv
```

Activate the virtual environment.

PowerShell:

```powershell
.\.venv\Scripts\Activate.ps1
```

If PowerShell blocks activation:

```powershell
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass
.\.venv\Scripts\Activate.ps1
```

Install dependencies inside `.venv`:

```bash
pip install -r requirements.txt
```

Run locally from the `ForecastingService` folder:

```bash
uvicorn main:app --reload --host 0.0.0.0 --port 8000
```

Next time you run the service, you only need:

```powershell
cd "e:\PROJECTS\Out Source Project\Modern Pos System\ForecastingService"
.\.venv\Scripts\Activate.ps1
uvicorn main:app --reload --host 0.0.0.0 --port 8000
```

Optional environment variables:

```bash
MODEL_PATH=../ML Model/xgboost_pos_forecasting_model.pkl
FEATURE_COLUMNS_PATH=../ML Model/xgboost_feature_columns.pkl
HOST=0.0.0.0
PORT=8000
LOG_LEVEL=info
```

## Endpoints

### `GET /health`

Returns whether the model and feature columns were loaded successfully.

### `POST /predict/next-day`

This endpoint predicts the next day for a single store and item.

```bash
curl -X POST http://localhost:8000/predict/next-day \
  -H "Content-Type: application/json" \
  -d '{
    "store_id": 1,
    "item_id": 1,
    "forecast_date": "2026-03-20",
    "sales_history": [
      { "date": "2026-03-10", "quantity": 11 },
      { "date": "2026-03-11", "quantity": 14 },
      { "date": "2026-03-12", "quantity": 12 }
    ]
  }'
```

### `POST /predict/next-7-days`

This endpoint predicts the next seven days iteratively. Day 1 is predicted from actual history, appended into the series, then Day 2 is predicted using that updated history, and so on through Day 7.

### `POST /predict/bulk`

This endpoint accepts multiple products in one request and returns a per-product response containing the next-day prediction, next-7-days daily forecast, total next-7-days demand, confidence label, and any warnings generated from short history.

## Confidence label

The service returns a simple confidence label based on how many days of history were available:

- `High`: 60 or more days
- `Medium`: 30 to 59 days
- `Low`: fewer than 30 days

## Important implementation notes

The model was trained for daily retail sales forecasting, so this service expects engineered features rather than raw transactional records. Historical sales must be aggregated daily before being sent from the POS backend. Feature ordering must match the saved training columns exactly. Model output is log-scale and must be transformed back with `expm1`. The next-7-days forecast is intentionally iterative so lag and rolling features evolve as each predicted day is appended to history.
