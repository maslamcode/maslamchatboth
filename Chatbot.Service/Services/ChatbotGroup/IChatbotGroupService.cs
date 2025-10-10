
using Chatbot.Service.Model.ChatbotGroup;

namespace Chatbot.Service.Services.ChatbotGroup
{
    public interface IChatbotGroupService
    {
        Task<IEnumerable<ChatbotGroupModel>> GetAllGroupsAsync();
        Task<ChatbotGroupModel?> GetGroupByIdAsync(Guid chatbotGroupId);
        Task<int> InsertGroupAsync(ChatbotGroupModel model);
        Task<int> UpdateGroupAsync(ChatbotGroupModel model);
        Task<int> DeleteGroupAsync(Guid chatbotGroupId);

    }
}
