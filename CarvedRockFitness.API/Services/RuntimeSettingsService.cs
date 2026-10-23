namespace CarvedRockFitness.API.Services;

/// <summary>
/// Thread-safe in-memory cache of admin-togglable runtime settings. Initialised from the
/// persisted "AppSettings" DB row (falling back to appsettings) on startup, and kept here for
/// fast reads; writers (AdminSettingsController) must also persist via AppSettingsStore so the
/// value survives an app restart/redeploy.
/// </summary>
public class RuntimeSettingsService
{
    public const string DemoSuggestionsVisibleKey = "DemoSuggestionsVisible";

    private volatile bool _demoSuggestionsVisible;

    public bool DemoSuggestionsVisible
    {
        get => _demoSuggestionsVisible;
        set => _demoSuggestionsVisible = value;
    }
}
