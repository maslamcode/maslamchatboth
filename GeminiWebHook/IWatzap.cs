using GeminiChatBot;
using System.Threading.Tasks;

namespace MaslamLibrary.Helper
{
    public interface IWatzap
    {
        Task<WatzapModel> SendWA(string _arg_number_key, string _arg_no_wa, string _arg_message); 
    }
}