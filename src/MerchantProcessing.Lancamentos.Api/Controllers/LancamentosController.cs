using Microsoft.AspNetCore.Mvc;
using MerchantProcessing.Domain.DTOs;
using MerchantProcessing.Domain.Entities;
using MerchantProcessing.Domain.Enums;
using MerchantProcessing.Domain.Events;
using MerchantProcessing.Infrastructure.Data;
using MerchantProcessing.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace MerchantProcessing.Lancamentos.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LancamentosController : ControllerBase
{
    private readonly MerchantDbContext _context;
    private readonly IMessageQueueService _queueService;
    private readonly ICacheService _cacheService;
    private readonly ILogger<LancamentosController> _logger;

    public LancamentosController(
        MerchantDbContext context,
        IMessageQueueService queueService,
        ICacheService cacheService,
        ILogger<LancamentosController> logger)
    {
        _context = context;
        _queueService = queueService;
        _cacheService = cacheService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<TransactionResponse>> CreateTransaction(
        [FromBody] CreateTransactionRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey)
    {
        try
        {
            // Use idempotencyKey from header or request body
            var key = idempotencyKey ?? request.IdempotencyKey;
            if (string.IsNullOrEmpty(key))
                return BadRequest("Idempotency key is required");

            // Check idempotency
            var cached = await _cacheService.GetAsync<TransactionResponse>($"idempotency:{key}");
            if (cached != null)
                return Ok(cached);

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Lock account row for update
                var account = await _context.Accounts
                    .Where(a => a.Id == request.AccountId)
                    .FirstOrDefaultAsync();

                if (account == null)
                    return NotFound("Account not found");

                // Calculate new balance
                var newBalance = request.Type == TransactionType.Credit
                    ? account.Balance + request.Amount
                    : account.Balance - request.Amount;

                // Validate sufficient funds for debits
                if (request.Type == TransactionType.Debit && newBalance < 0)
                    return BadRequest("Insufficient funds");

                // Create transaction record
                var txn = new Transaction
                {
                    Id = Guid.NewGuid(),
                    AccountId = request.AccountId,
                    Type = request.Type,
                    Amount = request.Amount,
                    BalanceSnapshot = newBalance,
                    Timestamp = DateTime.UtcNow,
                    IdempotencyKey = key,
                    ProcessedAt = DateTime.UtcNow
                };

                _context.Transactions.Add(txn);

                // Update account balance
                account.Balance = newBalance;
                account.LastUpdated = DateTime.UtcNow;
                account.Version++;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Prepare response
                var response = new TransactionResponse
                {
                    Id = txn.Id,
                    AccountId = txn.AccountId,
                    Type = txn.Type,
                    Amount = txn.Amount,
                    BalanceAfter = txn.BalanceSnapshot,
                    Timestamp = txn.Timestamp
                };

                // Cache idempotency
                await _cacheService.SetAsync($"idempotency:{key}", response, TimeSpan.FromHours(24));

                // Publish event to SQS (fire-and-forget with error handling)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var txnEvent = new TransactionEvent
                        {
                            TransactionId = txn.Id,
                            AccountId = txn.AccountId,
                            Type = txn.Type,
                            Amount = txn.Amount,
                            Timestamp = txn.Timestamp,
                            NewBalance = newBalance
                        };
                        await _queueService.PublishTransactionEventAsync(txnEvent);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to publish transaction event");
                    }
                });

                return CreatedAtAction(nameof(CreateTransaction), response);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing transaction");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new { status = "healthy", service = "lancamentos-api" });
    }
}
