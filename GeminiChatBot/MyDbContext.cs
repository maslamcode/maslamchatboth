using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GeminiChatBot
{
    public class MyDbContext : DbContext
    {
        private string _connectionString = string.Empty;
        public MyDbContext()
        {
        }
        public MyDbContext(string connectingString)
        {
            _connectionString = connectingString;
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql(_connectionString);
        }
        // Define your DbSets (tables)
        public DbSet<Chatbot> Chatbot { get; set; }
        public DbSet<ChatbotResponses> ChatbotResponses { get; set; }
        public DbSet<Configs> Configs { get; set; }
        public DbSet<JadwalShalat> JadwalShalats { get; set; }
        public DbSet<Kota> Kotas { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // Fluent API configurations if needed
        }
    }


    [Table("chat_boths", Schema = "public")]
    public class Chatbot
    {
        [Key]
        public int Id { get; set; }
        public string? tag_message { get; set; }
    }

    [Table("chatbot_respone", Schema = "public")]
    public class ChatbotResponses
    {
        [Key]
        public int Id { get; set; }
        public string? tag_message { get; set; }
        public int? type { get; set; }
        public int? order { get; set; }
    }

    [Table("config", Schema = "app")]
    public class Configs
    {
        [Key]
        public int config_id { get; set; }
        public string kategori { get; set; }
        public string param { get; set; }
        public string value { get; set; }
    }

    [Table("jadwal_shalat", Schema = "ref")]
    public class JadwalShalat
    {
        [Key]
        [Column("jadwal_shalat_id")]
        public Guid JadwalShalatId { get; set; }

        [Column("created_by")]
        public Guid CreatedBy { get; set; }

        [Column("created_date")]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        [Column("updated_by")]
        public Guid? UpdatedBy { get; set; }

        [Column("last_update")]
        public DateTime? LastUpdate { get; set; }

        [Column("rowversion")]
        public DateTime RowVersion { get; set; } = DateTime.UtcNow;

        [Column("kota_id")]
        public Guid KotaId { get; set; }

        [Column("tanggal")]
        public short? Tanggal { get; set; }

        [Column("bulan")]
        public short? Bulan { get; set; }

        [Column("imsak")]
        public TimeSpan? Imsak { get; set; }

        [Column("subuh")]
        public TimeSpan? Subuh { get; set; }

        [Column("terbit")]
        public TimeSpan? Terbit { get; set; }

        [Column("duha")]
        public TimeSpan? Duha { get; set; }

        [Column("zuhur")]
        public TimeSpan? Zuhur { get; set; }

        [Column("asar")]
        public TimeSpan? Asar { get; set; }

        [Column("magrib")]
        public TimeSpan? Magrib { get; set; }

        [Column("isya")]
        public TimeSpan? Isya { get; set; }

        [ForeignKey("KotaId")]
        public virtual Kota Kota { get; set; }
    }

    [Table("kota", Schema = "ref")]
    public class Kota
    {
        [Key]
        [Column("kota_id")]
        public Guid KotaId { get; set; }

        [Column("created_by")]
        public Guid CreatedBy { get; set; }

        [Column("created_date")]
        public DateTime CreatedDate { get; set; }

        [Column("updated_by")]
        public Guid? UpdatedBy { get; set; }

        [Column("last_updated")]
        public DateTime? LastUpdated { get; set; }

        [Column("rowversion")]
        public DateTime RowVersion { get; set; }

        [Column("negara_id")]
        public Guid NegaraId { get; set; }

        [Column("propinsi_id")]
        public Guid PropinsiId { get; set; }

        [Column("nama")]
        public string Nama { get; set; } = string.Empty;

        [Column("latitude")]
        public string Latitude { get; set; }

        [Column("longitude")]
        public string Longitude { get; set; }

        [Column("is_aktif")]
        public bool IsAktif { get; set; }

        [Column("nama_geocoding")]
        public string? NamaGeocoding { get; set; }

        [Column("group_wa")]
        public string? GroupWa { get; set; }
        public virtual ICollection<JadwalShalat> JadwalShalats { get; set; } = new List<JadwalShalat>();
    }


}
