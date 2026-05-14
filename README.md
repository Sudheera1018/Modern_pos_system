# Modern POS System

Modern POS System is a retail-ready ASP.NET Core MVC application with MongoDB Atlas, role-based access control, inventory and billing workflows, reporting, and an integrated ML forecasting module powered by a separate Python FastAPI service with an XGBoost model.

## Features

- Modern login screen with cookie authentication
- Role-based access and sidebar permission control
- POS billing and checkout flow
- Product, category, customer, supplier, inventory, purchase, sales, and report modules
- Forecasting dashboard with:
  - next-day sales prediction
  - next-7-days sales prediction
  - reorder suggestion quantity
  - slow-moving item detection
  - automatic daily forecast generation
  - persisted forecast results in MongoDB
- Product image support with:
  - direct image URL fallback
  - Cloudinary upload from the product form
- FastAPI forecasting microservice using:
  - `xgboost_pos_forecasting_model.pkl`
  - `xgboost_feature_columns.pkl`

## Tech Stack

- Frontend: ASP.NET Core MVC, Razor, Bootstrap 5, JavaScript
- Backend: ASP.NET Core 8
- Database: MongoDB Atlas
- ML Service: Python FastAPI
- ML Model: XGBoost

## Project Structure

```text
Modern Pos System/
|-- Controllers/
|-- Models/
|-- Services/
|-- Views/
|-- wwwroot/
|-- ForecastingService/
|-- ML Model/
|-- appsettings.json
|-- Program.cs
```

## Prerequisites

Before running the full project, make sure you have:

- .NET SDK / Runtime that can run `net8.0`
- Python 3.11 or newer
- Internet access for MongoDB Atlas

Cloudinary credentials should be stored in local user secrets or environment variables, not in committed `appsettings.json`.

Important note for this machine: if you only have .NET 10 installed and not .NET 8 runtime, set:

```powershell
$env:DOTNET_ROLL_FORWARD="Major"
```

This allows the app to run even though the project targets `.NET 8`.

## How To Run The Full Project

You need **2 terminals**.

### Terminal 1: Run the Python Forecasting Service

Open a new terminal and navigate to the forecasting service folder:

```powershell
cd "e:\PROJECTS\Out Source Project\Modern Pos System\ForecastingService"
```

Create a local virtual environment:

```powershell
python -m venv .venv
```

Activate the virtual environment:

If you are using **PowerShell**:

```powershell
.\.venv\Scripts\Activate.ps1
```

If PowerShell blocks script execution, run this once in the same terminal:

```powershell
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass
```

Then activate again:

```powershell
.\.venv\Scripts\Activate.ps1
```

Install Python packages inside the virtual environment:

```powershell
pip install -r requirements.txt
```

Then start the FastAPI service from the same terminal:

```powershell
uvicorn main:app --reload --host 0.0.0.0 --port 8000
```

After it starts, the forecasting API will be available at:

```text
http://localhost:8000
```

You can verify it with:

```text
http://localhost:8000/health
```

### Terminal 2: Run the ASP.NET Core POS Web Application

Open a second terminal and navigate to the main project folder:

```powershell
cd "e:\PROJECTS\Out Source Project\Modern Pos System"
```

If your machine only has .NET 10 runtime, run this first in the same terminal:

```powershell
$env:DOTNET_ROLL_FORWARD="Major"
```

Restore packages if needed:

```powershell
dotnet restore
```

Build the project:

```powershell
dotnet build
```

Then run the POS web application:

```powershell
dotnet run --no-launch-profile --urls http://localhost:5099
```

After it starts, open this in your browser:

```text
http://localhost:5099
```

## Login Credentials

Use one of the seeded users:

- `admin / Admin@123`
- `cashier / Cashier@123`
- `manager / Manager@123`

## Full Startup Order

Always start the system in this order:

1. Create and activate the Python virtual environment
2. Start the Python forecasting service first
3. Start the ASP.NET Core POS app second
4. Open the POS in the browser
5. Log in
6. The daily forecast scheduler will run automatically at the configured local time
7. Open the `Forecasting` module any time to review saved forecast results
8. Use `Generate Forecast` only when you want an on-demand refresh

If you close Terminal 1, the virtual environment session and the forecasting service will stop. When reopening it later, you must:

1. Go back to `ForecastingService`
2. Activate `.venv`
3. Run `uvicorn main:app --reload --host 0.0.0.0 --port 8000`

## Exact Full Run Example

### Terminal 1

```powershell
cd "e:\PROJECTS\Out Source Project\Modern Pos System\ForecastingService"
python -m venv .venv
.\.venv\Scripts\Activate.ps1
pip install -r requirements.txt
uvicorn main:app --reload --host 0.0.0.0 --port 8000
```

### Terminal 2

```powershell
cd "e:\PROJECTS\Out Source Project\Modern Pos System"
$env:DOTNET_ROLL_FORWARD="Major"
dotnet restore
dotnet build
dotnet run --no-launch-profile --urls http://localhost:5099
```

