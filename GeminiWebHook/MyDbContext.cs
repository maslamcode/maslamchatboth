using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GeminiChatBot
{
    public class MyDbContext : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql("Host=172.104.163.223;Port=5432;Database=maslam_training;Username=iqbal;Password=bA3F57aDjG7F");
        }
        // Define your DbSets (tables)
        public DbSet<Chatbot> Chatbot { get; set; }
        public DbSet<ChatbotResponses> ChatbotResponses { get; set; }
        public DbSet<Configs> Configs { get; set; }
        

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
}
