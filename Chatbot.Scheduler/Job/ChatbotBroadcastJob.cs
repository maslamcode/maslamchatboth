using Chatbot.Service.Services.ChatbotGroup;
using Chatbot.Service.Services.MessageList;
using Chatbot.Service.Services.Broadcast;
using Chatbot.Service.Services.Chatbot;
using System.Net.Http.Json;

namespace Chatbot.Scheduler.Job
{
    public class ChatbotBroadcastJob : IScheduledJob
    {
        private readonly IBroadcastScheduleService _broadcastScheduleService;
        private readonly IBroadcastMessageService _broadcastMessageService;
        private readonly IBroadcastTargetService _broadcastTargetService;
        private readonly IMessageListService _messageListService;
        private readonly IChatbotGroupService _chatbotGroupService;
        private readonly IChatbotService _chatbotService;
        private readonly ILogger<ChatbotBroadcastJob> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;

        public string Name => "Chatbot Broadcast Scheduler";
        public TimeSpan Interval => TimeSpan.FromMinutes(1);

        public ChatbotBroadcastJob(
            IBroadcastScheduleService broadcastScheduleService,
            IBroadcastMessageService broadcastMessageService,
            IBroadcastTargetService broadcastTargetService,
            IMessageListService messageListService,
            IChatbotGroupService chatbotGroupService,
            IChatbotService chatbotService,
            ILogger<ChatbotBroadcastJob> logger,
            IHttpClientFactory httpClientFactory,
            IConfiguration config)
        {
            _broadcastScheduleService = broadcastScheduleService;
            _broadcastMessageService = broadcastMessageService;
            _broadcastTargetService = broadcastTargetService;
            _messageListService = messageListService;
            _chatbotGroupService = chatbotGroupService;
            _chatbotService = chatbotService;
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
                _logger.LogError("Broadcast API URL not configured.");
                return;
            }

            //var partsDatas = new[] { new { text = $"Topic: Keutamaan Sholat Jum'at \n\nPertanyaan: Tolong buatkan broadcast message whatsapp dari topic tersebut, Sopan santun, energik, selalu mendoakan kebaikan, membuat jadi ingin bertanya lagi dan harus ada kutipan hadits shahih atau ayat Al-Qur'an yang relevan dengan jawaban, agar para pengguna maslam, Selalu bersemangat dalam agama Islam. Langsung berikan jawaban seolah bukan bot, tanpa perlu basa basi menginformasikan ini broadcast atau ini jawabannya." } };

            //var payloadGeminis = new
            //{
            //    contents = new[]
            //    {
            //                            new
            //                            {
            //                                parts = partsDatas
            //                            }
            //                        }
            //};

            //var messageTexts = "Random: " + await _chatbotService.GetResponseFromGeminiAsync(payloadGeminis);

            //_logger.LogInformation("Broadcast Message: " + messageTexts);

            //return;

            //  Step 1: Retrieve all active schedules that are due now 
            var now = DateTime.Now;
            _logger.LogInformation("Day Of Week: " + (int)now.DayOfWeek);
            var dueSchedules = await _broadcastScheduleService.GetDueSchedulesAsync(now);

            if (!dueSchedules.Any())
            {
                _logger.LogInformation("No broadcast schedules due at {time}", now);
                return;
            }

            _logger.LogInformation("{count} broadcast schedule(s) found due at {time}", dueSchedules.Count(), now);

