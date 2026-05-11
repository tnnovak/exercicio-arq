using MerchantProcessing.Infrastructure.Data;
using MerchantProcessing.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using MerchantProcessing.Domain.Entities;

namespace MerchantProcessing.Consolidator.Worker;

public class ConsolidatorWorker : BackgroundService
{
    private readonly ILogger<ConsolidatorWorker> _logger;
    private readonly IServiceProvider _serviceProvider;

    public ConsolidatorWorker(ILogger<ConsolidatorWorker> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Consolidator Worker starting...");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var queueService = scope.ServiceProvider.GetRequiredService<IMessageQueueService>();
                var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();
                var context = scope.ServiceProvider.GetRequiredService<MerchantDbContext>();

                // Receive messages from SQS
                var events = await queueService.ReceiveTransactionEventsAsync(10, stoppingToken);

                foreach (var txnEvent in events)
                {
                    _logger.LogInformation("Processing transaction event: {TransactionId}", txnEvent.TransactionId);

                    // Update cache with new balance
                    var accountCacheKey = $"account:{txnEvent.AccountId}:balance";
                    await cacheService.SetAsync(accountCacheKey, new
                    {
                        AccountId = txnEvent.AccountId,
                        Balance = txnEvent.NewBalance,
                        LastUpdated = txnEvent.Timestamp
                    }, TimeSpan.FromSeconds(60));

                    // TODO: Implement daily consolidation logic
                    // This would aggregate transactions and update ConsolidatedDaily table
                }

                // Wait between polling cycles
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing messages");
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }

        _logger.LogInformation("Consolidator Worker stopping...");
    }
}
