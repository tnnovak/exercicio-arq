namespace MerchantProcessing.Domain.Entities;

/// <summary>
/// Materialized view for daily consolidated report
/// </summary>
public class ConsolidatedDaily
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public DateTime Date { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal TotalCredits { get; set; }
    public decimal TotalDebits { get; set; }
    public decimal ClosingBalance { get; set; }
    public int TransactionCount { get; set; }
    public DateTime GeneratedAt { get; set; }
    
    // Navigation property
    public Account Account { get; set; } = null!;
}
