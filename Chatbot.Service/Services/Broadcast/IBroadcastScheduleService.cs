using Chatbot.Service.Model.Chatbot;

namespace Chatbot.Service.Services.Broadcast
{
    public interface IBroadcastScheduleService
    {
        Task<IEnumerable<BroadcastScheduleModel>> GetAllAsync();
        Task<IEnumerable<BroadcastScheduleModel>> GetDueSchedulesAsync(DateTime now);
    }
}
