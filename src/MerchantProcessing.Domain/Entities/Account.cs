namespace MerchantProcessing.Domain.Entities;

/// <summary>
/// Represents a merchant account with balance tracking
/// </summary>
public class Account
{
    public Guid Id { get; set; }
    public decimal Balance { get; set; }
    public int Version { get; set; } // For optimistic concurrency
    public DateTime LastUpdated { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // Navigation property
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
