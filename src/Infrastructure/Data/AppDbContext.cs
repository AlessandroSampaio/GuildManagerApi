using GuildManagerApi.Domain.Entities;
using GuildManagerApi.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace GuildManagerApi.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Report> Reports => Set<Report>();
    public DbSet<Fight> Fights => Set<Fight>();
    public DbSet<Character> Characters => Set<Character>();
    public DbSet<Class> Classes => Set<Class>();
    public DbSet<Guild> Guilds => Set<Guild>();
    public DbSet<PerformanceEntry> PerformanceEntries => Set<PerformanceEntry>();
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<WclUserToken> WclUserTokens => Set<WclUserToken>();
    public DbSet<WclCredential> WclCredentials => Set<WclCredential>();
    public DbSet<Player> Players => Set<Player>();
    public DbSet<ScoringSettings> ScoringSettings => Set<ScoringSettings>();
    public DbSet<ScoringTier> ScoringTiers => Set<ScoringTier>();
    public DbSet<RaidWeek> RaidWeeks => Set<RaidWeek>();
    public DbSet<RaidWeekReport> RaidWeekReports => Set<RaidWeekReport>();

    protected override void OnModelCreating(ModelBuilder builder)
    {

        builder.Entity<Class>(e =>
            {
                e.HasKey(c => c.Id);
                e.Property(c => c.Name).HasMaxLength(20).IsRequired();
                e.Property(c => c.SlugName).HasMaxLength(20);
                e.ToTable("classes");
            });

        builder.Entity<Class>()
            .HasData(
                new Class()
                {
                    Id = 1,
                    Name = "Death Knight",
                    SlugName = "DeathKnight",
                },
                new Class()
                {
                    Id = 2,
                    Name = "Druid",
                    SlugName = "Druid",
                },
                new Class()
                {
                    Id = 3,
                    Name = "Hunter",
                    SlugName = "Hunter",
                },
                new Class()
                {
                    Id = 4,
                    Name = "Mage",
                    SlugName = "Mage",
                },
                new Class()
                {
                    Id = 5,
                    Name = "Monk",
                    SlugName = "Monk",
                },
                new Class()
                {
                    Id = 6,
                    Name = "Paladin",
                    SlugName = "Paladin",
                },
                new Class()
                {
                    Id = 7,
                    Name = "Priest",
                    SlugName = "Priest",
                },
                new Class()
                {
                    Id = 8,
                    Name = "Rogue",
                    SlugName = "Rogue",
                },
                new Class()
                {
                    Id = 9,
                    Name = "Shaman",
                    SlugName = "Shaman",
                },
                new Class()
                {
                    Id = 10,
                    Name = "Warlock",
                    SlugName = "Warlock",
                },
                new Class()
                {
                    Id = 11,
                    Name = "Warrior",
                    SlugName = "Warrior",
                },
                new Class()
                {
                    Id = 12,
                    Name = "Demon Hunter",
                    SlugName = "DemonHunter",
                },
                new Class()
                {
                    Id = 13,
                    Name = "Evoker",
                    SlugName = "Evoker",
                },
                new Class()
                {
                    Id = 99,
                    Name = "Unknown",
                    SlugName = "Unknown",
                }
            );

        builder.Entity<Specialization>(e =>
            {
                e.HasKey(c => new { c.Id, c.ClassId });
                e.Property(c => c.Name).HasMaxLength(20).IsRequired();
                e.Property(c => c.SlugName).HasMaxLength(20);
                e.HasIndex(c => c.ClassId);

                e.HasOne(c => c.Class)
                    .WithMany(c => c.Specializations)
                    .HasForeignKey(c => c.ClassId)
                    .OnDelete(DeleteBehavior.Restrict);
                e.ToTable("specializations");
            });

        builder.Entity<Specialization>().HasData(
            new Specialization() { Id = 1, ClassId = 1, Name = "Blood", SlugName = "Blood" },
            new Specialization() { Id = 2, ClassId = 1, Name = "Frost", SlugName = "Frost" },
            new Specialization() { Id = 3, ClassId = 1, Name = "Unholy", SlugName = "Unholy" },
            new Specialization() { Id = 1, ClassId = 2, Name = "Balance", SlugName = "Balance" },
            new Specialization() { Id = 2, ClassId = 2, Name = "Feral", SlugName = "Feral" },
            new Specialization() { Id = 3, ClassId = 2, Name = "Guardian", SlugName = "Guardian" },
            new Specialization() { Id = 4, ClassId = 2, Name = "Restoration", SlugName = "Restoration" },
            new Specialization() { Id = 1, ClassId = 3, Name = "Beast Mastery", SlugName = "BeastMastery" },
            new Specialization() { Id = 2, ClassId = 3, Name = "Marksmanship", SlugName = "Marksmanship" },
            new Specialization() { Id = 3, ClassId = 3, Name = "Survival", SlugName = "Survival" },
            new Specialization() { Id = 1, ClassId = 4, Name = "Arcane", SlugName = "Arcane" },
            new Specialization() { Id = 2, ClassId = 4, Name = "Fire", SlugName = "Fire" },
            new Specialization() { Id = 3, ClassId = 4, Name = "Frost", SlugName = "Frost" },
            new Specialization() { Id = 1, ClassId = 5, Name = "Brewmaster", SlugName = "Brewmaster" },
            new Specialization() { Id = 2, ClassId = 5, Name = "Mistweaver", SlugName = "Mistweaver" },
            new Specialization() { Id = 3, ClassId = 5, Name = "Windwalker", SlugName = "Windwalker" },
            new Specialization() { Id = 1, ClassId = 6, Name = "Holy", SlugName = "Holy" },
            new Specialization() { Id = 2, ClassId = 6, Name = "Protection", SlugName = "Protection" },
            new Specialization() { Id = 3, ClassId = 6, Name = "Retribution", SlugName = "Retribution" },
            new Specialization() { Id = 1, ClassId = 7, Name = "Discipline", SlugName = "Discipline" },
            new Specialization() { Id = 2, ClassId = 7, Name = "Holy", SlugName = "Holy" },
            new Specialization() { Id = 3, ClassId = 7, Name = "Shadow", SlugName = "Shadow" },
            new Specialization() { Id = 1, ClassId = 8, Name = "Assassination", SlugName = "Assassination" },
            new Specialization() { Id = 2, ClassId = 8, Name = "Subtlety", SlugName = "Subtlety" },
            new Specialization() { Id = 3, ClassId = 8, Name = "Outlaw", SlugName = "Outlaw" },
            new Specialization() { Id = 1, ClassId = 9, Name = "Elemental", SlugName = "Elemental" },
            new Specialization() { Id = 2, ClassId = 9, Name = "Enhancement", SlugName = "Enhancement" },
            new Specialization() { Id = 3, ClassId = 9, Name = "Restoration", SlugName = "Restoration" },
            new Specialization() { Id = 1, ClassId = 10, Name = "Affliction", SlugName = "Affliction" },
            new Specialization() { Id = 2, ClassId = 10, Name = "Demonology", SlugName = "Demonology" },
            new Specialization() { Id = 3, ClassId = 10, Name = "Destruction", SlugName = "Destruction" },
            new Specialization() { Id = 1, ClassId = 11, Name = "Arms", SlugName = "Arms" },
            new Specialization() { Id = 2, ClassId = 11, Name = "Fury", SlugName = "Fury" },
            new Specialization() { Id = 3, ClassId = 11, Name = "Protection", SlugName = "Protection" },
            new Specialization() { Id = 1, ClassId = 12, Name = "Havoc", SlugName = "Havoc" },
            new Specialization() { Id = 2, ClassId = 12, Name = "Vengeance", SlugName = "Vengeance" },
            new Specialization() { Id = 1, ClassId = 13, Name = "Devastation", SlugName = "Devastation" },
            new Specialization() { Id = 2, ClassId = 13, Name = "Preservation", SlugName = "Preservation" },
            new Specialization() { Id = 3, ClassId = 13, Name = "Augmentation", SlugName = "Augmentation" },
            new Specialization() { Id = 1, ClassId = 99, Name = "Unknown", SlugName = "Unknown" }
        );

        builder.Entity<Report>(e =>
            {
                e.HasKey(r => r.Id);
                e.Property(r => r.Id).HasMaxLength(16);
                e.Property(r => r.Title).HasMaxLength(256).IsRequired();
                e.Property(r => r.ImportStatus)
                             .HasConversion<int>()
                             .HasDefaultValue(ImportStatus.Queued);
                e.Property(r => r.ImportError).HasMaxLength(512);
                e.HasIndex(r => r.GuildId);
                e.HasIndex(r => r.StartTime);
                e.HasIndex(r => r.ImportStatus);

                e.HasOne(r => r.Guild)
                 .WithMany(g => g.Reports)
                 .HasForeignKey(r => r.GuildId)
                 .OnDelete(DeleteBehavior.SetNull);

                e.ToTable("reports");
            });

        builder.Entity<Fight>(e =>
            {
                e.HasKey(f => f.Id);
                e.Property(f => f.Name).HasMaxLength(128).IsRequired();
                e.HasIndex(f => f.ReportId);
                e.HasIndex(f => new { f.ReportId, f.FightIndex }).IsUnique();

                e.HasOne(f => f.Report)
                                .WithMany(r => r.Fights)
                    .HasForeignKey(f => f.ReportId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.ToTable("fights");
            });

        builder.Entity<Character>(e =>
            {
                e.HasKey(c => c.Id);
                e.Property(c => c.Name).HasMaxLength(64).IsRequired();
                e.Property(c => c.Server).HasMaxLength(64);
                e.Property(c => c.Region).HasMaxLength(20);
                e.HasIndex(c => c.ClassId);
                e.HasIndex(c => new { c.WclActorId, c.Server }).IsUnique();
                e.HasIndex(c => c.GuildId);

                e.HasOne(c => c.Class)
                    .WithMany(c => c.Characters)
                    .HasForeignKey(c => c.ClassId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(c => c.Guild)
                    .WithMany(g => g.Characters)
                    .HasForeignKey(c => c.GuildId)
                    .OnDelete(DeleteBehavior.SetNull);

                e.HasOne(c => c.Player)
                    .WithMany(p => p.Characters)
                    .HasForeignKey(c => c.PlayerId)
                    .OnDelete(DeleteBehavior.SetNull);


                e.ToTable("characters");
            });

        builder.Entity<Guild>(e =>
            {
                e.HasKey(g => g.Id);
                e.Property(g => g.Name).HasMaxLength(64).IsRequired();
                e.Property(g => g.Region).HasMaxLength(20);
                e.HasIndex(g => g.Region);
                e.HasIndex(g => g.Name);

                e.ToTable("guilds");
            });

        builder.Entity<AppUser>(e =>
            {
                e.HasKey(u => u.Id);
                e.Property(u => u.Username).HasMaxLength(32).IsRequired();
                e.Property(u => u.Email).HasMaxLength(128).IsRequired();
                e.Property(u => u.PasswordHash).IsRequired();
                e.Property(u => u.Role)
                    .HasConversion<string>()
                    .HasMaxLength(16)
                    .IsRequired();
                e.HasIndex(u => u.Username).IsUnique();
                e.HasIndex(u => u.Email).IsUnique();

                e.ToTable("users");
            });

        builder.Entity<RefreshToken>(e =>
            {
                e.HasKey(t => t.Id);
                e.Property(t => t.Token).HasMaxLength(128).IsRequired();
                e.HasIndex(t => t.Token).IsUnique();
                e.HasIndex(t => t.UserId);

                e.HasOne(t => t.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);
                e.ToTable("refresh_tokens");
            });

        builder.Entity<WclUserToken>(e =>
            {
                e.HasKey(t => t.Id);
                e.Property(t => t.AccessToken).IsRequired();
                e.Property(t => t.WclRefreshToken);
                e.HasIndex(t => t.UserId).IsUnique(); // one token per user

                e.HasOne(t => t.User)
                .WithOne(u => u.WclToken)
                .HasForeignKey<WclUserToken>(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);

                e.ToTable("wcl_user_tokens");
            });

        builder.Entity<PerformanceEntry>(e =>
            {
                e.HasKey(p => p.Id);
                e.Property(p => p.Spec).HasMaxLength(32);
                e.Property(p => p.Role).HasMaxLength(16);
                e.HasIndex(p => p.FightId);
                e.HasIndex(p => p.CharacterId);
                e.HasIndex(p => new { p.FightId, p.CharacterId }).IsUnique();

                e.HasOne(p => p.Fight)
                .WithMany(f => f.PerformanceEntries)
                .HasForeignKey(p => p.FightId)
                .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(p => p.Character)
                .WithMany(c => c.PerformanceEntries)
                .HasForeignKey(p => p.CharacterId)
                .OnDelete(DeleteBehavior.Cascade);

                e.ToTable("performance_entries");
            });

        builder.Entity<Player>(e =>
            {
                e.HasKey(p => p.Id);
                e.Property(p => p.Name).HasMaxLength(64).IsRequired();
                e.HasIndex(p => p.Name);
                e.ToTable("players");
            });

        builder.Entity<WclCredential>(e =>
            {
                e.HasKey(c => c.Id);
                e.Property(c => c.Id).ValueGeneratedNever();
                e.Property(c => c.ClientIdEncrypted).IsRequired();
                e.Property(c => c.ClientSecretEncrypted).IsRequired();
                e.Property(c => c.Label).HasMaxLength(128);
                e.ToTable("wcl_credentials");
            });

        builder.Entity<ScoringSettings>(e =>
            {
                e.HasKey(s => s.Id);
                e.Property(s => s.Id).ValueGeneratedNever();
                e.ToTable("scoring_settings");
            });

        builder.Entity<ScoringTier>(e =>
            {
                e.HasKey(t => t.Id);
                e.Property(t => t.Label).HasMaxLength(64);
                e.HasIndex(t => t.ScoringSettingsId);
                e.HasIndex(t => new { t.ScoringSettingsId, t.MinPercent }).IsUnique();

                e.HasOne(t => t.ScoringSettings)
                    .WithMany(s => s.Tiers)
                    .HasForeignKey(t => t.ScoringSettingsId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.ToTable("scoring_tiers");
            });

        builder.Entity<RaidWeek>(e =>
            {
                e.HasKey(w => w.Id);
                e.Property(w => w.Label).HasMaxLength(128).IsRequired();
                // StartsAt deve ser sempre uma terça-feira — validado no controller
                e.HasIndex(w => w.StartsAt).IsUnique();
                e.Ignore(w => w.EndsAt);                // propriedade calculada
                e.ToTable("raid_weeks");
            });

        builder.Entity<RaidWeekReport>(e =>
            {
                e.HasKey(r => r.Id);
                e.Property(r => r.ReportCode).HasMaxLength(16).IsRequired();
                // Um mesmo report code não pode aparecer duas vezes na mesma semana
                e.HasIndex(r => new { r.RaidWeekId, r.ReportCode }).IsUnique();

                e.HasOne(r => r.RaidWeek)
                .WithMany(w => w.ReportEntries)
                .HasForeignKey(r => r.RaidWeekId)
                .OnDelete(DeleteBehavior.Cascade);

                e.ToTable("raid_week_reports");
            });
    }
}
