using System.Reflection;

namespace KerbalLifeHacks.Hacks;

/// <summary>
/// Attribute for all hacks.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class HackAttribute : Attribute
{
    /// <summary>
    /// The name of the hack displayed as description in the config file.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Whether the hack is enabled by default.
    /// </summary>
    public bool IsEnabledByDefault { get; }

    /// <summary>
    /// Creates a new hack attribute.
    /// </summary>
    /// <param name="name">The name of the hack displayed as description in the config file.</param>
    /// <param name="isEnabledByDefault">Whether the hack is enabled by default.</param>
    public HackAttribute(string name, bool isEnabledByDefault = true)
    {
        Name = name;
        IsEnabledByDefault = isEnabledByDefault;
    }

    /// <summary>
    /// Gets the instance of the hack attribute on a type.
    /// </summary>
    /// <param name="type">The type to get the hack attribute from.</param>
    /// <returns>The hack attribute instance.</returns>
    /// <exception cref="Exception">Thrown if the type does not have a hack attribute.</exception>
    internal static HackAttribute GetForType(Type type)
    {
        try
        {
            return type.GetCustomAttribute<HackAttribute>();
        }
        catch (Exception)
        {
            throw new Exception(
                $"The attribute {typeof(HackAttribute).FullName} has to be declared on the class {type.FullName}."
            );
        }
    }
}