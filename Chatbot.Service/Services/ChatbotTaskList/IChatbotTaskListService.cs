using Chatbot.Service.Model.ChatbotTaskList;

namespace Chatbot.Service.Services.ChatbotTaskList
{
    public interface IChatbotTaskListService
    {
        Task<IEnumerable<ChatbotTaskListModel>> GetAllTaskListsAsync();
        Task<ChatbotTaskListModel?> GetTaskListByIdAsync(Guid chatbotTaskListId);
    }
}
