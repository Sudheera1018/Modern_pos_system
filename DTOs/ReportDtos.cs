using System.ComponentModel.DataAnnotations;

namespace ModernPosSystem.DTOs;

public class ReportFilterDto
{
    [DataType(DataType.Date)]
    public DateTime? FromDate { get; set; }

    [DataType(DataType.Date)]
    public DateTime? ToDate { get; set; }

    public string? CashierUserId { get; set; }

    public string? ProductId { get; set; }
}
