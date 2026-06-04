using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace FlappyBirdClone.Models
{
    public class AppDbContext : DbContext
    {
        public DbSet<Score> Scores { get; set; }

        protected override void OnConfiguring(
            DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(
                "Data Source=scores.db");
        }
        public AppDbContext()
        {
            Database.Migrate();
        }
    }
}
