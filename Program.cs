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
        static readonly HttpClient client = new HttpClient();

        static async Task ProcessEventHandler(ProcessEventArgs eventArgs)
        {
            string webhookUrl = System.Environment.GetEnvironmentVariable("WEBHOOK_URL") ?? "";
            string token = Environment.GetEnvironmentVariable("TOKEN") ?? "";
           // Write the body of the event to the console window
            Console.WriteLine("\tReceived event: {0}", Encoding.UTF8.GetString(eventArgs.Data.Body.ToArray()));
            var request = new HttpRequestMessage {
                RequestUri = new Uri(webhookUrl),
                Method = HttpMethod.Post
            };
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/x-www-form-urlencoded"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token); 
            Payload payload = new Payload();
            payload.Text = Encoding.UTF8.GetString(eventArgs.Data.Body.ToArray());
            request.Content = new StringContent(JsonSerializer.Serialize(payload));
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            try
            {
                var responseMessage = await client.SendAsync(request);
                var responseBody = await responseMessage.Content.ReadAsStringAsync();
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine("Error: " + e.Message);
            }
            await eventArgs.UpdateCheckpointAsync(eventArgs.CancellationToken);
        }

        static Task ProcessErrorHandler(ProcessErrorEventArgs eventArgs)
        {
            // Write details about the error to the console window
            Console.WriteLine($"\tPartition '{ eventArgs.PartitionId}': an unhandled exception was encountered. This was not expected to happen.");
            Console.WriteLine(eventArgs.Exception.Message);
            return Task.CompletedTask;
        }
        public class Payload
        {
            //[JsonProperty("token")]
            //public string Token { get; set; }

            [JsonPropertyName("channel")]
            public string Channel { get; set; }
            
            [JsonPropertyName("text")]
            public string Text { get; set; }

            [JsonPropertyName("as_user")]
            public bool As_User { get; set; }
    
        }
    }
}
