using GeminiChatBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeminiChatBot.Services
{
    public interface ISholatService
    {
        Task<string?> ExtractKotaNameDapper(string prompt);
        Task<IEnumerable<JadwalSholatModel>> GetJadwalSholatByKotaName(string kotaName, bool isCurrentMonth = true);
        Task<string> GetJadwalSholatByKotaNameAsCsv(string kotaName, bool isCurrentMonth = true);

    }
}
