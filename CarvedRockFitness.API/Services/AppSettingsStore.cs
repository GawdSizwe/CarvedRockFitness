using CarvedRockFitness.API.DbContexts;
using CarvedRockFitness.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace CarvedRockFitness.API.Services;

/// <summary>
/// Persists admin-togglable runtime settings to the "AppSettings" table so they survive
/// app restarts/redeploys. Falls back gracefully (logs a warning) if the table doesn't exist
/// yet, e.g. a local SQL Server dev DB that hasn't had the AddAppSettings migration applied.
/// </summary>
public class AppSettingsStore(CarvedRockFitnessDbContext db, ILogger<AppSettingsStore> logger)
{
    public async Task<bool?> TryGetBoolAsync(string key, CancellationToken ct = default)
    {
        try
        {
            var setting = await db.AppSettings.AsNoTracking().FirstOrDefaultAsync(s => s.Key == key, ct);
            return setting != null && bool.TryParse(setting.Value, out var value) ? value : null;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not read AppSettings[{Key}] — falling back to default", key);
            return null;
        }
    }

    public async Task SetBoolAsync(string key, bool value, CancellationToken ct = default)
    {
        try
        {
            var setting = await db.AppSettings.FirstOrDefaultAsync(s => s.Key == key, ct);
            if (setting == null)
            {
                db.AppSettings.Add(new AppSetting { Key = key, Value = value.ToString(), UpdatedAt = DateTime.UtcNow });
            }
            else
            {
                setting.Value = value.ToString();
                setting.UpdatedAt = DateTime.UtcNow;
            }
            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not persist AppSettings[{Key}] — value will not survive a restart", key);
        }
    }
}
