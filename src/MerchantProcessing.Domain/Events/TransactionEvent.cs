using MerchantProcessing.Domain.Enums;

namespace MerchantProcessing.Domain.Events;

/// <summary>
/// Event published to SQS after a transaction is processed
/// </summary>
public class TransactionEvent
{
    public Guid TransactionId { get; set; }
    public Guid AccountId { get; set; }
    public TransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public DateTime Timestamp { get; set; }
    public decimal NewBalance { get; set; }
}
