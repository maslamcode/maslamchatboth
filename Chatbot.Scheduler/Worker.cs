using Chatbot.Service.Services.ChatbotGroup;
using System.Net.Http.Json;
using System.Text.RegularExpressions;

namespace Chatbot.Scheduler
{
    public class Worker : BackgroundService
    {
        private readonly IChatbotGroupService _chatbotGroupService;
        private readonly ILogger<Worker> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;

        public Worker(IChatbotGroupService chatbotGroupService, ILogger<Worker> logger, IHttpClientFactory httpClientFactory, IConfiguration config)
        {
            _chatbotGroupService = chatbotGroupService;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _config = config;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Scheduler started at: {time}", DateTimeOffset.Now);

            var apiUrl = _config["BroadcastApi:Url"];
            var apiKey = _config["BroadcastApi:ApiKey"];

            if (string.IsNullOrEmpty(apiUrl))
            {
                _logger.LogError("Broadcast API URL not configured!");
                return;
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                var groups = await _chatbotGroupService.GetAllGroupsAsync();
                var activeGroups = groups.Where(x => x.is_receive_broadcast).ToList();

                if (activeGroups.Any())
                {
                    _logger.LogInformation("Sending broadcast to {count} groups", activeGroups.Count);

                    var groupIds = activeGroups.Select(g => g.group_id).ToList();
                    var groupNames = string.Join(", ", activeGroups.Select(g => g.group_name));

                    var payload = new
                    {
                        message = $"[Broadcast] Total {activeGroups.Count} groups — scheduled every 1 min. " +
                                  $"\nStarted at {DateTime.Now:HH:mm:ss}. " +
                                  $"\nGroup Receives: {groupNames}",
                        groupIds
                    };

                    try
                    {
                        var client = _httpClientFactory.CreateClient();
                        client.DefaultRequestHeaders.Add("x-api-key", apiKey);

                        var response = await client.PostAsJsonAsync($"{apiUrl}/broadcast-bulk", payload, stoppingToken);

                        if (response.IsSuccessStatusCode)
                        {
                            _logger.LogInformation("Broadcast success at {time}", DateTimeOffset.Now);
                        }
                        else
                        {
                            var body = await response.Content.ReadAsStringAsync();
                            _logger.LogError("Broadcast failed: {status} - {body}", response.StatusCode, body);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error calling broadcast API");
                    }
                }
                else
                {
                    _logger.LogInformation("No groups marked for broadcast at {time}", DateTimeOffset.Now);
                }

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }
}
