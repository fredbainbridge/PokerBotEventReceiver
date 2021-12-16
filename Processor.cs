using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.Messaging.EventHubs.Processor;
using Microsoft.Extensions.Logging;

namespace PokerEventHubReceiver
{
    public interface IProcessor {
        
    }
    public class Processor : IProcessor {
        static readonly HttpClient client = new HttpClient();
        private static ILogger<Processor> _logger;
        public Processor(ILogger<Processor> logger) {
            _logger = logger;
        }
        public static async Task ProcessEventHandler(ProcessEventArgs eventArgs)
        {
            string webhookUrl = System.Environment.GetEnvironmentVariable("EVENT_WEBHOOK_URL") ?? "";
            string token = Environment.GetEnvironmentVariable("TOKEN") ?? "";
            _logger.LogInformation("\tReceived event: {0}", Encoding.UTF8.GetString(eventArgs.Data.Body.ToArray()));
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
                _logger.LogInformation("Response Body: \n" + responseBody);
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine("Error: " + e.Message);
            }
            await eventArgs.UpdateCheckpointAsync(eventArgs.CancellationToken);
        }

        public static Task ProcessErrorHandler(ProcessErrorEventArgs eventArgs)
        {
            // Write details about the error to the console window
            Console.WriteLine($"\tPartition '{ eventArgs.PartitionId}': an unhandled exception was encountered. This was not expected to happen.");
            Console.WriteLine(eventArgs.Exception.Message);
            return Task.CompletedTask;
        }

        public class Payload
        {
            public Payload () {
                Text = "";
                Channel = "";
            }
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