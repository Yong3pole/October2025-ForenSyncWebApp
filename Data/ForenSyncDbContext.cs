using Microsoft.EntityFrameworkCore;
using ForenSync_WebApp_New.Models;

namespace ForenSync_WebApp_New.Data
{
    public class ForenSyncDbContext : DbContext
    {
        public ForenSyncDbContext(DbContextOptions<ForenSyncDbContext> options)
            : base(options)
        {
        }

        public DbSet<users_tbl> users_tbl { get; set; } 
        public DbSet<case_log> case_logs { get; set; }
        public DbSet<acquisition_log> acquisition_log { get; set; }

        public DbSet<audit_trail> audit_trail { get; set; }

        public DbSet<ImportLog> ImportLog { get; set; }

        public DbSet<import_to_main_logs> import_to_main_logs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<users_tbl>().ToTable("users_tbl");
            modelBuilder.Entity<ImportLog>().ToTable("import_log");
        }
    }
}