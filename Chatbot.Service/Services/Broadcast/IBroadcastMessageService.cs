using Chatbot.Service.Model.Broadcast;

namespace Chatbot.Service.Services.Broadcast
{
    public interface IBroadcastMessageService
    {
        Task<IEnumerable<BroadcastMessageModel>> GetAllAsync();
    }
}
