﻿using Chatbot.Service.Services.ChatbotGroup;
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
                    var message = await _broadcastMessageService.GetByIdAsync(schedule.BroadcastMessageId);
                    if (message == null)
                    {
                        _logger.LogWarning("Message not found for schedule {id}", schedule.BroadcastScheduleId);
                        continue;
                    }

                    //  Step 3: Retrieve the target groups 
                    var targets = await _broadcastTargetService.GetAllAsync();
                    if (!targets.Any())
                    {
                        _logger.LogInformation("No broadcast targets found for message {id}", schedule.BroadcastMessageId);
                        continue;
                    }

                    var groupIds = targets.Where(x => x.TargetType == 'G' && x.ChatbotGroupId.HasValue).Select(x => x.ChatbotGroupId.Value).ToList();

                    var groups = await _chatbotGroupService.GetAllGroupsByIdsAsync(groupIds);
                  
                    if (!groups.Any())
                    {
                        _logger.LogInformation("No chatbot groups found for target groups.");
                        continue;
                    }

                    //  Step 4: Prepare message content 
                    string messageText;

                    if (message.IsRandom)
                    {
                        var dayIndex = (int)now.DayOfWeek;
                        var messageList = await _messageListService.GetAllAsync();

                        //Get Valid Messages Topic
                        var validMessages = messageList.Where(m => m.IsActive && m.DayOfWeek.HasValue && m.DayOfWeek.Value == dayIndex).ToList();

                        if (validMessages.Any())
                        {
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

                               messageText = "Random: "+ await _chatbotService.GetResponseFromGeminiAsync(payloadGemini);

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
                            _logger.LogWarning("No valid random messages for today ({dayIndex})", dayIndex);
                            continue;
                        }
                    }
                    else
                    {
                        messageText = message.MessageContent;
                    }

                    _logger.LogInformation("Broadcast Message: " + messageText);

                    //  Step 5: Push to broadcast API 
                    var payload = new
                    {
                        message = messageText,
                        groupIds = groups.Select(t => t.group_id).ToList()
                    };

                    _logger.LogInformation("Broadcast Groups: " + string.Join(", ", groups.Select(t => t.group_name)));

                    var httpClient = _httpClientFactory.CreateClient();
                    httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);

                    var response = await httpClient.PostAsJsonAsync($"{apiUrl}/broadcast-bulk", payload, cancellationToken);

                    if (response.IsSuccessStatusCode)
                    {
                        _logger.LogInformation("Broadcast sent successfully for schedule {id}", schedule.BroadcastScheduleId);
                    }
                    else
                    {
                        _logger.LogError("Failed to send broadcast for schedule {id}: {status}",
                            schedule.BroadcastScheduleId, response.StatusCode);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error executing broadcast schedule {id}", schedule.BroadcastScheduleId);
                }
            }
        }
    }
}
