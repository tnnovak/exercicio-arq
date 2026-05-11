using MerchantProcessing.Domain.Enums;

namespace MerchantProcessing.Domain.Entities;

/// <summary>
/// Represents a financial transaction (credit or debit)
/// </summary>
public class Transaction
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public TransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public decimal BalanceSnapshot { get; set; } // Balance after this transaction
    public DateTime Timestamp { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public DateTime ProcessedAt { get; set; }
    
    // Navigation property
    public Account Account { get; set; } = null!;
}
