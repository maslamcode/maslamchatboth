using Chatbot.Service.Model.Chatbot;

namespace Chatbot.Service.Services.Broadcast
{
    public interface IBroadcastTargetService
    {
        Task<IEnumerable<BroadcastTargetModel>> GetAllAsync();
    }
}
