using GeminiChatBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeminiChatBot.Services
{
    public interface IChatBotGroupService
    {
        Task<IEnumerable<ChatBotGroupModel>> GetAllGroupsAsync();
        Task<ChatBotGroupModel?> GetGroupByIdAsync(Guid chatbotGroupId);
        Task<int> InsertGroupAsync(ChatBotGroupModel model);
        Task<int> UpdateGroupAsync(ChatBotGroupModel model);
        Task<int> DeleteGroupAsync(Guid chatbotGroupId);

    }
}