After both terminals are running:

1. Open `http://localhost:5099`
2. Log in with a seeded account
3. Open `Forecasting`
4. Review saved results or manually refresh if needed

## Next Time You Run The Project

You do **not** need to create the virtual environment again every time.

Next time, just do this in Terminal 1:

```powershell
cd "e:\PROJECTS\Out Source Project\Modern Pos System\ForecastingService"
.\.venv\Scripts\Activate.ps1
uvicorn main:app --reload --host 0.0.0.0 --port 8000
```

And in Terminal 2:

```powershell
cd "e:\PROJECTS\Out Source Project\Modern Pos System"
$env:DOTNET_ROLL_FORWARD="Major"
dotnet run --no-launch-profile --urls http://localhost:5099
```

## If `python` Does Not Work

If `python` is not recognized in the terminal, install Python 3.11+ first and then reopen the terminal.

You can verify Python with:

```powershell
python --version
```

## If `pip` Does Not Work

After activating `.venv`, `pip` should work automatically. If needed, use:

```powershell
python -m pip install -r requirements.txt
```

## If Activation Fails

Use:

```powershell
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass
.\.venv\Scripts\Activate.ps1
```

This only affects the current terminal window.

## Useful Commands

Run only the web app:

```powershell
dotnet run --no-launch-profile --urls http://localhost:5099
```

Run only the forecasting service after activating `.venv`:

```powershell
uvicorn main:app --reload --host 0.0.0.0 --port 8000
```

Build the web app:

```powershell
dotnet build
```

## Cloudinary Setup

The product form now supports Cloudinary image upload. Configure the credentials locally with user secrets:

```powershell
dotnet user-secrets set "Cloudinary:CloudName" "your-cloud-name"
dotnet user-secrets set "Cloudinary:ApiKey" "your-api-key"
dotnet user-secrets set "Cloudinary:ApiSecret" "your-api-secret"
dotnet user-secrets set "Cloudinary:Folder" "modern-pos-system/products"
```

After that, open `Products > Add Product` or `Edit Product` and upload an image file. If you do not upload a file, the manual `ImageUrl` field still works.

## Troubleshooting

### 1. Forecasting is not working

Check that Terminal 1 is still running:

```text
http://localhost:8000/health
```

If it is not responding, restart the FastAPI service inside the virtual environment.

### 2. Web app starts but forecasting buttons fail

This usually means:

- the Python service is not running
- the virtual environment is not activated and the service was not started
- the model files are missing
- the configured `ForecastApi.BaseUrl` is wrong

### 3. `dotnet run` returns immediately

This usually means:

- the app is already running on the same port
- or the port is already being used by another process

If you see `address already in use`, the fix is usually simple: the POS app is already running in another terminal or background process. Open `http://localhost:5099`, stop the old process, or run a new instance on a different port.

Try a different port:

```powershell
dotnet run --no-launch-profile --urls http://localhost:5100
```

Then open:

```text
http://localhost:5100
```

### 4. Python command not found

Install Python 3.11+ and make sure `python` is available in your terminal.

### 5. Virtual environment already exists

If `.venv` is already there, do not run `python -m venv .venv` again unless you want to recreate it.

Just activate it:

```powershell
.\.venv\Scripts\Activate.ps1
```

## ML Forecasting Notes

The forecasting model does not use raw invoice rows directly. The ASP.NET POS backend first aggregates completed sales into **daily sales history per product**. That daily history is sent to the FastAPI service, which rebuilds the same engineered features used during training.

The XGBoost model predicts `log1p(sales)`, so the FastAPI service converts predictions back using:

```python
predicted_sales = np.expm1(model_output)
```

That final value is what is shown in the Forecasting UI and used for reorder calculations.

The ASP.NET Core application also includes a hosted background scheduler that checks for the configured daily forecast time and automatically generates the current day's forecast for the default store. Manual `Generate Forecast` and `Refresh Forecast` actions still remain available for manager-triggered updates.

## Related Files

- Web app startup: `Program.cs`
- Web app config: `appsettings.json`
- Forecasting service startup: `ForecastingService/main.py`
- Forecasting service docs: `ForecastingService/README.md`
- ML model files:
- `ML Model/xgboost_pos_forecasting_model.pkl`
- `ML Model/xgboost_feature_columns.pkl`

If the Python service is not running, the Forecasting screen will load but forecast generation will fail because the ASP.NET app cannot call the FastAPI service.

## MongoDB Configuration

The application reads MongoDB settings from `appsettings.json`.

Current sections used:

- `MongoDbSettings`
- `ForecastApi`
- `Forecasting`

The forecasting API base URL is currently configured as:

```json
"ForecastApi": {
  "BaseUrl": "http://localhost:8000",
  "TimeoutSeconds": 20
}
```
