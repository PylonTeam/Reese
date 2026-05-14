using System;
using System.Reflection;
using Terraria.UI;

namespace Reese.Core.Utilities;

public static class ConfigHelper
{
    private static bool initialized;
    private static FieldInfo currentStateField;
    private static Type modConfigType;
    private static Type modConfigListType;

    public static bool IsAnyConfigUIOpen()
    {
        if (Main.ingameOptionsWindow)
            return true;

        if (!TryInitialize())
            return false;

        object state = currentStateField?.GetValue(Main.InGameUI);
        return state != null && (modConfigType?.IsInstanceOfType(state) == true || modConfigListType?.IsInstanceOfType(state) == true);
    }

    private static bool TryInitialize()
    {
        if (initialized)
            return currentStateField != null && modConfigType != null && modConfigListType != null;

        initialized = true;

        try
        {
            currentStateField = typeof(UserInterface).GetField("_currentState", BindingFlags.Instance | BindingFlags.NonPublic);
            modConfigType = typeof(Mod).Assembly.GetType("Terraria.ModLoader.Config.UI.UIModConfig");
            modConfigListType = typeof(Mod).Assembly.GetType("Terraria.ModLoader.Config.UI.UIModConfigList");

            if (currentStateField == null || modConfigType == null || modConfigListType == null)
            {
                Log.Warn("Failed to initialize config UI helper reflection");
                return false;
            }

            return true;
        }
        catch (Exception e)
        {
            Log.Warn("Failed to initialize config UI helper reflection: " + e);
            return false;
        }
    }
}