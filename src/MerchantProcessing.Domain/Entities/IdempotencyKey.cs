namespace MerchantProcessing.Domain.Entities;

/// <summary>
/// Stores idempotency keys with cached responses
/// </summary>
public class IdempotencyKey
{
    public string Key { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string ResponseCache { get; set; } = string.Empty; // JSON serialized response
    public DateTime ExpiresAt { get; set; }
}
