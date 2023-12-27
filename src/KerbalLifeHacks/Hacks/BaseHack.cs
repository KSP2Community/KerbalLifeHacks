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
        Logger = new BepInExLogger(BepInEx.Logging.Logger.CreateLogSource($"KLH/{GetType().Name}"));
        Config = KerbalLifeHacksPlugin.Config;
        HarmonyInstance = new Harmony(GetType().FullName);
    }

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