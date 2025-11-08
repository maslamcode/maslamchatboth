using Chatbot.Service.Model.ChatbotNumberTask;

namespace Chatbot.Service.Services.ChatbotNumberTask
{
    public interface IChatbotNumberTaskService
    {
        Task<IEnumerable<ChatbotNumberTaskModel>> GetAllTasksAsync();
        Task<IEnumerable<ChatbotNumberTaskModel>> GetAllTasksByNumberIdAsync(Guid chatbotNumberId);
        Task<ChatbotNumberTaskModel?> GetTaskByIdAsync(Guid chatbotNumberTaskId);
    }
}
