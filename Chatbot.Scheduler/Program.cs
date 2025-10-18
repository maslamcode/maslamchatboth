
using Chatbot.Scheduler;
using Chatbot.Scheduler.Job;
using Chatbot.Service;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddHttpClient();

builder.Services.AddChatbotServices();

builder.Services.AddSingleton<IScheduledJob, ChatbotBroadcastJob>();
//builder.Services.AddSingleton<IScheduledJob, FileCleanupJob>();

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
