using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ProxyRouterWpf.Configuration
{
    /// <summary>
    /// Thread-safe holder for <see cref="AppConfig"/> with JSON persistence to a file next to the
    /// executable. All service mutations happen under <see cref="SyncRoot"/> and call <see cref="Save"/>.
    /// </summary>
    public sealed class ConfigStore
    {
        static readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() },
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        readonly string _path;
        readonly object _sync = new();
        AppConfig _config;

        public ConfigStore(string path)
        {
            _path = path;
            _config = LoadOrCreate();
        }

        /// <summary>Lock this before compound read-modify-write over <see cref="Config"/>.</summary>
        public object SyncRoot => _sync;

        public AppConfig Config => _config;

        AppConfig LoadOrCreate()
        {
            try
            {
                if (File.Exists(_path))
                {
                    var json = File.ReadAllText(_path);
                    var cfg = JsonSerializer.Deserialize<AppConfig>(json, _jsonOptions);
                    if (cfg != null)
                        return cfg;
                }
            }
            catch
            {
                // Corrupt/unreadable config → start fresh rather than crash.
            }
            return new AppConfig();
        }

        public void Save()
        {
            lock (_sync)
            {
                try
                {
                    var json = JsonSerializer.Serialize(_config, _jsonOptions);
                    var dir = Path.GetDirectoryName(_path);
                    if (!string.IsNullOrEmpty(dir))
                        Directory.CreateDirectory(dir);
                    File.WriteAllText(_path, json);
                }
                catch
                {
                    // Best-effort persistence; ignore IO errors (e.g. read-only folder).
                }
            }
        }
    }
}
