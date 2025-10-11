
using Chatbot.Service.Model.Sholat;

namespace Chatbot.Service.Services.Sholat
{
    public interface ISholatService
    {
        Task<string?> ExtractKotaNameDapper(string prompt);
        Task<IEnumerable<JadwalSholatModel>> GetJadwalSholatByKotaName(string kotaName, bool isCurrentMonth = true);
        Task<string> GetJadwalSholatByKotaNameAsCsv(string kotaName, bool isCurrentMonth = true);

    }
}
