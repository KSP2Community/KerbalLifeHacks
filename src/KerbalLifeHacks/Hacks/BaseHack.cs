using BepInEx.Configuration;
using HarmonyLib;
using KerbalLifeHacks.Config;
using KSP.Game;
using SpaceWarp.API.Logging;

namespace KerbalLifeHacks.Hacks;

/// <summary>
/// Base class for all hacks.
/// </summary>
public abstract class BaseHack : KerbalMonoBehaviour
{
    private Configuration Config { get; }

    /// <summary>
    /// The logger for the hack.
    /// </summary>
    internal ILogger Logger { get; }

    /// <summary>
    /// The harmony instance for the hack.
    /// </summary>
    private protected Harmony HarmonyInstance { get; }

    /// <summary>
    /// Creates a new hack.
    /// </summary>
    private protected BaseHack()
    {
        Config = KerbalLifeHacksPlugin.Config;
        Logger = new BepInExLogger(BepInEx.Logging.Logger.CreateLogSource($"KLH/{GetType().Name}"));
        HarmonyInstance = new Harmony(GetType().FullName);
    }

    /// <summary>
    /// Binds a config entry to a hack class.
    /// </summary>
    /// <param name="key">The config entry key.</param>
    /// <param name="defaultValue">The config entry default value.</param>
    /// <param name="description">The config entry description.</param>
    /// <typeparam name="T">The config entry type.</typeparam>
    /// <returns>The config entry.</returns>
    private protected ConfigEntry<T> BindConfigValue<T>(string key, T defaultValue, string description = null)
    {
        return Config.BindHackValue(GetType(), key, defaultValue, description);
    }

    /// <summary>
    /// Called when the hack is initialized.
    /// </summary>
    public virtual void OnInitialized()
    {
    }
}