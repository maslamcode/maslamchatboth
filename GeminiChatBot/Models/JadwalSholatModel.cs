using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeminiChatBot.Models
{
    public class JadwalSholatModel
    {
        public string propinsi { get; set; } = string.Empty;
        public string kota { get; set; } = string.Empty;
        public int tanggal { get; set; }
        public int bulan { get; set; }
        public string imsak { get; set; } = string.Empty;
        public string subuh { get; set; } = string.Empty;
        public string terbit { get; set; } = string.Empty;
        public string duha { get; set; } = string.Empty;
        public string zuhur { get; set; } = string.Empty;
        public string asar { get; set; } = string.Empty;
        public string magrib { get; set; } = string.Empty;
        public string isya { get; set; } = string.Empty;
    }
}
