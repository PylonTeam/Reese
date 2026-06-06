namespace Reese.Core.Localization;

/// <summary>
/// Loc: Short for Localization
/// A static helper class for retrieving localized text for ModReloader.
/// </summary>
public static class Loc
{
    private static string prefix = "Mods.Reese.";

    /// <summary>
    /// Gets the text for the given key from the Reese localization file.
    /// If no localization is found, the key itself is returned.
    /// Reference:
    /// https://github.com/ScalarVector1/DragonLens/blob/master/Helpers/LocalizationHelper.cs
    /// </summary>
    public static string Get(string key, params object[] args)
    {
        if (Terraria.Localization.Language.Exists($"{prefix}{key}"))
        {
            return Terraria.Localization.Language.GetTextValue($"{prefix}{key}", args);
        }
        else
        {
            string modifiedKey;
            if (key.StartsWith(prefix))
            {
                modifiedKey = key.Substring(prefix.Length);
            }
            else
            {
                modifiedKey = key;
            }
            return modifiedKey;
        }
    }
}
