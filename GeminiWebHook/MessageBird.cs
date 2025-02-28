using Microsoft.AspNetCore.Components.Web;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using Newtonsoft.Json;
using System.Text;

namespace GeminiWebHook
{
    public interface IMessageBird
    {
        public Task<bool> sendMessage(string to, string message);
    }
    public class MessageBird : IMessageBird
    {
        public async Task<bool> sendMessage(string to, string message)
        {
            var url = "https://conversations.messagebird.com/v1/send"; // Replace with your API URL
            var payload = new
            {
                to = to,
                from = "daf3ab32-f541-4960-87fa-d393f0cbf0f3",
                type = "text",
                content = new
                {
                    text = message,
                    disableUrlPreview = false
                }
            };

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", "AccessKey 5poJFHT0AOEAHEKIXnk6Q6flU"); // Add if needed

                var json = JsonConvert.SerializeObject(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(url, content);
                var responseString = await response.Content.ReadAsStringAsync();

                Console.WriteLine($"Response: {response.StatusCode} - {responseString}");
            }
            return true;
        }
    }
}
