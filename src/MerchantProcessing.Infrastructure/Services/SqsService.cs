using Amazon.SQS;
using Amazon.SQS.Model;
using System.Text.Json;
using MerchantProcessing.Domain.Events;

namespace MerchantProcessing.Infrastructure.Services;

public interface IMessageQueueService
{
    Task PublishTransactionEventAsync(TransactionEvent transactionEvent, CancellationToken cancellationToken = default);
    Task<List<TransactionEvent>> ReceiveTransactionEventsAsync(int maxMessages = 10, CancellationToken cancellationToken = default);
    Task DeleteMessageAsync(string receiptHandle, CancellationToken cancellationToken = default);
}

public class SqsService : IMessageQueueService
{
    private readonly IAmazonSQS _sqsClient;
    private readonly string _queueUrl;

    public SqsService(IAmazonSQS sqsClient, string queueUrl)
    {
        _sqsClient = sqsClient;
        _queueUrl = queueUrl;
    }

    public async Task PublishTransactionEventAsync(TransactionEvent transactionEvent, CancellationToken cancellationToken = default)
    {
        var messageBody = JsonSerializer.Serialize(transactionEvent);
        var request = new SendMessageRequest
        {
            QueueUrl = _queueUrl,
            MessageBody = messageBody
        };

        await _sqsClient.SendMessageAsync(request, cancellationToken);
    }

    public async Task<List<TransactionEvent>> ReceiveTransactionEventsAsync(int maxMessages = 10, CancellationToken cancellationToken = default)
    {
        var request = new ReceiveMessageRequest
        {
            QueueUrl = _queueUrl,
            MaxNumberOfMessages = maxMessages,
            WaitTimeSeconds = 20 // Long polling
        };

        var response = await _sqsClient.ReceiveMessageAsync(request, cancellationToken);
        var events = new List<TransactionEvent>();

        foreach (var message in response.Messages)
        {
            var transactionEvent = JsonSerializer.Deserialize<TransactionEvent>(message.Body);
            if (transactionEvent != null)
            {
                events.Add(transactionEvent);
            }
        }

        return events;
    }

    public async Task DeleteMessageAsync(string receiptHandle, CancellationToken cancellationToken = default)
    {
        var request = new DeleteMessageRequest
        {
            QueueUrl = _queueUrl,
            ReceiptHandle = receiptHandle
        };

        await _sqsClient.DeleteMessageAsync(request, cancellationToken);
    }
}
