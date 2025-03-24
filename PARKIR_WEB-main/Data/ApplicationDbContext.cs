using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ParkIRC.Models;
using ParkIRC;

namespace ParkIRC.Data
{
    public class ApplicationDbContext : IdentityDbContext<Operator>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<ParkingSpace> ParkingSpaces { get; set; }
        public DbSet<Vehicle> Vehicles { get; set; }
        public DbSet<ParkingTransaction> ParkingTransactions { get; set; }
        public DbSet<Shift> Shifts { get; set; }
        public DbSet<Operator> Operators { get; set; }
        public DbSet<ParkingTicket> ParkingTickets { get; set; }
        public DbSet<Journal> Journals { get; set; }
        public DbSet<ParkingRateConfiguration> ParkingRates { get; set; }
        public DbSet<CameraSettings> CameraSettings { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<ParkingSpace>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.SpaceNumber).IsRequired();
                entity.Property(e => e.SpaceType).IsRequired();
                entity.Property(e => e.HourlyRate).HasColumnType("decimal(18,2)");
                entity.HasOne(e => e.CurrentVehicle)
                    .WithOne(v => v.ParkingSpace)
                    .HasForeignKey<ParkingSpace>(p => p.CurrentVehicleId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            builder.Entity<Vehicle>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.VehicleNumber).IsRequired();
                entity.Property(e => e.VehicleType).IsRequired();
                entity.HasOne(e => e.Shift)
                    .WithMany(s => s.Vehicles)
                    .HasForeignKey(e => e.ShiftId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            builder.Entity<ParkingTransaction>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.TransactionNumber).IsRequired();
                entity.Property(e => e.HourlyRate).HasColumnType("decimal(18,2)");
                entity.Property(e => e.TotalAmount).HasColumnType("decimal(18,2)");
                entity.HasOne(e => e.Vehicle)
                    .WithMany(v => v.Transactions)
                    .HasForeignKey(e => e.VehicleId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.ParkingSpace)
                    .WithMany(p => p.Transactions)
                    .HasForeignKey(e => e.ParkingSpaceId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<Shift>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ShiftName).IsRequired();
                entity.Property(e => e.StartTime).IsRequired();
                entity.Property(e => e.EndTime).IsRequired();
            });

            builder.Entity<Operator>(entity =>
            {
                entity.Property(e => e.FullName).IsRequired();
                entity.HasMany(e => e.Shifts)
                    .WithMany(s => s.Operators)
                    .UsingEntity(j => j.ToTable("OperatorShifts"));
            });

            builder.Entity<ParkingTicket>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.TicketNumber).IsRequired();
                entity.Property(e => e.BarcodeData).IsRequired();
                entity.HasOne(e => e.Vehicle)
                    .WithMany()
                    .HasForeignKey(e => e.VehicleId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.IssuedByOperator)
                    .WithMany()
                    .HasForeignKey(e => e.OperatorId)
                    .OnDelete(DeleteBehavior.SetNull);
                entity.HasOne(e => e.Shift)
                    .WithMany()
                    .HasForeignKey(e => e.ShiftId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure Journal
            builder.Entity<Journal>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Action).IsRequired();
                entity.Property(e => e.Description).IsRequired();
                entity.Property(e => e.Timestamp).IsRequired();
                entity.Property(e => e.OperatorId).IsRequired();
                
                entity.HasOne(e => e.Operator)
                      .WithMany()
                      .HasForeignKey(e => e.OperatorId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure ParkingRateConfiguration
            builder.Entity<ParkingRateConfiguration>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.VehicleType).IsRequired();
                entity.Property(e => e.BaseRate).HasColumnType("decimal(18,2)");
                entity.Property(e => e.HourlyRate).HasColumnType("decimal(18,2)");
                entity.Property(e => e.DailyRate).HasColumnType("decimal(18,2)");
                entity.Property(e => e.WeeklyRate).HasColumnType("decimal(18,2)");
                entity.Property(e => e.MonthlyRate).HasColumnType("decimal(18,2)");
                entity.Property(e => e.PenaltyRate).HasColumnType("decimal(18,2)");
                entity.Property(e => e.EffectiveFrom).IsRequired();
                entity.Property(e => e.CreatedBy).IsRequired();
                entity.Property(e => e.LastModifiedBy).IsRequired();
            });

            builder.Entity<CameraSettings>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ProfileName).IsRequired();
                entity.Property(e => e.LightingCondition).HasDefaultValue("Normal");
            });
        }
    }
}