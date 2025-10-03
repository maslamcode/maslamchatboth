using GeminiChatBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeminiChatBot.Services
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
