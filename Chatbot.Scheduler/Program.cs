
using Chatbot.Scheduler;
using Chatbot.Scheduler.Job;
using Chatbot.Service.Services.ChatbotGroup;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddHttpClient();
builder.Services.AddSingleton<IChatbotGroupService, ChatbotGroupService>();
builder.Services.AddSingleton<IScheduledJob, ChatbotBroadcastJob>();

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
