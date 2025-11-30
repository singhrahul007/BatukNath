using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using WhatsappBot.Model;

namespace WhatsappBot.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        public DbSet<MessageLog> MessageLogs { get; set; }
    }
}
