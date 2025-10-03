using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeminiChatBot.Services
{
    public interface IChatbotService
    {
        Task<string> HandlePromptGreetingsAsync(string prompt);
        Task<IEnumerable<string>> GetMatchedDataLinksAsync(string prompt);
        Task<IEnumerable<string>> GetMatchedDataFilesAsync(string prompt);

    }
}
