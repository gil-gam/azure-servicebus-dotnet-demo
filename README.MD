# AZURE SERVICE BUS DEMO WITH .NET 10.0
## Asynchronous Messaging and Decoupled Architecture Implementation

## 1. Project Overview
This repository contains a high-performance .NET 10.0 Console Application designed to
demonstrate the implementation of asynchronous messaging patterns using Azure
Service Bus. The project showcases a robust integration between a Producer and a
Consumer, utilizing the latest cloud-native practices for message brokering.
The primary goal of this application is to illustrate how to build scalable, resilient, and
decoupled systems where components communicate through message queues rather
than direct API calls, ensuring high availability and fault tolerance.

## 2. Key Features
Decoupled Architecture: Complete separation between the message sender
(Producer) and the message receiver (Consumer).
Asynchronous Processing: Full implementation of the async/await pattern to
ensure non-blocking I/O operations.
Robust Configuration Pipeline: Multi-source configuration support including
appsettings.json , Environment Variables, and Command-Line Interface (CLI)
arguments.
Advanced Resource Management: Utilization of IAsyncDisposable and await
using blocks to ensure deterministic disposal of Service Bus clients and processors.
JSON Serialization: Seamless object-to-message mapping using
System.Text.Json .

## 3. Architecture & Design
The application is built following the Single Responsibility Principle (SRP), featuring
dedicated classes for different roles in the messaging lifecycle:
ServiceBusProducer: Encapsulates the logic for connecting to the namespace,
serializing the OrderMessage DTO, and dispatching it to the queue.
ServiceBusConsumer: Manages the message processor, handles concurrent calls,
and implements the "PeekLock" pattern for reliable message processing.
Modern SDK: Built with the Azure.Messaging.ServiceBus library, leveraging
modern features like the ServiceBusProcessor for automated lock renewal and
error handling.

## 4. Security Best Practices
As a project developed with a CyberSecurity mindset, several protection layers were
implemented:
Secret Management: Sensitive credentials (Connection Strings) are never
hardcoded. The application utilizes a hierarchical configuration system.
Credential Protection: A strict .gitignore policy is enforced to prevent
appsettings.json and other sensitive files from being committed to version control.

Production Readiness: While this demo uses configuration files for simplicity, the
architecture is designed to be easily upgraded to Azure Key Vault or Managed
Identities (RBAC) for passwordless authentication in production environments.
Input Validation: Strict validation of configuration strings at startup to prevent
runtime failures (Fail-Fast principle).

## 5. Prerequisites
To run this project locally, you will need:
.NET 10.0 SDK or later.
An active Azure Subscription.
An Azure Service Bus Namespace with a configured Queue (e.g., orders-queue ).

## 6. Setup & Configuration

**1. Clone the repository:**

git clone https://github.com/gil-gam/azure-servicebus-dotnet-demo.git

cd azure-servicebus-dotnet-demo

**2. Configure Settings:**

Locate the appsettings.example.json file and create a copy named
appsettings.json.

Fill in your Azure credentials:


``` json
{
"ServiceBus": {<br/>
"ConnectionString": "Endpoint=sb://your-namespace.servicebus.windows.net/;SharedA"
"QueueName": "orders-queue"
}
}
``` 
**Warning:** Never commit your appsettings.json file to a public repository. Ensure it is
listed in your .gitignore .

## 7. How to Run
Restore the dependencies and execute the application using the .NET CLI:
dotnet build
dotnet run
The console will display the Producer sending an order and the Consumer subsequently
receiving and completing the message.

## 8. Author Information
Gilberto Andreatta, Senior Software Engineer
Focused on building secure, scalable, and high-performance distributed
systems for the global market.

## 9. License
This project is licensed under the MIT License, which is a permissive license that allows for reuse and modification, making it ideal for portfolio projects. This choice reflects a commitment to open-source collaboration.
