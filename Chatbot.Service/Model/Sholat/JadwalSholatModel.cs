using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chatbot.Service.Model.Sholat
{
    public class JadwalSholatModel
    {
        public string propinsi { get; set; }
        public string kota { get; set; }
        public int tanggal { get; set; }
        public int bulan { get; set; }
        public TimeSpan imsak { get; set; }
        public TimeSpan subuh { get; set; }
        public TimeSpan terbit { get; set; }
        public TimeSpan duha { get; set; }
        public TimeSpan zuhur { get; set; }
        public TimeSpan asar { get; set; }
        public TimeSpan magrib { get; set; }
        public TimeSpan isya { get; set; }
    }
}
