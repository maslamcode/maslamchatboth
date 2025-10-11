namespace Chatbot.Service.Services.Chatbot
{
    public interface IChatbotService
    {
        Task<string> HandlePromptGreetingsAsync(string prompt);
        Task<IEnumerable<string>> GetMatchedDataLinksAsync(string prompt);
        Task<IEnumerable<string>> GetMatchedDataFilesAsync(string prompt);

    }
}
