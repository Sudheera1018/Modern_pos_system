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

