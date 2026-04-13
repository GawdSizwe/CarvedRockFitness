namespace CarvedRockFitness.API.Services;

/// <summary>
/// Thread-safe in-memory runtime settings that can be toggled via the admin API.
/// Initialised from appsettings on startup.
/// </summary>
public class RuntimeSettingsService
{
    private volatile bool _demoSuggestionsVisible;

    public bool DemoSuggestionsVisible
    {
        get => _demoSuggestionsVisible;
        set => _demoSuggestionsVisible = value;
    }
}
