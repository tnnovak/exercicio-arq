using MerchantProcessing.Infrastructure.Data;
using MerchantProcessing.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();
