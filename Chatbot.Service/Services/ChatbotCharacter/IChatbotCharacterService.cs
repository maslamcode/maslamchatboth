using Chatbot.Service.Model.ChatbotCharacter;

namespace Chatbot.Service.Services.ChatbotCharacter
{
    public interface IChatbotCharacterService
    {
        Task<IEnumerable<ChatbotCharacterModel>> GetAllCharactersAsync();
        Task<IEnumerable<ChatbotCharacterModel>> GetAllCharactersByIdsAsync(List<Guid> ids);
        Task<ChatbotCharacterModel?> GetCharacterByIdAsync(Guid chatbotCharacterId);
    }
}
