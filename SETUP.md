# Modern POS System Setup Guide

This guide explains how to set up and run the full Modern POS System after cloning it from GitHub. The system has two runtime parts, and they should be started in this order.

| Order | Runtime Part | Purpose | Default URL |
| --- | --- | --- | --- |
| 1 | Python FastAPI ML Forecasting Service | Loads the XGBoost model and serves demand predictions | `http://localhost:8000` |
| 2 | ASP.NET Core MVC POS Web App | Runs login, dashboard, POS billing, inventory, forecasting UI, reports, and admin modules | `http://localhost:5099` |

The POS web app can load without the ML service in some screens, but the Forecasting module needs the FastAPI service to be running. For the complete project demo, always start the ML service first.


## 1. Prerequisites

Install these tools before running the project.

| Tool | Required For | Notes |
| --- | --- | --- |
| Git | Clone the repository | Use Git Bash or PowerShell. |
| .NET SDK | ASP.NET Core MVC app | Use a SDK that can build `net8.0`. |
| Python 3.11 or newer | FastAPI forecasting service | Enable `Add python.exe to PATH` during installation. |
| MongoDB Atlas or local MongoDB | Database | Atlas needs internet access and IP allow-listing. |
| MongoDB Compass | Optional database testing | Useful for checking the connection string. |
| Node.js | Optional diagram export | Only needed if exporting Mermaid diagrams from command line. |

If your machine has a newer .NET SDK only, set `DOTNET_ROLL_FORWARD=Major` before running the ASP.NET app.

## 2. Clone The Project

Use either PowerShell or Git Bash. Replace the folder path if you want to clone somewhere else.

### PowerShell

```powershell
cd "C:\FinalYear Project"
git clone https://github.com/MrAlfaa/modern-pos-system.git
cd "C:\FinalYear Project\modern-pos-system"
```

### Git Bash

```bash
cd "/c/FinalYear Project"
git clone https://github.com/MrAlfaa/modern-pos-system.git
cd "/c/FinalYear Project/modern-pos-system"
```

## 3. Configure Required Settings

Open `appsettings.json` and confirm the MongoDB settings.

```json
"MongoDbSettings": {
  "ConnectionString": "your MongoDB connection string",
  "DatabaseName": "ModernPosResearchDb"
}
```

The app also supports Cloudinary product image upload. Configure Cloudinary through `appsettings.json`, user secrets, or environment variables.

```json
"Cloudinary": {
  "CloudName": "your-cloud-name",
  "ApiKey": "your-api-key",
  "ApiSecret": "your-api-secret",
  "Folder": "modern-pos-system/products"
}
```

Do not commit real MongoDB or Cloudinary secrets to GitHub in a production or shared repository.

## 4. First-Time ML Service Setup

Do this once after cloning. It creates a local Python virtual environment inside `ForecastingService/.venv` and installs the required Python packages there. This does not install packages globally.

### PowerShell

```powershell
cd "C:\FinalYear Project\modern-pos-system\ForecastingService"
python -m venv .venv
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass
.\.venv\Scripts\Activate.ps1
python -m pip install --upgrade pip
pip install -r requirements.txt
```

### Git Bash

```bash
cd "/c/FinalYear Project/modern-pos-system/ForecastingService"
python -m venv .venv
source .venv/Scripts/activate
python -m pip install --upgrade pip
pip install -r requirements.txt
```

If `python` is not found on Windows, install Python 3.11+ and reopen the terminal. On some machines, this command may work instead:

```powershell
py -3.11 -m venv .venv
```

## 5. Run The Full Project

Use two terminals. Keep both terminals open while using the system.

### Terminal 1: Start ML Forecasting Service

Start this first.

#### PowerShell

```powershell
cd "C:\FinalYear Project\modern-pos-system\ForecastingService"
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass
.\.venv\Scripts\Activate.ps1
uvicorn main:app --reload --host 0.0.0.0 --port 8000
```

#### Git Bash

```bash
cd "/c/FinalYear Project/modern-pos-system/ForecastingService"
source .venv/Scripts/activate
uvicorn main:app --reload --host 0.0.0.0 --port 8000
```

Check the ML service in the browser:

```text
http://localhost:8000/health
```

Expected result:

```json
{
  "status": "ok",
  "model_loaded": true,
  "feature_columns_loaded": true
}
```

### Terminal 2: Start ASP.NET Core POS Web App

Start this after Terminal 1 is running.

#### PowerShell

```powershell
cd "C:\FinalYear Project\modern-pos-system"
$env:DOTNET_ROLL_FORWARD="Major"
dotnet restore
dotnet build
dotnet run --no-launch-profile --urls http://localhost:5099
```

#### Git Bash

```bash
cd "/c/FinalYear Project/modern-pos-system"
export DOTNET_ROLL_FORWARD=Major
dotnet restore
dotnet build
dotnet run --no-launch-profile --urls http://localhost:5099
```

Open the POS system:

```text
http://localhost:5099
```

## 6. Quick Daily Run Commands

Use these after the first-time setup is already completed.

