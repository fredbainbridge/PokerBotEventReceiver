using System;
using System.Text;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Consumer;
using Azure.Messaging.EventHubs.Processor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
// See https://aka.ms/new-console-template for more information

//DefaultEndpointsProtocol=https;AccountName=pokerdatabase;AccountKey=f/7COPU+CkzIWEBIEG+c7KtFfbPKLKdCIk2jaRkFu99KFYm/H2eT0cZwqAxwn3A2pJwscXNa1tUdQTCmiJCN1g==;EndpointSuffix=core.windows.net

namespace PokerEventHubReceiver
{
    class Program
    {
        static async Task Main()
        {
            string ehubNamespaceConnectionString = System.Environment.GetEnvironmentVariable("EVENTHUB_CONNECTION_STRING") ?? "";
            string eventHubName = System.Environment.GetEnvironmentVariable("EVENTHUB_NAME") ?? "";
            string blobStorageConnectionString = System.Environment.GetEnvironmentVariable("BLOB_CONNECTION_STRING") ?? "";
            string blobContainerName = System.Environment.GetEnvironmentVariable("BLOB_CONTAINER_NAME") ?? "";

           
            string consumerGroup = EventHubConsumerClient.DefaultConsumerGroupName;
            BlobContainerClient storageClient = new BlobContainerClient(blobStorageConnectionString, blobContainerName);

            // Create an event processor client to process events in the event hub
            EventProcessorClient processor = new EventProcessorClient(storageClient, consumerGroup, ehubNamespaceConnectionString, eventHubName);

            // Register handlers for processing events and handling errors
            processor.ProcessEventAsync += ProcessEventHandler;
            processor.ProcessErrorAsync += ProcessErrorHandler;

            // Start the processing
            await processor.StartProcessingAsync();

            // Wait for 30 seconds for the events to be processed
            await Task.Delay(TimeSpan.FromSeconds(30));

            // Stop the processing
            await processor.StopProcessingAsync();
        }

        static async Task ProcessEventHandler(ProcessEventArgs eventArgs)
        {
            // Write the body of the event to the console window
            Console.WriteLine("\tReceived event: {0}", Encoding.UTF8.GetString(eventArgs.Data.Body.ToArray()));

            // Update checkpoint in the blob storage so that the app receives only new events the next time it's run
            await eventArgs.UpdateCheckpointAsync(eventArgs.CancellationToken);
        }

        static Task ProcessErrorHandler(ProcessErrorEventArgs eventArgs)
        {
            // Write details about the error to the console window
            Console.WriteLine($"\tPartition '{ eventArgs.PartitionId}': an unhandled exception was encountered. This was not expected to happen.");
            Console.WriteLine(eventArgs.Exception.Message);
            return Task.CompletedTask;
        }
    }
}
