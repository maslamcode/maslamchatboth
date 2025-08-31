using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeminiChatBot.Models
{
    public class KotaModel
    {
        public Guid kota_id { get; set; }

        public Guid created_by { get; set; }

        public DateTime created_date { get; set; }

        public Guid? updated_by { get; set; }

        public DateTime? last_updated { get; set; }

        public DateTime rowversion { get; set; }

        public Guid negara_id { get; set; }

        public Guid propinsi_id { get; set; }

        public string nama { get; set; } = string.Empty;

        public string latitude { get; set; }

        public string longitude { get; set; }

        public bool is_aktif { get; set; }

        public string? nama_geocoding { get; set; }

        public string? group_wa { get; set; }
    }
}
