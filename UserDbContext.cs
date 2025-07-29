using Microsoft.EntityFrameworkCore;
using phoenix_sangam_api.Models;

namespace phoenix_sangam_api.Data;

public class UserDbContext : DbContext
{
    public UserDbContext(DbContextOptions<UserDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<UserRole> UserRoles { get; set; }
    public DbSet<UserLogin> UserLogins { get; set; }
    public DbSet<Meeting> Meetings { get; set; }
    public DbSet<Attendance> Attendances { get; set; }
    public DbSet<MeetingPayment> MeetingPayments { get; set; }
    public DbSet<Loan> Loans { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure UserRole entity
        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(200);
            entity.HasIndex(e => e.Name).IsUnique();
        });

        // Configure User entity
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Address).HasMaxLength(200);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.UserRoleId).IsRequired();
            // Add unique constraint on Email
            entity.HasIndex(e => e.Email).IsUnique();
            
            // Foreign key relationship with UserRole
            entity.HasOne(e => e.UserRole)
                  .WithMany(r => r.Users)
                  .HasForeignKey(e => e.UserRoleId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure UserLogin entity
        modelBuilder.Entity<UserLogin>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Username).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Password).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure Meeting entity
        modelBuilder.Entity<Meeting>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Date).IsRequired().HasColumnType("timestamp with time zone");
            entity.Property(e => e.Time).IsRequired().HasColumnType("timestamp with time zone");
            entity.Property(e => e.Description).HasMaxLength(200);
            entity.Property(e => e.Location).HasMaxLength(100);
        });

        // Configure Attendance entity
        modelBuilder.Entity<Attendance>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.UserId).IsRequired();
            entity.Property(e => e.MeetingId).IsRequired();
            entity.Property(e => e.IsPresent).IsRequired();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            // Foreign key relationships
            entity.HasOne(e => e.User)
                  .WithMany(u => u.Attendances)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.Meeting)
                  .WithMany(m => m.Attendances)
                  .HasForeignKey(e => e.MeetingId)
                  .OnDelete(DeleteBehavior.Cascade);
            
            // Composite unique constraint
            entity.HasIndex(e => new { e.UserId, e.MeetingId }).IsUnique();
        });

        // Configure MeetingPayment entity
        modelBuilder.Entity<MeetingPayment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.UserId).IsRequired();
            entity.Property(e => e.MeetingId).IsRequired();
            entity.Property(e => e.MainPayment).HasColumnType("decimal(18,2)");
            entity.Property(e => e.WeeklyPayment).HasColumnType("decimal(18,2)");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            // Foreign key relationships
            entity.HasOne(e => e.User)
                  .WithMany(u => u.MeetingPayments)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.Meeting)
                  .WithMany(m => m.MeetingPayments)
                  .HasForeignKey(e => e.MeetingId)
                  .OnDelete(DeleteBehavior.Cascade);
            
            // Composite unique constraint
            entity.HasIndex(e => new { e.UserId, e.MeetingId }).IsUnique();
        });

        // Configure Loan entity
        modelBuilder.Entity<Loan>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.UserId).IsRequired();
            entity.Property(e => e.Date).IsRequired().HasColumnType("timestamp with time zone");
            entity.Property(e => e.DueDate).IsRequired().HasColumnType("timestamp with time zone");
            entity.Property(e => e.ClosedDate).HasColumnType("timestamp with time zone");
            entity.Property(e => e.InterestRate).HasColumnType("decimal(18,2)").IsRequired();
            entity.Property(e => e.Amount).HasColumnType("decimal(18,2)").IsRequired();
            entity.Property(e => e.InterestReceived).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Seed initial data
        modelBuilder.Entity<UserRole>().HasData(
            new UserRole { Id = 1, Name = "Admin", Description = "Administrator with full access" },
            new UserRole { Id = 2, Name = "User", Description = "Regular user with limited access" }
        );
        modelBuilder.Entity<User>().HasData(
            new User { Id = 1, Name = "John Doe", Address = "123 Main St", Email = "john@example.com", Phone = "555-0101", UserRoleId = 1 },
            new User { Id = 2, Name = "Jane Smith", Address = "456 Oak Ave", Email = "jane@example.com", Phone = "555-0102", UserRoleId = 2 }
        );
        modelBuilder.Entity<UserLogin>().HasData(
            new UserLogin { Id = 1, Username = "john@example.com", Password = "password1", UserId = 1 },
            new UserLogin { Id = 2, Username = "jane@example.com", Password = "password1", UserId = 2 }
        );
    }
} 