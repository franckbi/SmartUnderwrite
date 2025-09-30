using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SmartUnderwrite.Core.Entities;
using SmartUnderwrite.Core.RulesEngine.Models;

namespace SmartUnderwrite.Infrastructure.Data;

public class SmartUnderwriteDbContext : IdentityDbContext<User, Role, int>
{
    public SmartUnderwriteDbContext(DbContextOptions<SmartUnderwriteDbContext> options)
        : base(options)
    {
    }

    public DbSet<Affiliate> Affiliates { get; set; }
    public DbSet<Applicant> Applicants { get; set; }
    public DbSet<LoanApplication> LoanApplications { get; set; }
    public DbSet<Document> Documents { get; set; }
    public DbSet<Decision> Decisions { get; set; }
    public DbSet<Rule> Rules { get; set; }
    public DbSet<RuleVersion> RuleVersions { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure entity relationships and constraints
        ConfigureAffiliateEntity(modelBuilder);
        ConfigureUserEntity(modelBuilder);
        ConfigureApplicantEntity(modelBuilder);
        ConfigureLoanApplicationEntity(modelBuilder);
        ConfigureDocumentEntity(modelBuilder);
        ConfigureDecisionEntity(modelBuilder);
        ConfigureRuleEntity(modelBuilder);
        ConfigureRuleVersionEntity(modelBuilder);
        ConfigureAuditLogEntity(modelBuilder);
    }

    private static void ConfigureAffiliateEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Affiliate>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.ExternalId).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.ExternalId).IsUnique();
            entity.Property(e => e.IsActive).HasDefaultValue(true);
        });
    }

    private static void ConfigureUserEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(e => e.FirstName).HasMaxLength(100);
            entity.Property(e => e.LastName).HasMaxLength(100);
            entity.HasOne(e => e.Affiliate)
                  .WithMany(a => a.Users)
                  .HasForeignKey(e => e.AffiliateId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureApplicantEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Applicant>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.SsnHash).IsRequired().HasMaxLength(256);
            entity.Property(e => e.DateOfBirth).IsRequired();
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.OwnsOne(e => e.Address, address =>
            {
                address.Property(a => a.Street).HasMaxLength(200);
                address.Property(a => a.City).HasMaxLength(100);
                address.Property(a => a.State).HasMaxLength(50);
                address.Property(a => a.ZipCode).HasMaxLength(10);
            });
        });
    }

    private static void ConfigureLoanApplicationEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<LoanApplication>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ProductType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.Property(e => e.IncomeMonthly).HasPrecision(18, 2);
            entity.Property(e => e.EmploymentType).HasMaxLength(100);
            entity.Property(e => e.Status).HasConversion<string>();
            
            entity.HasOne(e => e.Affiliate)
                  .WithMany(a => a.LoanApplications)
                  .HasForeignKey(e => e.AffiliateId)
                  .OnDelete(DeleteBehavior.Restrict);
                  
            entity.HasOne(e => e.Applicant)
                  .WithMany()
                  .HasForeignKey(e => e.ApplicantId)
                  .OnDelete(DeleteBehavior.Restrict);

            // Performance indexes
            entity.HasIndex(e => new { e.AffiliateId, e.Status });
            entity.HasIndex(e => new { e.Status, e.CreatedAt });
        });
    }

    private static void ConfigureDocumentEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Document>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FileName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.ContentType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.StoragePath).IsRequired().HasMaxLength(500);
            entity.Property(e => e.FileSize).IsRequired();
            
            entity.HasOne(e => e.LoanApplication)
                  .WithMany(la => la.Documents)
                  .HasForeignKey(e => e.LoanApplicationId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureDecisionEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Decision>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Outcome).HasConversion<string>();
            entity.Property(e => e.Score).IsRequired();
            entity.Property(e => e.Reasons)
                  .HasConversion(
                      v => string.Join(';', v),
                      v => v.Split(';', StringSplitOptions.RemoveEmptyEntries));
            
            entity.HasOne(e => e.LoanApplication)
                  .WithMany(la => la.Decisions)
                  .HasForeignKey(e => e.LoanApplicationId)
                  .OnDelete(DeleteBehavior.Cascade);
                  
            entity.HasOne(e => e.DecidedByUser)
                  .WithMany(u => u.Decisions)
                  .HasForeignKey(e => e.DecidedByUserId)
                  .OnDelete(DeleteBehavior.SetNull);
        });
    }

    private static void ConfigureRuleEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Rule>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.RuleDefinition).IsRequired();
            entity.Property(e => e.Priority).IsRequired();
            entity.Property(e => e.IsActive).HasDefaultValue(true);

            // Performance index for rule execution
            entity.HasIndex(e => new { e.IsActive, e.Priority });
        });
    }

    private static void ConfigureRuleVersionEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RuleVersion>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OriginalRuleId).IsRequired();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.RuleDefinition).IsRequired();
            entity.Property(e => e.Priority).IsRequired();
            entity.Property(e => e.Version).IsRequired();
            entity.Property(e => e.CreatedBy).IsRequired().HasMaxLength(100);
            entity.Property(e => e.ChangeReason).HasMaxLength(500);

            // Performance indexes
            entity.HasIndex(e => new { e.OriginalRuleId, e.Version });
            entity.HasIndex(e => e.CreatedAt);
        });
    }

    private static void ConfigureAuditLogEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EntityType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.EntityId).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Action).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Changes).IsRequired();
            entity.Property(e => e.UserId).HasMaxLength(50);
            entity.Property(e => e.Timestamp).IsRequired();
            
            entity.HasIndex(e => new { e.EntityType, e.EntityId });
            entity.HasIndex(e => e.Timestamp);
        });
    }
}