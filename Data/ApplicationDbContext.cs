using HumanBirthPredictionSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace HumanBirthPredictionSystem.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<User> Users => Set<User>();
        public DbSet<Country> Countries => Set<Country>();
        public DbSet<City> Cities => Set<City>();
        public DbSet<BirthRecord> BirthRecords => Set<BirthRecord>();
        public DbSet<Prediction> Predictions => Set<Prediction>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ---- USERS -------------------------------------------------
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            // ---- COUNTRIES ----------------------------------------------
            modelBuilder.Entity<Country>()
                .HasIndex(c => c.CountryCode)
                .IsUnique();

            // ---- CITIES: Country 1 -> many Cities -----------------------
            modelBuilder.Entity<City>()
                .HasOne(c => c.Country)
                .WithMany(co => co.Cities)
                .HasForeignKey(c => c.CountryId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<City>()
                .HasIndex(c => new { c.CountryId, c.CityName })
                .IsUnique();

            // ---- BIRTH RECORDS -------------------------------------------
            modelBuilder.Entity<BirthRecord>()
                .HasOne(b => b.Country)
                .WithMany(co => co.BirthRecords)
                .HasForeignKey(b => b.CountryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<BirthRecord>()
                .HasOne(b => b.City)
                .WithMany(ci => ci.BirthRecords)
                .HasForeignKey(b => b.CityId)
                .OnDelete(DeleteBehavior.Restrict);

            // Prevent duplicate records for the same Country/City/Year/RecordType
            modelBuilder.Entity<BirthRecord>()
                .HasIndex(b => new { b.CountryId, b.CityId, b.Year, b.RecordType })
                .IsUnique();

            modelBuilder.Entity<BirthRecord>()
                .HasIndex(b => b.Year);

            // ---- PREDICTIONS -----------------------------------------------
            modelBuilder.Entity<Prediction>()
                .HasOne(p => p.Country)
                .WithMany(co => co.Predictions)
                .HasForeignKey(p => p.CountryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Prediction>()
                .HasOne(p => p.City)
                .WithMany()
                .HasForeignKey(p => p.CityId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Prediction>()
                .HasIndex(p => new { p.CountryId, p.CityId, p.Year, p.PredictionModel });
        }
    }
}
