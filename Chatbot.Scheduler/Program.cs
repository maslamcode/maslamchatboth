
using Chatbot.Scheduler;
using Chatbot.Service.Services.ChatbotGroup;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<IChatbotGroupService, ChatbotGroupService>();
builder.Services.AddHttpClient();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
