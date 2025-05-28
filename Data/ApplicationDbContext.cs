using Microsoft.EntityFrameworkCore;
using _10.Models; 

namespace _10.Data 
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Package> Packages { get; set; }
        public DbSet<PackageHistory> PackageHistories { get; set; }
        public DbSet<StatusDefinition> StatusDefinitions { get; set; }
        public DbSet<Address> Addresses { get; set; } 

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.UserId); 
                entity.HasIndex(e => e.Username).IsUnique();
                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasIndex(e => e.ApiKey).IsUnique();

                entity.Property(e => e.Role)
                      .HasConversion<string>() 
                      .HasMaxLength(50);    


                if (typeof(User).GetProperty("AddressId") != null) 
                {
                    entity.HasOne(u => u.Address)
                          .WithMany(a => a.Users) 
                          .HasForeignKey(u => u.AddressId)
                          .IsRequired(false) 
                          .OnDelete(DeleteBehavior.SetNull); 
                }
            });

            modelBuilder.Entity<Package>(entity =>
            {
                entity.HasKey(e => e.PackageId);
                entity.HasIndex(e => e.TrackingNumber).IsUnique();

                entity.HasOne(d => d.SenderUser)
                    .WithMany(p => p.SentPackages)
                    .HasForeignKey(d => d.SenderUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.RecipientUser)
                    .WithMany(p => p.ReceivedPackages)
                    .HasForeignKey(d => d.RecipientUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.AssignedCourier)
                    .WithMany(p => p.AssignedPackagesAsCourier)
                    .HasForeignKey(d => d.AssignedCourierId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Property(e => e.PackageSize)
                      .HasConversion<string>() 
                      .HasMaxLength(50);   


                entity.HasOne(p => p.OriginAddress)
                      .WithMany(a => a.PackagesWithThisOrigin) 
                      .HasForeignKey(p => p.OriginAddressId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(p => p.DestinationAddress)
                      .WithMany(a => a.PackagesWithThisDestination) 
                      .HasForeignKey(p => p.DestinationAddressId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<StatusDefinition>(entity =>
            {
                entity.HasKey(e => e.StatusId);
                entity.HasIndex(e => e.Name).IsUnique(); 
            });

            // Konfiguracja dla encji PackageHistory
            modelBuilder.Entity<PackageHistory>(entity =>
            {
                entity.HasKey(e => e.PackageHistoryId);
            });

            modelBuilder.Entity<Address>(entity =>
            {
                entity.HasKey(e => e.AddressId);
                entity.HasIndex(e => new { e.Street, e.City, e.ZipCode, e.Country })
                      .IsUnique()
                      .HasDatabaseName("IX_Address_UniqueLocation");
            });
        }
    }
}