            foreach (var schedule in dueSchedules)
            {
                try
                {
                    //  Step 2: Retrieve the broadcast message content 
                    var message = await _broadcastMessageService.GetByIdAsync(schedule.broadcast_message_id);
                    if (message == null)
                    {
                        _logger.LogWarning("Message not found for schedule {id}", schedule.broadcast_schedule_id);
                        continue;
                    }

                    //  Step 3: Retrieve the target groups 
                    _logger.LogInformation("By Broadcast Message Id=" + schedule.broadcast_message_id);
                    var targets = await _broadcastTargetService.GetByBroadcastMessageIdAsync(schedule.broadcast_message_id);

                    if (!targets.Any())
                    {
                        _logger.LogInformation("No broadcast targets found for message {id}", schedule.broadcast_message_id);
                        continue;
                    }

                    _logger.LogInformation("targets=" + targets.Count());
                    foreach (var target in targets)
                    {
                        _logger.LogInformation("id=" + target.broadcast_target_id);
                        _logger.LogInformation("broadcast_message_id=" + target.broadcast_message_id);
                        _logger.LogInformation("chatbot_group_id=" + target.chatbot_group_id);
                        _logger.LogInformation("target_type=" + target.target_type);
                    }


                    var groupTargets = targets.Where(t => t.target_type == 'G' && t.chatbot_group_id.HasValue).ToList();
                    _logger.LogInformation("groupTargets=" + groupTargets.Count());

                    var personalTargets = targets.Where(t => t.target_type == 'P' && !string.IsNullOrEmpty(t.no_wa)).ToList();

                    var groupIds = groupTargets.Select(t => t.chatbot_group_id!.Value).ToList();
                    var groups = await _chatbotGroupService.GetAllGroupsByIdsAsync(groupIds);

                    _logger.LogInformation("groupIds=" + groupIds.Count());
                    _logger.LogInformation("groups=" + groups.Count());

                    //  Step 4: Prepare message content 
                    string messageText;

                    if (message.IsRandom)
                    {
                        var dayIndex = (int)now.DayOfWeek;
                        var messageList = await _messageListService.GetAllAsync();

                        //Get Valid Messages Topic
                        var validMessages = messageList.Where(m => m.IsActive && m.DayOfWeek.HasValue && m.DayOfWeek.Value == dayIndex).ToList();


                        if (!validMessages.Any())
                        {
                            _logger.LogWarning("No valid random messages for today ({dayIndex})", dayIndex);
                            continue;
                        }

                        //Pick Random Message
                        var random = new Random();
                        var picked = validMessages[random.Next(validMessages.Count)];

                        try
                        {
                            var partsData = new[] { new { text = $"Topic:  {picked.Title} \n\nPertanyaan: Tolong buatkan broadcast message whatsapp dari topic tersebut, Sopan santun, energik, selalu mendoakan kebaikan, membuat jadi ingin bertanya lagi dan harus ada kutipan hadits shahih atau ayat Al-Qur'an yang relevan dengan jawaban, agar para pengguna maslam, Selalu bersemangat dalam agama Islam. Langsung berikan jawaban seolah bukan bot, tanpa perlu basa basi menginformasikan ini broadcast atau ini jawabannya." } };

                            var payloadGemini = new
                            {
                                contents = new[]
                                {
                                        new
                                        {
                                            parts = partsData
                                        }
                                    }
                            };

                            messageText = "Random: " + await _chatbotService.GetResponseFromGeminiAsync(payloadGemini);

                            _logger.LogInformation("Random message picked from list: {title}", picked.Title);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error getting LLM response, fallback to message content.");
                            messageText = picked.MessageContent;
                        }

                    }
                    else
                    {
                        messageText = message.MessageContent;
                    }

                    _logger.LogInformation("Broadcast Message: " + messageText);
                    var httpClient = _httpClientFactory.CreateClient();
                    httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);

                    // Step 5: Send to group targets
                    if (groups.Any())
                    {
                        var groupPayload = new
                        {
                            message = messageText,
                            groupIds = groups.Select(g => g.group_id).ToList()
                        };

                        _logger.LogInformation("Broadcast Groups: {groups}", string.Join(", ", groups.Select(g => g.group_name)));

                        var response = await httpClient.PostAsJsonAsync($"{apiUrl}/broadcast-bulk", groupPayload, cancellationToken);

                        if (response.IsSuccessStatusCode)
                        {
                            await _broadcastScheduleService.UpdateLastExecutedDateAsync(schedule.broadcast_schedule_id, DateTime.Now);

                            _logger.LogInformation("Broadcast sent successfully to groups for schedule {id}", schedule.broadcast_schedule_id);
                        }
                        else
                        {
                            _logger.LogError("Failed to send broadcast to groups for schedule {id}: {status}", schedule.broadcast_schedule_id, response.StatusCode);
                        }
                    }

                    // Step 6: Send to personal targets
                    foreach (var p in personalTargets)
                    {
                        var personalPayload = new
                        {
                            message = messageText,
                            phoneNumber = p.no_wa
                        };

                        _logger.LogInformation("Broadcast Personal: {no_wa}", p.no_wa);

                        var response = await httpClient.PostAsJsonAsync($"{apiUrl}/broadcast-personal", personalPayload, cancellationToken);

                        if (response.IsSuccessStatusCode)
                        {
                            await _broadcastScheduleService.UpdateLastExecutedDateAsync(schedule.broadcast_schedule_id, DateTime.Now);
                            _logger.LogInformation("Broadcast sent successfully to {no_wa}", p.no_wa);
                        }
                        else
                        {
                            _logger.LogError("Failed to send broadcast to {no_wa}: {status}", p.no_wa, response.StatusCode);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error executing broadcast schedule {id}", schedule.broadcast_schedule_id);
                }
            }
        }
    }
}
