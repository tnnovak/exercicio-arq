using MerchantProcessing.Domain.Entities;
using MerchantProcessing.Infrastructure.Data;
using MerchantProcessing.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MerchantProcessing.Consolidator.Worker.Jobs;

public class DailyConsolidationJob
{
    private readonly ILogger<DailyConsolidationJob> _logger;
    private readonly MerchantDbContext _context;
    private readonly ICacheService _cacheService;

    public DailyConsolidationJob(
        ILogger<DailyConsolidationJob> logger,
        MerchantDbContext context,
        ICacheService cacheService)
    {
        _logger = logger;
        _context = context;
        _cacheService = cacheService;
    }

    public async Task ExecuteAsync()
    {
        var yesterday = DateTime.UtcNow.Date.AddDays(-1);
        _logger.LogInformation("Starting daily consolidation for date: {Date}", yesterday);

        try
        {
            // Get all accounts that had transactions yesterday
            var accountsWithTransactions = await _context.Transactions
                .Where(t => t.Timestamp >= yesterday && t.Timestamp < yesterday.AddDays(1))
                .Select(t => t.AccountId)
                .Distinct()
                .ToListAsync();

            _logger.LogInformation("Found {Count} accounts with transactions", accountsWithTransactions.Count);

            foreach (var accountId in accountsWithTransactions)
            {
                await ConsolidateAccountDailyAsync(accountId, yesterday);
            }

            _logger.LogInformation("Daily consolidation completed successfully for {Date}", yesterday);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during daily consolidation for {Date}", yesterday);
            throw;
        }
    }

    private async Task ConsolidateAccountDailyAsync(Guid accountId, DateTime date)
    {
        try
        {
            // Check if consolidation already exists (idempotency)
            var existingConsolidation = await _context.ConsolidatedDaily
                .FirstOrDefaultAsync(c => c.AccountId == accountId && c.Date == date);

            if (existingConsolidation != null)
            {
                _logger.LogWarning("Consolidation already exists for account {AccountId} on {Date}", accountId, date);
                return;
            }

            // Get all transactions for this account on this day
            var transactions = await _context.Transactions
                .Where(t => t.AccountId == accountId &&
                           t.Timestamp >= date &&
                           t.Timestamp < date.AddDays(1))
                .OrderBy(t => t.Timestamp)
                .ToListAsync();

            if (!transactions.Any())
            {
                _logger.LogWarning("No transactions found for account {AccountId} on {Date}", accountId, date);
                return;
            }

            // Calculate totals
            var totalCredits = transactions
                .Where(t => t.Type == MerchantProcessing.Domain.Enums.TransactionType.Credit)
                .Sum(t => t.Amount);

            var totalDebits = transactions
                .Where(t => t.Type == MerchantProcessing.Domain.Enums.TransactionType.Debit)
                .Sum(t => t.Amount);

            var transactionCount = transactions.Count;

            // Get final balance (from the last transaction of the day)
            var closingBalance = transactions.Last().BalanceSnapshot;

            // Get opening balance (before first transaction)
            var firstTransaction = transactions.First();
            var openingBalance = firstTransaction.Type == MerchantProcessing.Domain.Enums.TransactionType.Credit
                ? firstTransaction.BalanceSnapshot - firstTransaction.Amount
                : firstTransaction.BalanceSnapshot + firstTransaction.Amount;

            // Create consolidated record
            var consolidated = new ConsolidatedDaily
            {
                AccountId = accountId,
                Date = date,
                OpeningBalance = openingBalance,
                ClosingBalance = closingBalance,
                TotalCredits = totalCredits,
                TotalDebits = totalDebits,
                TransactionCount = transactionCount,
                GeneratedAt = DateTime.UtcNow
            };

            _context.ConsolidatedDaily.Add(consolidated);
            await _context.SaveChangesAsync();

            // Invalidate cache for this date
            var cacheKey = $"consolidated:{accountId}:{date:yyyy-MM-dd}";
            await _cacheService.RemoveAsync(cacheKey);

            _logger.LogInformation(
                "Consolidated account {AccountId} for {Date}: Credits={Credits}, Debits={Debits}, Count={Count}, Balance={Balance}",
                accountId, date, totalCredits, totalDebits, transactionCount, closingBalance);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error consolidating account {AccountId} for {Date}", accountId, date);
            throw;
        }
    }
}
