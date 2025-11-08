using System.Text.Json;

namespace MiniHttpServer.Framework.Settings;

public class SettingsManager
{
    private static SettingsManager _instanse;

    private static readonly object _lock = new();

    public AppSettings Settings { get; private set; }

    private SettingsManager()
    {
        LoadSettings();
    }
    
    public static SettingsManager Instance
    {
        get
        {
            if (_instanse == null)
            {
                lock (_lock)
                {
                    if (_instanse == null)
                        _instanse = new SettingsManager();
                }
            }
            return _instanse;
        }
    }

    private void LoadSettings()
    {
        string path = "settings.json";
        if (!File.Exists(path))
            throw new FileNotFoundException("Settings file not found", path);

        string json = File.ReadAllText(path);

        Settings = JsonSerializer.Deserialize<AppSettings>(json)
                                ?? throw new InvalidOperationException("Failed to deserialize settings");
    }
}
