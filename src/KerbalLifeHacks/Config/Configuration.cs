using BepInEx.Configuration;
using KerbalLifeHacks.Hacks;

namespace KerbalLifeHacks.Config;

/// <summary>
/// Configuration for life hacks.
/// </summary>
internal class Configuration
{
    private const string TogglesSection = "Toggle life hacks";

    private readonly ConfigFile _file;
    private readonly Dictionary<Type, ConfigEntry<bool>> _hacksEnabled = new();

    /// <summary>
    /// Creates a new config file object.
    /// </summary>
    /// <param name="configFile">The config file to use.</param>
    public Configuration(ConfigFile configFile)
    {
        _file = configFile;
    }

    /// <summary>
    /// Gets the toggle value for a hack class.
    /// </summary>
    /// <param name="type">The hack class type.</param>
    /// <returns>The toggle value for the hack class.</returns>
    public bool IsHackEnabled(Type type)
    {
        // If the toggle value for a hack class is already defined, we return it
        if (_hacksEnabled.TryGetValue(type, out var isEnabled))
        {
            return isEnabled.Value;
        }

        // Otherwise create a new config entry for the hack class and return its default value (true)
        var metadata = HackAttribute.GetForType(type);
        var configEntry = _file.Bind(TogglesSection, type.Name, metadata.IsEnabledByDefault, metadata.Name);
        _hacksEnabled.Add(type, configEntry);
        return configEntry.Value;
    }

    /// <summary>
    /// Binds a config entry to a hack class.
    /// </summary>
    /// <param name="hackType">The hack class type.</param>
    /// <param name="key">The config entry key.</param>
    /// <param name="defaultValue">The config entry default value.</param>
    /// <param name="description">The config entry description.</param>
    /// <typeparam name="T">The config entry type.</typeparam>
    /// <returns>The config entry.</returns>
    public ConfigEntry<T> BindHackValue<T>(Type hackType, string key, T defaultValue, string description = null)
    {
        return _file.Bind($"{hackType.Name} settings", key, defaultValue, description);
    }
}