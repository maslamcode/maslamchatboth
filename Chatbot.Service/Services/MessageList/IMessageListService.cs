using Chatbot.Service.Model.Chatbot;
using Chatbot.Service.Model.MessageList;

namespace Chatbot.Service.Services.MessageList
{
    public interface IMessageListService
    {
        Task<IEnumerable<MessageListModel>> GetAllAsync();
    }
}
