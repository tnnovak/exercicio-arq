using MerchantProcessing.Consolidator.Worker;
using MerchantProcessing.Consolidator.Worker.Jobs;
using MerchantProcessing.Infrastructure.Data;
using MerchantProcessing.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Hangfire;
using Hangfire.PostgreSql;

var builder = Host.CreateApplicationBuilder(args);

// Database configuration
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Host=localhost;Database=merchantdb;Username=merchant;Password=merchant123";

builder.Services.AddDbContext<MerchantDbContext>(options =>
    options.UseNpgsql(connectionString));

// Redis Cache configuration
var redisConnection = builder.Configuration.GetValue<string>("Redis:ConnectionString") 
    ?? "localhost:6379";

builder.Services.AddSingleton<StackExchange.Redis.IConnectionMultiplexer>(
    StackExchange.Redis.ConnectionMultiplexer.Connect(redisConnection));

builder.Services.AddSingleton<ICacheService, RedisCacheService>();

// AWS SQS configuration
var sqsQueueUrl = builder.Configuration.GetValue<string>("AWS:SQS:QueueUrl") 
    ?? "http://localhost:4566/000000000000/transaction-events";
var sqsServiceUrl = builder.Configuration.GetValue<string>("AWS:SQS:ServiceUrl") 
    ?? "http://localhost:4566";

builder.Services.AddSingleton<Amazon.SQS.IAmazonSQS>(sp =>
{
    var config = new Amazon.SQS.AmazonSQSConfig
    {
        ServiceURL = sqsServiceUrl
    };
    return new Amazon.SQS.AmazonSQSClient(config);
});

builder.Services.AddSingleton<IMessageQueueService>(sp =>
    new SqsService(sp.GetRequiredService<Amazon.SQS.IAmazonSQS>(), sqsQueueUrl));

// Hangfire configuration
builder.Services.AddHangfire(config =>
{
    config.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
          .UseSimpleAssemblyNameTypeSerializer()
          .UseRecommendedSerializerSettings()
          .UsePostgreSqlStorage(options =>
          {
              options.UseNpgsqlConnection(connectionString);
          });
});

builder.Services.AddHangfireServer(options =>
{
    options.WorkerCount = 1; // Single worker for lightweight jobs
    options.ServerName = "ConsolidatorWorker";
});

// Register jobs
builder.Services.AddScoped<DailyConsolidationJob>();

// Background services
builder.Services.AddHostedService<ConsolidatorWorker>();

var host = builder.Build();

// Schedule recurring jobs
using (var scope = host.Services.CreateScope())
{
    var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();
    
    // Schedule daily consolidation at 23:59 UTC every day
    recurringJobManager.AddOrUpdate<DailyConsolidationJob>(
        "daily-consolidation",
        job => job.ExecuteAsync(),
        "59 23 * * *", // CRON: At 23:59 every day
        new RecurringJobOptions
        {
            TimeZone = TimeZoneInfo.Utc
        });
    
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Hangfire job scheduled: daily-consolidation at 23:59 UTC");
}

host.Run();
