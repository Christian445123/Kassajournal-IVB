using Kassajournal.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace Kassajournal.Data.Local;

/// <summary>
/// Lokale, verschlüsselte SQLite-Datenbank (SQLCipher). Das ist der primäre Speicher der App:
/// Alle Eingaben landen zuerst hier (funktioniert immer, auch ohne Internet/Server-Verbindung)
/// und werden danach von <see cref="Kassajournal.Data.Sync.SyncService"/> mit der Zentraldatenbank abgeglichen.
/// </summary>
public class LocalDbContext(DbContextOptions<LocalDbContext> options) : DbContext(options)
{
    public DbSet<KassaEntry> Entries => Set<KassaEntry>();

    public DbSet<MonthSettings> MonthSettings => Set<MonthSettings>();

    public DbSet<IvbEntry> IvbEntries => Set<IvbEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<KassaEntry>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Date).IsRequired();
            entity.Property(e => e.Category).IsRequired();
            entity.Property(e => e.Amount).HasColumnType("TEXT"); // SQLite: decimal wird als TEXT exakt gespeichert
            entity.HasIndex(e => e.Date);
        });

        modelBuilder.Entity<MonthSettings>(entity =>
        {
            entity.HasKey(m => new { m.Year, m.Month });
        });

        modelBuilder.Entity<IvbEntry>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Date).IsRequired();
            entity.Property(e => e.Amount).HasColumnType("TEXT");
            entity.HasIndex(e => e.Date);
        });
    }
}
