namespace ModernPosSystem.Models;

public enum PaymentMethod
{
    Cash = 1,
    Card = 2,
    Split = 3,
    FundTransfer = 4
}

public enum StockMovementType
{
    Purchase = 1,
    Sale = 2,
    AdjustmentIn = 3,
    AdjustmentOut = 4
}

public enum ForecastPeriod
{
    NextDay = 1,
    NextWeek = 2
}

public enum RiskLevel
{
    Low = 1,
    Medium = 2,
    High = 3
}

public enum ReorderStatus
{
    Healthy = 1,
    ReorderNow = 2,
    Critical = 3
}
