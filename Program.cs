using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;

namespace ServiceBusDemo;

/// <summary>
/// Represents an order message that is sent and received through Azure Service Bus.
/// </summary>
public class OrderMessage
{
    /// <summary>
    /// Gets or sets the unique identifier for the order.
    /// </summary>
    public Guid OrderId { get; set; }

    /// <summary>
    /// Gets or sets the name of the customer placing the order.
    /// </summary>
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the total amount of the order.
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Gets or sets the date and time the order was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Responsible for producing and sending messages to an Azure Service Bus queue.
/// </summary>
public class ServiceBusProducer : IAsyncDisposable
{
    private readonly ServiceBusSender _sender;
    private readonly ServiceBusClient _client;

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceBusProducer"/> class.
    /// </summary>
    /// <param name="connectionString">The Azure Service Bus connection string.</param>
    /// <param name="queueName">The name of the queue to send messages to.</param>
    public ServiceBusProducer(string connectionString, string queueName)
    {
        _client = new ServiceBusClient(connectionString);
        _sender = _client.CreateSender(queueName);
    }

    /// <summary>
    /// Sends an order message to the Azure Service Bus queue asynchronously.
    /// </summary>
    /// <param name="order">The order message to send.</param>
    /// <returns>A task that represents the asynchronous send operation.</returns>
    public async Task SendOrderAsync(OrderMessage order)
    {
        var messageBody = JsonSerializer.Serialize(order);
        var message = new ServiceBusMessage(messageBody)
        {
            ContentType = "application/json",
            MessageId = order.OrderId.ToString()
        };

        await _sender.SendMessageAsync(message);
        Console.WriteLine($"[Producer] Message sent successfully. OrderId: {order.OrderId}, Customer: {order.CustomerName}, Total: {order.TotalAmount:C}");
    }

    /// <summary>
    /// Disposes the producer and releases associated resources asynchronously.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        await _sender.DisposeAsync();
        await _client.DisposeAsync();
    }
}

/// <summary>
/// Responsible for consuming and processing messages from an Azure Service Bus queue.
/// </summary>
public class ServiceBusConsumer : IAsyncDisposable
{
    private readonly ServiceBusProcessor _processor;
    private readonly ServiceBusClient _client;

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceBusConsumer"/> class.
    /// </summary>
    /// <param name="connectionString">The Azure Service Bus connection string.</param>
    /// <param name="queueName">The name of the queue to receive messages from.</param>
    public ServiceBusConsumer(string connectionString, string queueName)
    {
        _client = new ServiceBusClient(connectionString);
        _processor = _client.CreateProcessor(queueName, new ServiceBusProcessorOptions
        {
            AutoCompleteMessages = false,
            MaxConcurrentCalls = 1
        });

        _processor.ProcessMessageAsync += ProcessMessageAsync;
        _processor.ProcessErrorAsync += ProcessErrorAsync;
    }

    /// <summary>
    /// Starts the message processor and begins listening for incoming messages.
    /// </summary>
    /// <returns>A task that represents the asynchronous start operation.</returns>
    public async Task StartProcessingAsync()
    {
        await _processor.StartProcessingAsync();
        Console.WriteLine("[Consumer] Message processing has started. Waiting for incoming messages...");
    }

    /// <summary>
    /// Stops the message processor and ceases listening for incoming messages.
    /// </summary>
    /// <returns>A task that represents the asynchronous stop operation.</returns>
    public async Task StopProcessingAsync()
    {
        await _processor.StopProcessingAsync();
        Console.WriteLine("[Consumer] Message processing has been stopped.");
    }

    /// <summary>
    /// Handles the processing of an individual message received from the queue.
    /// </summary>
    /// <param name="args">The event arguments containing the received message.</param>
    private static async Task ProcessMessageAsync(ProcessMessageEventArgs args)
    {
        var body = args.Message.Body.ToString();
        var order = JsonSerializer.Deserialize<OrderMessage>(body);

        if (order is null)
        {
            Console.WriteLine("[Consumer] Warning: Received a message that could not be deserialized. Abandoning message.");
            await args.AbandonMessageAsync(args.Message);
            return;
        }

        Console.WriteLine($"[Consumer] Message received. OrderId: {order.OrderId}, Customer: {order.CustomerName}, Total: {order.TotalAmount:C}, Created: {order.CreatedAt:O}");

        await args.CompleteMessageAsync(args.Message);
        Console.WriteLine($"[Consumer] Message completed and removed from the queue. OrderId: {order.OrderId}");
    }

    /// <summary>
    /// Handles errors that occur during message processing.
    /// </summary>
    /// <param name="args">The event arguments containing error details.</param>
    private static Task ProcessErrorAsync(ProcessErrorEventArgs args)
    {
        Console.WriteLine($"[Consumer] Error encountered while processing message. Source: {args.ErrorSource}, Exception: {args.Exception.Message}");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Disposes the consumer and releases associated resources asynchronously.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        await _processor.DisposeAsync();
        await _client.DisposeAsync();
    }
}

/// <summary>
/// Entry point for the Azure Service Bus demo application.
/// </summary>
public class Program
{
    /// <summary>
    /// Main application method that reads configuration, sends a message, and consumes it.
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    /// <returns>A task that represents the asynchronous application execution.</returns>
    public static async Task Main(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddEnvironmentVariables()
            .AddCommandLine(args)
            .Build();

        var connectionString = configuration["ServiceBus:ConnectionString"];
        var queueName = configuration["ServiceBus:QueueName"];

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("The Service Bus connection string is missing. Please verify the 'ServiceBus:ConnectionString' setting in appsettings.json.");
        }

        if (string.IsNullOrWhiteSpace(queueName))
        {
            throw new InvalidOperationException("The Service Bus queue name is missing. Please verify the 'ServiceBus:QueueName' setting in appsettings.json.");
        }

        var order = new OrderMessage
        {
            OrderId = Guid.NewGuid(),
            CustomerName = "John Doe",
            TotalAmount = 125.75m,
            CreatedAt = DateTime.UtcNow
        };

        await using var producer = new ServiceBusProducer(connectionString, queueName);
        await using var consumer = new ServiceBusConsumer(connectionString, queueName);

        try
        {
            Console.WriteLine("=== Azure Service Bus Demo ===");
            Console.WriteLine($"Queue: {queueName}");
            Console.WriteLine();

            Console.WriteLine("[Producer] Sending order message to the queue...");
            await producer.SendOrderAsync(order);
            Console.WriteLine();

            Console.WriteLine("[Consumer] Starting the consumer to process the message...");
            await consumer.StartProcessingAsync();

            Console.WriteLine("[Main] Waiting for the consumer to process the message. Press any key to stop.");
            Console.ReadKey(true);

            await consumer.StopProcessingAsync();
            Console.WriteLine();
            Console.WriteLine("=== Demo completed successfully ===");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Main] An unexpected error occurred: {ex.Message}");
            Console.WriteLine($"[Main] Stack trace: {ex.StackTrace}");
        }
    }
}