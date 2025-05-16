using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System;
namespace NGA.Models
{
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions<DataContext> options) : base(options)
        {
          
        }
      
        public DbSet<Topic> Topics { get; set; }
        public DbSet<Black> Blacks { get; set; }
        public DbSet<Log> Logs { get; set; }
        public DbSet<Replay> Replays { get; set; }
        public DbSet<ReplayHis> ReplayHis { get; set; }

        public DbSet<User> Users { get; set; }
    }
    public class DataContextFactory : IDesignTimeDbContextFactory<DataContext>
    {
        public DataContext CreateDbContext(string[] args)
        {
            var connectionString = Environment.GetEnvironmentVariable("EFConString"); 
            if (string.IsNullOrEmpty(connectionString))
                throw new InvalidOperationException("The connection string was not set in the 'EFConString' environment variable.");
            var optionsBuilder = new DbContextOptionsBuilder<DataContext>();
          
            optionsBuilder.UseMySql(connectionString,ServerVersion.AutoDetect(connectionString));
            return new DataContext(optionsBuilder.Options);
        }
    }
}
