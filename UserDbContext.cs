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
    public DbSet<LoanRequest> LoanRequests { get; set; }
    public DbSet<LoanType> LoanTypes { get; set; }
    public DbSet<UserActivity> UserActivities { get; set; }

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

        // Configure LoanType entity
        modelBuilder.Entity<LoanType>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.LoanTypeName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.InterestRate).IsRequired();
            entity.HasIndex(e => e.LoanTypeName).IsUnique();
        });

        // Configure LoanRequest entity
        modelBuilder.Entity<LoanRequest>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.UserId).IsRequired();
            entity.Property(e => e.Date).IsRequired().HasColumnType("timestamp with time zone");
            entity.Property(e => e.DueDate).IsRequired().HasColumnType("timestamp with time zone");
            entity.Property(e => e.LoanTypeId).IsRequired();
            entity.Property(e => e.Amount).HasColumnType("decimal(18,2)").IsRequired();
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ProcessedDate).HasColumnType("timestamp with time zone");
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.LoanType)
                  .WithMany()
                  .HasForeignKey(e => e.LoanTypeId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.ProcessedByUser)
                  .WithMany()
                  .HasForeignKey(e => e.ProcessedByUserId)
                  .OnDelete(DeleteBehavior.Restrict);
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
            entity.Property(e => e.LoanTypeId).IsRequired();
            entity.Property(e => e.Amount).HasColumnType("decimal(18,2)").IsRequired();
            entity.Property(e => e.InterestReceived).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.LoanType)
                  .WithMany()
                  .HasForeignKey(e => e.LoanTypeId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure UserActivity entity
        modelBuilder.Entity<UserActivity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.UserId).IsRequired();
            entity.Property(e => e.UserName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.UserRole).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Action).IsRequired().HasMaxLength(50);
            entity.Property(e => e.EntityType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.EntityId);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Details).HasMaxLength(2000);
            entity.Property(e => e.HttpMethod).IsRequired().HasMaxLength(10);
            entity.Property(e => e.Endpoint).IsRequired().HasMaxLength(200);
            entity.Property(e => e.IpAddress).HasMaxLength(45);
            entity.Property(e => e.UserAgent).HasMaxLength(500);
            entity.Property(e => e.StatusCode).IsRequired();
            entity.Property(e => e.IsSuccess).IsRequired();
            entity.Property(e => e.ErrorMessage).HasMaxLength(1000);
            entity.Property(e => e.Timestamp).IsRequired();
            entity.Property(e => e.DurationMs).IsRequired();
            
            // Foreign key relationship with User
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
            
            // Index for performance
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Action);
            entity.HasIndex(e => e.EntityType);
        });

        // Seed initial data
        modelBuilder.Entity<UserRole>().HasData(
            new UserRole { Id = 1, Name = "Secretary", Description = "Secretary with full access" },
            new UserRole { Id = 2, Name = "President", Description = "President with full access" },
            new UserRole { Id = 3, Name = "Treasurer", Description = "Treasurer with full access" },
            new UserRole { Id = 4, Name = "Member", Description = "Regular member with limited access" }
        );
        modelBuilder.Entity<User>().HasData(
            new User { Id = 1, Name = "Secretary", Address = "Pattanikoop", Email = "secretary@phenix.com", Phone = "8089011871", UserRoleId = 1, IsActive = true }
            
        );
        modelBuilder.Entity<UserLogin>().HasData(
            new UserLogin { Id = 1, Username = "secretary@phoenix.com", Password = "password1", UserId = 1 }
            
        );
        modelBuilder.Entity<LoanType>().HasData(
            new LoanType { Id = 1, LoanTypeName = "Marriage Loan", InterestRate = 1.16 },
            new LoanType { Id = 2, LoanTypeName = "Personal Loan", InterestRate = 2.5 }
        );
    }
} 