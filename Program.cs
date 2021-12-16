using System;
using System.Text;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Consumer;
using Azure.Messaging.EventHubs.Processor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text.Json.Serialization;
using System.Text.Json;
// See https://aka.ms/new-console-template for more information

//DefaultEndpointsProtocol=https;AccountName=pokerdatabase;AccountKey=f/7COPU+CkzIWEBIEG+c7KtFfbPKLKdCIk2jaRkFu99KFYm/H2eT0cZwqAxwn3A2pJwscXNa1tUdQTCmiJCN1g==;EndpointSuffix=core.windows.net

namespace PokerEventHubReceiver
{
    class Program
    {
        static async Task Main()
        {
            var serviceProvider = new ServiceCollection()
                .AddLogging( configure => configure.AddConsole())
                .AddSingleton<Processor>()
                .BuildServiceProvider();
            
            ILogger<Program> _logger = serviceProvider.GetRequiredService<ILogger<Program>>();
            Processor processor = serviceProvider.GetRequiredService<Processor>();
            
            string ehubNamespaceConnectionString = System.Environment.GetEnvironmentVariable("EVENTHUB_CONNECTION_STRING") ?? "";
            string eventHubName = System.Environment.GetEnvironmentVariable("EVENTHUB_NAME") ?? "";
            string blobStorageConnectionString = System.Environment.GetEnvironmentVariable("BLOB_CONNECTION_STRING") ?? "";
            string blobContainerName = System.Environment.GetEnvironmentVariable("BLOB_CONTAINER_NAME") ?? "";
            string consumerGroup = EventHubConsumerClient.DefaultConsumerGroupName;

            BlobContainerClient storageClient = new BlobContainerClient(blobStorageConnectionString, blobContainerName);

            // Create an event processor client to process events in the event hub
            EventProcessorClient processorClient = new EventProcessorClient(storageClient, consumerGroup, ehubNamespaceConnectionString, eventHubName);
            
            // Register handlers for processing events and handling errors
            processorClient.ProcessEventAsync += Processor.ProcessEventHandler;
            processorClient.ProcessErrorAsync += Processor.ProcessErrorHandler;
            while(true) {
                await processorClient.StartProcessingAsync();
                // Wait for 30 seconds for the events to be processed
                await Task.Delay(TimeSpan.FromSeconds(30));

                // Stop the processing
                await processorClient.StopProcessingAsync();
            }
            
        }
        
        
    }
}