### PowerShell

Terminal 1:

```powershell
cd "C:\FinalYear Project\modern-pos-system\ForecastingService"
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass
.\.venv\Scripts\Activate.ps1
uvicorn main:app --reload --host 0.0.0.0 --port 8000
```

Terminal 2:

```powershell
cd "C:\FinalYear Project\modern-pos-system"
$env:DOTNET_ROLL_FORWARD="Major"
dotnet run --no-launch-profile --urls http://localhost:5099
```

### Git Bash

Terminal 1:

```bash
cd "/c/FinalYear Project/modern-pos-system/ForecastingService"
source .venv/Scripts/activate
uvicorn main:app --reload --host 0.0.0.0 --port 8000
```

Terminal 2:

```bash
cd "/c/FinalYear Project/modern-pos-system"
export DOTNET_ROLL_FORWARD=Major
dotnet run --no-launch-profile --urls http://localhost:5099
```

## 7. Login Accounts

The database seeder creates these demo accounts.

| Role | Username | Password | Access |
| --- | --- | --- | --- |
| Admin | `admin` | `Admin@123` | Full system access |
| Manager | `manager` | `Manager@123` | Dashboard, inventory, reports, forecasting |
| Cashier | `cashier` | `Cashier@123` | POS billing, customers, sales history |

## 8. Common Problems And Fixes

### Port 5099 Already In Use

This means another ASP.NET app instance is already running. Stop it or run on a different port.

PowerShell:

```powershell
Get-NetTCPConnection -LocalPort 5099 | Select-Object LocalAddress,LocalPort,State,OwningProcess
Stop-Process -Id <PID> -Force
```

Git Bash:

```bash
netstat -ano | grep 5099
taskkill //PID <PID> //F
```

Alternative port:

```powershell
dotnet run --no-launch-profile --urls http://localhost:5100
```

### Port 8000 Already In Use

This means another Python/FastAPI service is already running.

PowerShell:

```powershell
Get-NetTCPConnection -LocalPort 8000 | Select-Object LocalAddress,LocalPort,State,OwningProcess
Stop-Process -Id <PID> -Force
```

Git Bash:

```bash
netstat -ano | grep 8000
taskkill //PID <PID> //F
```

### Python Was Not Found

Install Python 3.11 or newer and enable `Add python.exe to PATH`. Then close and reopen the terminal.

Check:

```powershell
python --version
```

If the Microsoft Store opens instead of Python, disable the Python app execution alias in Windows settings or reinstall Python from the official installer.

### PowerShell Blocks Virtual Environment Activation

Run this in the same PowerShell terminal before activating the environment.

```powershell
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass
```

Then activate again:

```powershell
.\.venv\Scripts\Activate.ps1
```

### MongoDB Atlas Connection Error

If the ASP.NET app fails during startup or seeding, check these items.

| Check | What To Do |
| --- | --- |
| Internet | Confirm the machine can access the internet. |
| Atlas IP allowlist | Add the machine public IP in MongoDB Atlas Network Access. |
| Username/password | Verify the database username and password in the connection string. |
| Windows time | Correct date/time can affect TLS authentication. |
| Compass test | Test the same connection string in MongoDB Compass. |

For local testing, you can use local MongoDB:

```json
"MongoDbSettings": {
  "ConnectionString": "mongodb://localhost:27017",
  "DatabaseName": "ModernPosResearchDb"
}
```

### Forecasting Page Has No Predictions

Check the ML service first:

```text
http://localhost:8000/health
```

If `model_loaded` or `feature_columns_loaded` is `false`, confirm the model files exist in the expected local folder and that `ForecastingService` can access them.

### Antivirus Blocks ModernPosSystem.exe

Some antivirus tools can block unsigned local debug builds. If this is your trusted local project, add exclusions for:

```text
bin/
obj/
```

Then rebuild:

```powershell
dotnet clean
dotnet build
dotnet run --no-launch-profile --urls http://localhost:5099
```

## 9. Stop The Project

You can stop both running terminals with `Ctrl + C`. If processes remain running, use the commands below.

### PowerShell

```powershell
Get-Process python,dotnet,ModernPosSystem -ErrorAction SilentlyContinue
Stop-Process -Name python,dotnet,ModernPosSystem -Force
```

### Git Bash

```bash
taskkill //IM python.exe //F
taskkill //IM dotnet.exe //F
taskkill //IM ModernPosSystem.exe //F
```

## 10. Recommended Run Checklist

Before a demo or supervisor review, confirm these in order.

| Step | Check |
| --- | --- |
| 1 | MongoDB connection string is correct. |
| 2 | Cloudinary settings are configured if product image upload is required. |
| 3 | Terminal 1 FastAPI service is running on `http://localhost:8000`. |
| 4 | `http://localhost:8000/health` returns model and feature columns loaded. |
| 5 | Terminal 2 ASP.NET app is running on `http://localhost:5099`. |
| 6 | Login works with `admin / Admin@123`. |
| 7 | POS Billing, Products, Dashboard, and Forecasting screens load. |
