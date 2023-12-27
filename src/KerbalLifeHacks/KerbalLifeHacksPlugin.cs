using BepInEx;
using JetBrains.Annotations;
using KerbalLifeHacks.Config;
using KerbalLifeHacks.Hacks;
using SpaceWarp;
using SpaceWarp.API.Configuration;
using SpaceWarp.API.Mods;

namespace KerbalLifeHacks;

/// <summary>
/// The main plugin class of the mod.
/// </summary>
[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency(SpaceWarpPlugin.ModGuid, SpaceWarpPlugin.ModVer)]
public class KerbalLifeHacksPlugin : BaseSpaceWarpPlugin
{
    /// <summary>
    /// The GUID of the mod.
    /// </summary>
    [PublicAPI] public const string ModGuid = MyPluginInfo.PLUGIN_GUID;

    /// <summary>
    /// The name of the mod.
    /// </summary>
    [PublicAPI] public const string ModName = MyPluginInfo.PLUGIN_NAME;

    /// <summary>
    /// The version of the mod.
    /// </summary>
    [PublicAPI] public const string ModVer = MyPluginInfo.PLUGIN_VERSION;

    internal new static Configuration Config;

    private readonly List<BaseHack> _hacks = new();

    public override void OnPreInitialized()
    {
        Type[] types;
        try
        {
            types = typeof(KerbalLifeHacksPlugin).Assembly.GetTypes();
        }
        catch (Exception ex)
        {
            Logger.LogError($"Could not get types: ${ex.Message}");
            return;
        }

        Config = new Configuration(base.Config);

        foreach (var type in types)
        {
            if (type.IsAbstract || !type.IsSubclassOf(typeof(BaseHack)))
            {
                continue;
            }

            try
            {
                var isLoaded = LoadHack(type);
                Logger.LogInfo($"Hack {type.Name} is " + (isLoaded ? "enabled" : "disabled"));
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error loading hack {type.FullName}: {ex}");
            }
        }
    }

    private bool LoadHack(Type type)
    {
        if (!Config.IsHackEnabled(type))
        {
            return false;
        }

        var hack = gameObject.AddComponent(type) as BaseHack;
        if (hack == null)
        {
            throw new Exception($"Could not instantiate hack {type.Name}.");
        }

        hack.transform.parent = transform;
        _hacks.Add(hack);

        return true;
    }

    /// <summary>
    /// Runs when the mod is first initialized.
    /// </summary>
    public override void OnInitialized()
    {
        foreach (var hack in _hacks)
        {
            hack.OnInitialized();
        }
    }
}