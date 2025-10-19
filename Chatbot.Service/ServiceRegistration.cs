using Chatbot.Service.Services.Chatbot;
using Chatbot.Service.Services.ChatbotGroup;
using Chatbot.Service.Services.GoogleDrive;
using Chatbot.Service.Services.MessageList;
using Chatbot.Service.Services.Sholat;
using Chatbot.Service.Services.Broadcast;
using Microsoft.Extensions.DependencyInjection;

namespace Chatbot.Service
{
    public static class ServiceRegistration
    {
        public static IServiceCollection AddChatbotServices(this IServiceCollection services)
        {
            services.AddSingleton<IChatbotService, ChatbotService>();

            services.AddSingleton<IChatbotGroupService, ChatbotGroupService>();

            services.AddSingleton<IMessageListService, MessageListService>();

            services.AddSingleton<IBroadcastMessageService, BroadcastMessageService>();
            services.AddSingleton<IBroadcastScheduleService, BroadcastScheduleService>();
            services.AddSingleton<IBroadcastTargetService, BroadcastTargetService>();

            services.AddSingleton<ISholatService, SholatService>();

            services.AddSingleton<IGoogleDriveService, GoogleDriveService>();

            return services;
        }
    }
}
