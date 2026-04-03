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
    public DbSet<BattleNetUserToken> BattleNetUserTokens => Set<BattleNetUserToken>();
    public DbSet<BattleNetCredential> BattleNetCredentials => Set<BattleNetCredential>();
    public DbSet<RaiderIoCredential> RaiderIoCredentials => Set<RaiderIoCredential>();
    public DbSet<Player> Players => Set<Player>();
    public DbSet<ScoringSettings> ScoringSettings => Set<ScoringSettings>();
    public DbSet<ScoringTier> ScoringTiers => Set<ScoringTier>();
    public DbSet<Core> Cores => Set<Core>();
    public DbSet<CorePlayer> CorePlayers => Set<CorePlayer>();
    public DbSet<RaidWeek> RaidWeeks => Set<RaidWeek>();
    public DbSet<RaidWeekReport> RaidWeekReports => Set<RaidWeekReport>();
    public DbSet<PenaltyEvent> PenaltyEvents => Set<PenaltyEvent>();
    public DbSet<PlayerWeekPenalty> PlayerWeekPenalties => Set<PlayerWeekPenalty>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<RaiderIoCharacterSnapshot> RaiderIoCharacterSnapshots => Set<RaiderIoCharacterSnapshot>();
    public DbSet<RaiderIoMythicRun> RaiderIoMythicRuns => Set<RaiderIoMythicRun>();
    public DbSet<RaiderIoRunAffix> RaiderIoRunAffixes => Set<RaiderIoRunAffix>();

    protected override void OnModelCreating(ModelBuilder builder)
    {

        builder.Entity<Class>(e =>
            {
                e.HasKey(c => c.Id);
                e.Property(c => c.Id).HasColumnName("id");
                e.Property(c => c.Name).HasColumnName("name").HasMaxLength(20).IsRequired();
                e.Property(c => c.SlugName).HasColumnName("slug_name").HasMaxLength(20);
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
                e.Property(c => c.Id).HasColumnName("id");
                e.Property(c => c.ClassId).HasColumnName("class_id");
                e.Property(c => c.Name).HasColumnName("name").HasMaxLength(20).IsRequired();
                e.Property(c => c.SlugName).HasColumnName("slug_name").HasMaxLength(20);
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
                e.Property(r => r.Id).HasColumnName("id").HasMaxLength(16);
                e.Property(r => r.Title).HasColumnName("title").HasMaxLength(256).IsRequired();
                e.Property(r => r.ImportStatus)
                             .HasColumnName("import_status")
                             .HasConversion<int>()
                             .HasDefaultValue(ImportStatus.Queued);
                e.Property(r => r.ImportError).HasColumnName("import_error").HasMaxLength(512);
                e.Property(r => r.GuildId).HasColumnName("guild_id");
                e.Property(r => r.StartTime).HasColumnName("start_time");
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
                e.Property(f => f.Id).HasColumnName("id");
                e.Property(f => f.Name).HasColumnName("name").HasMaxLength(128).IsRequired();
                e.Property(f => f.ReportId).HasColumnName("report_id");
                e.Property(f => f.FightIndex).HasColumnName("fight_index");
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
                e.Property(c => c.Id).HasColumnName("id");
                e.Property(c => c.Name).HasColumnName("name").HasMaxLength(64).IsRequired();
                e.Property(c => c.Server).HasColumnName("server").HasMaxLength(64);
                e.Property(c => c.Region).HasColumnName("region").HasMaxLength(20);
                e.Property(c => c.ClassId).HasColumnName("class_id");
                e.Property(c => c.WclActorId).HasColumnName("wcl_actor_id");
                e.Property(c => c.GuildId).HasColumnName("guild_id");
                e.Property(c => c.PlayerId).HasColumnName("player_id");
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
                e.Property(g => g.Id).HasColumnName("id");
                e.Property(g => g.Name).HasColumnName("name").HasMaxLength(64).IsRequired();
                e.Property(g => g.Region).HasColumnName("region").HasMaxLength(20);
                e.HasIndex(g => g.Region);
                e.HasIndex(g => g.Name);

                e.ToTable("guilds");
            });

        builder.Entity<AppUser>(e =>
            {
                e.HasKey(u => u.Id);
                e.Property(u => u.Id).HasColumnName("id");
                e.Property(u => u.Username).HasColumnName("username").HasMaxLength(32).IsRequired();
                e.Property(u => u.Email).HasColumnName("email").HasMaxLength(128).IsRequired();
                e.Property(u => u.PasswordHash).HasColumnName("password_hash").IsRequired();
                e.Property(u => u.Role)
                    .HasColumnName("role")
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
                e.Property(t => t.Id).HasColumnName("id");
                e.Property(t => t.Token).HasColumnName("token").HasMaxLength(128).IsRequired();
                e.Property(t => t.UserId).HasColumnName("user_id");
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
                e.Property(t => t.Id).HasColumnName("id");
                e.Property(t => t.AccessToken).HasColumnName("access_token").IsRequired();
                e.Property(t => t.WclRefreshToken).HasColumnName("wcl_refresh_token");
                e.Property(t => t.UserId).HasColumnName("user_id");
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
                e.Property(p => p.Id).HasColumnName("id");
                e.Property(p => p.Spec).HasColumnName("spec").HasMaxLength(32);
                e.Property(p => p.Role).HasColumnName("role").HasMaxLength(16);
                e.Property(p => p.FightId).HasColumnName("fight_id");
                e.Property(p => p.CharacterId).HasColumnName("character_id");
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
                e.Property(p => p.Id).HasColumnName("id");
                e.Property(p => p.Name).HasColumnName("name").HasMaxLength(64).IsRequired();
                e.Property(p => p.AppUserId).HasColumnName("app_user_id");
                e.HasIndex(p => p.Name);
                e.HasIndex(p => p.AppUserId).IsUnique();

                e.HasOne(p => p.AppUser)
                    .WithOne(u => u.Player)
                    .HasForeignKey<Player>(p => p.AppUserId)
                    .OnDelete(DeleteBehavior.SetNull)
                    .IsRequired(false);

                e.ToTable("players");
            });

        builder.Entity<WclCredential>(e =>
            {
                e.HasKey(c => c.Id);
                e.Property(c => c.Id).HasColumnName("id").ValueGeneratedNever();
                e.Property(c => c.ClientIdEncrypted).HasColumnName("client_id_encrypted").IsRequired();
                e.Property(c => c.ClientSecretEncrypted).HasColumnName("client_secret_encrypted").IsRequired();
                e.Property(c => c.Label).HasColumnName("label").HasMaxLength(128);
                e.ToTable("wcl_credentials");
            });

        builder.Entity<ScoringSettings>(e =>
            {
                e.HasKey(s => s.Id);
                e.Property(s => s.Id).HasColumnName("id").ValueGeneratedNever();
                e.ToTable("scoring_settings");
            });

        builder.Entity<ScoringTier>(e =>
            {
                e.HasKey(t => t.Id);
                e.Property(t => t.Id).HasColumnName("id");
                e.Property(t => t.Label).HasColumnName("label").HasMaxLength(64);
                e.Property(t => t.ScoringSettingsId).HasColumnName("scoring_settings_id");
                e.Property(t => t.MinPercent).HasColumnName("min_percent");
                e.HasIndex(t => t.ScoringSettingsId);
                e.HasIndex(t => new { t.ScoringSettingsId, t.MinPercent }).IsUnique();

                e.HasOne(t => t.ScoringSettings)
                    .WithMany(s => s.Tiers)
                    .HasForeignKey(t => t.ScoringSettingsId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.ToTable("scoring_tiers");
            });

        builder.Entity<Core>(e =>
            {
                e.HasKey(c => c.Id);
                e.Property(c => c.Id).HasColumnName("id");
                e.Property(c => c.Name).HasColumnName("name").HasMaxLength(128).IsRequired();
                e.Property(c => c.GuildId).HasColumnName("guild_id");
                e.Property(c => c.CreatedAt).HasColumnName("created_at");
                e.Property(c => c.UpdatedAt).HasColumnName("updated_at");

                e.HasOne(c => c.Guild)
                    .WithMany(g => g.Cores)
                    .HasForeignKey(c => c.GuildId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.ToTable("cores");
            });

        builder.Entity<CorePlayer>(e =>
            {
                e.HasKey(cp => new { cp.CoreId, cp.PlayerId });
                e.Property(cp => cp.CoreId).HasColumnName("core_id");
                e.Property(cp => cp.PlayerId).HasColumnName("player_id");

                e.HasOne(cp => cp.Core)
                    .WithMany(c => c.Players)
                    .HasForeignKey(cp => cp.CoreId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(cp => cp.Player)
                    .WithMany(p => p.CoreMemberships)
                    .HasForeignKey(cp => cp.PlayerId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.ToTable("core_players");
            });

        builder.Entity<RaidWeek>(e =>
            {
                e.HasKey(w => w.Id);
                e.Property(w => w.Id).HasColumnName("id");
                e.Property(w => w.Label).HasColumnName("label").HasMaxLength(128).IsRequired();
                e.Property(w => w.StartsAt).HasColumnName("starts_at");
                e.Property(w => w.CoreId).HasColumnName("core_id");
                // StartsAt deve ser sempre uma terça-feira — validado no controller
                e.HasIndex(w => w.StartsAt).IsUnique();
                e.Ignore(w => w.EndsAt);                // propriedade calculada

                e.HasOne(w => w.Core)
                    .WithMany(c => c.RaidWeeks)
                    .HasForeignKey(w => w.CoreId)
                    .OnDelete(DeleteBehavior.SetNull)
                    .IsRequired(false);

                e.ToTable("raid_weeks");
            });

        builder.Entity<RaidWeekReport>(e =>
            {
                e.HasKey(r => r.Id);
                e.Property(r => r.Id).HasColumnName("id");
                e.Property(r => r.ReportCode).HasColumnName("report_code").HasMaxLength(16).IsRequired();
                e.Property(r => r.RaidWeekId).HasColumnName("raid_week_id");
                // Um mesmo report code não pode aparecer duas vezes na mesma semana
                e.HasIndex(r => new { r.RaidWeekId, r.ReportCode }).IsUnique();

                e.HasOne(r => r.RaidWeek)
                .WithMany(w => w.ReportEntries)
                .HasForeignKey(r => r.RaidWeekId)
                .OnDelete(DeleteBehavior.Cascade);

                e.ToTable("raid_week_reports");
            });

        builder.Entity<PenaltyEvent>(e =>
            {
                e.HasKey(p => p.Id);
                e.Property(p => p.Id).HasColumnName("id");
                e.Property(p => p.Description).HasColumnName("description").HasMaxLength(128).IsRequired();
                e.ToTable("penalty_events");
            });

        builder.Entity<PlayerWeekPenalty>(e =>
            {
                e.HasKey(p => p.Id);
                e.Property(p => p.Id).HasColumnName("id");
                e.Property(p => p.Note).HasColumnName("note").HasMaxLength(256);
                e.Property(p => p.RaidWeekId).HasColumnName("raid_week_id");
                e.Property(p => p.PlayerId).HasColumnName("player_id");
                e.Property(p => p.PenaltyEventId).HasColumnName("penalty_event_id");
                e.HasIndex(p => new { p.RaidWeekId, p.PlayerId, p.PenaltyEventId });

                e.HasOne(p => p.Player)
                    .WithMany()
                    .HasForeignKey(p => p.PlayerId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(p => p.RaidWeek)
                    .WithMany()
                    .HasForeignKey(p => p.RaidWeekId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(p => p.PenaltyEvent)
                    .WithMany(e => e.PlayerPenalties)
                    .HasForeignKey(p => p.PenaltyEventId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.ToTable("player_week_penalties");
            });

        builder.Entity<AuditLog>(e =>
            {
                e.HasKey(a => a.Id);
                e.Property(a => a.Id).HasColumnName("id");
                e.Property(a => a.Action).HasColumnName("action").HasMaxLength(100).IsRequired();
                e.Property(a => a.EntityType).HasColumnName("entity_type").HasMaxLength(100).IsRequired();
                e.Property(a => a.EntityId).HasColumnName("entity_id").HasMaxLength(100);
                e.Property(a => a.ActorId).HasColumnName("actor_id");
                e.Property(a => a.ActorUsername).HasColumnName("actor_username").HasMaxLength(100);
                e.Property(a => a.Details).HasColumnName("details");
                e.Property(a => a.OccurredAt).HasColumnName("occurred_at");
                e.HasIndex(a => a.OccurredAt);
                e.HasIndex(a => a.EntityType);
                e.HasIndex(a => a.Action);
                e.ToTable("audit_logs");
            });

        builder.Entity<BattleNetUserToken>(e =>
            {
                e.HasKey(t => t.Id);
                e.Property(t => t.Id).HasColumnName("id");
                e.Property(t => t.UserId).HasColumnName("user_id");
                e.Property(t => t.AccessToken).HasColumnName("access_token").IsRequired();
                e.Property(t => t.ExpiresAt).HasColumnName("expires_at");
                e.Property(t => t.CreatedAt).HasColumnName("created_at");
                e.Property(t => t.LastRefreshedAt).HasColumnName("last_refreshed_at");
                e.Property(t => t.BattleTag).HasColumnName("battle_tag").HasMaxLength(64);
                e.Property(t => t.Sub).HasColumnName("sub").HasMaxLength(32);
                e.Ignore(t => t.IsExpired);
                e.HasIndex(t => t.UserId).IsUnique(); // one token per user

                e.HasOne(t => t.User)
                .WithOne(u => u.BattleNetToken)
                .HasForeignKey<BattleNetUserToken>(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);

                e.ToTable("bnet_user_tokens");
            });

        builder.Entity<BattleNetCredential>(e =>
            {
                e.HasKey(c => c.Id);
                e.Property(c => c.Id).HasColumnName("id").ValueGeneratedNever();
                e.Property(c => c.ClientIdEncrypted).HasColumnName("client_id_encrypted").IsRequired();
                e.Property(c => c.ClientSecretEncrypted).HasColumnName("client_secret_encrypted").IsRequired();
                e.Property(c => c.Label).HasColumnName("label").HasMaxLength(128);
                e.Property(c => c.CreatedAt).HasColumnName("created_at");
                e.Property(c => c.UpdatedAt).HasColumnName("updated_at");
                e.ToTable("bnet_credentials");
            });

        builder.Entity<RaiderIoCredential>(e =>
            {
                e.HasKey(c => c.Id);
                e.Property(c => c.Id).HasColumnName("id").ValueGeneratedNever();
                e.Property(c => c.ApiKeyEncrypted).HasColumnName("api_key_encrypted").IsRequired();
                e.Property(c => c.Label).HasColumnName("label").HasMaxLength(128);
                e.Property(c => c.CreatedAt).HasColumnName("created_at");
                e.Property(c => c.UpdatedAt).HasColumnName("updated_at");
                e.ToTable("raider_io_credentials");
            });

        builder.Entity<RaiderIoCharacterSnapshot>(e =>
            {
                e.HasKey(s => s.Id);
                e.Property(s => s.Id).HasColumnName("id");
                e.Property(s => s.Name).HasColumnName("name").HasMaxLength(50).IsRequired();
                e.Property(s => s.Realm).HasColumnName("realm").HasMaxLength(100).IsRequired();
                e.Property(s => s.Region).HasColumnName("region").HasMaxLength(10).IsRequired();
                e.Property(s => s.ThumbnailUrl).HasColumnName("thumbnail_url").HasMaxLength(512);
                e.Property(s => s.LastCrawledAt).HasColumnName("last_crawled_at");
                e.Property(s => s.CachedAt).HasColumnName("cached_at");
                e.Property(s => s.CharacterId).HasColumnName("character_id");
                e.HasIndex(s => new { s.Name, s.Realm, s.Region }).IsUnique();
                e.HasIndex(s => s.CharacterId);

                e.HasOne(s => s.Character)
                    .WithOne(c => c.RaiderIoSnapshot)
                    .HasForeignKey<RaiderIoCharacterSnapshot>(s => s.CharacterId)
                    .OnDelete(DeleteBehavior.SetNull)
                    .IsRequired(false);

                e.ToTable("raiderio_character_snapshots");
            });

        builder.Entity<RaiderIoMythicRun>(e =>
            {
                e.HasKey(r => r.Id);
                e.Property(r => r.Id).HasColumnName("id");
                e.Property(r => r.SnapshotId).HasColumnName("snapshot_id");
                e.Property(r => r.KeystoneRunId).HasColumnName("keystone_run_id");
                e.Property(r => r.Dungeon).HasColumnName("dungeon").HasMaxLength(128).IsRequired();
                e.Property(r => r.ShortName).HasColumnName("short_name").HasMaxLength(16);
                e.Property(r => r.MythicLevel).HasColumnName("mythic_level");
                e.Property(r => r.CompletedAt).HasColumnName("completed_at");
                e.Property(r => r.Score).HasColumnName("score");
                e.Property(r => r.IconUrl).HasColumnName("icon_url").HasMaxLength(512);
                e.Property(r => r.BackgroundImageUrl).HasColumnName("background_image_url").HasMaxLength(512);
                e.HasIndex(r => r.KeystoneRunId).IsUnique();
                e.HasIndex(r => r.SnapshotId);

                e.HasOne(r => r.Snapshot)
                    .WithMany(s => s.MythicRuns)
                    .HasForeignKey(r => r.SnapshotId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.ToTable("raiderio_mythic_runs");
            });

        builder.Entity<RaiderIoRunAffix>(e =>
            {
                e.HasKey(a => a.Id);
                e.Property(a => a.Id).HasColumnName("id");
                e.Property(a => a.MythicRunId).HasColumnName("mythic_run_id");
                e.Property(a => a.AffixId).HasColumnName("affix_id");
                e.Property(a => a.Name).HasColumnName("name").HasMaxLength(128).IsRequired();
                e.Property(a => a.IconUrl).HasColumnName("icon_url").HasMaxLength(512);
                e.HasIndex(a => new { a.MythicRunId, a.AffixId }).IsUnique();

                e.HasOne(a => a.MythicRun)
                    .WithMany(r => r.Affixes)
                    .HasForeignKey(a => a.MythicRunId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.ToTable("raiderio_run_affixes");
            });
    }
}
