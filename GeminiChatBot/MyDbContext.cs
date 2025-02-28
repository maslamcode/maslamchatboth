using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GeminiChatBot
{
    public class MyDbContext : DbContext
    {
        private string _connectionString=string.Empty;
        public MyDbContext()
        {
        }
        public MyDbContext(string connectingString)
        {
            _connectionString=connectingString;
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql(_connectionString);
        }
        // Define your DbSets (tables)
        public DbSet<ChatBoths> ChatBoths { get; set; }
        public DbSet<ChatBothResponses> ChatBothResponses { get; set; }
        public DbSet<Configs> Configs { get; set; }
        

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // Fluent API configurations if needed
        }
    }


    [Table("chat_boths", Schema = "public")]
    public class ChatBoths
    {
        [Key]
        public int Id { get; set; }
        public string? tag_message { get; set; }
    }

    [Table("chat_boths_respone", Schema = "public")]
    public class ChatBothResponses
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
