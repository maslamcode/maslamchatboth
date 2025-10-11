using Chatbot.Scheduler.Job;
using Chatbot.Service.Services.ChatbotGroup;
using System.Net.Http.Json;

namespace Chatbot.Scheduler.Job
{
    public class ChatbotBroadcastJob : IScheduledJob
    {
        private readonly IChatbotGroupService _chatbotGroupService;
        private readonly ILogger<ChatbotBroadcastJob> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;

        public string Name => "Chatbot Broadcast";
        public TimeSpan Interval => TimeSpan.FromMinutes(5);

        public ChatbotBroadcastJob(
            IChatbotGroupService chatbotGroupService,
            ILogger<ChatbotBroadcastJob> logger,
            IHttpClientFactory httpClientFactory,
            IConfiguration config)
        {
            _chatbotGroupService = chatbotGroupService;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _config = config;
        }

        public async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            var apiUrl = _config["BroadcastApi:Url"];
            var apiKey = _config["BroadcastApi:ApiKey"];

            if (string.IsNullOrEmpty(apiUrl))
            {
                _logger.LogError("Broadcast API URL not configured!");
                return;
            }

            var groups = await _chatbotGroupService.GetAllGroupsAsync();
            var activeGroups = groups.Where(x => x.is_receive_broadcast).ToList();

            //Broacast Schdeule WHERE DateTime Now >= Record Data di Schedule
            //If(valid) executed data tersebut
            //{
            //  //var scheduleTime = groups.Where(x => x.schedule_time <= DateTime.Now.TimeOfDay).ToList();
            // Get groups target & scheduler message
            // // Todo Implement chatbot broadcast topic ke LLM  -> response
            // Push to API Broadcast Bulk
            //}

            if (!activeGroups.Any())
            {
                _logger.LogInformation("No groups marked for broadcast.");
                return;
            }

            var payload = new
            {
                message = $"[Broadcast] Total {activeGroups.Count} groups — scheduled every 1 min. " +
                          $"\nStarted at {DateTime.Now:HH:mm:ss}.",
                groupIds = activeGroups.Select(g => g.group_id).ToList()
            };

            try
            {
                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Add("x-api-key", apiKey);

                var response = await client.PostAsJsonAsync($"{apiUrl}/broadcast-bulk", payload, cancellationToken);

                if (response.IsSuccessStatusCode)
                    _logger.LogInformation("Broadcast success at {time}", DateTimeOffset.Now);
                else
                    _logger.LogError("Broadcast failed: {status}", response.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing Chatbot Broadcast Job");
            }
        }
    }
}
