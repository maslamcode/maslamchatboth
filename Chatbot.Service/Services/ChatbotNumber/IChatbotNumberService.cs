using Chatbot.Service.Model.ChatbotNumber;

namespace Chatbot.Service.Services.ChatbotNumber
{
    public interface IChatbotNumberService
    {
        Task<IEnumerable<ChatbotNumberModel>> GetAllNumbersAsync();
        Task<IEnumerable<ChatbotNumberModel>> GetAllNumbersByIdsAsync(List<Guid> ids);
        Task<ChatbotNumberModel?> GetNumberByIdAsync(Guid chatbotNumberId);
        Task UpdateAllNumbersAsync(string newNomor, string newId);
    }
}
