using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Reese.Core.Compat;

public static class DragonLensIntegration
{
    private static bool _isInitialized;
    private static MethodInfo _cachedAdminMethod;

    private static bool GetDragonLensMod(out Mod dragonLensMod)
    {
        if (!ModLoader.TryGetMod("DragonLens", out dragonLensMod))
        {
            Log.Error("DragonLens mod not found.");
            return false;
        }
        return true;
    }

    private static bool GetPermissionHandlerClass(Mod mod)
    {
        Type permissionHandlerType = mod.Code.GetType("DragonLens.Core.Systems.PermissionHandler");
        if (permissionHandlerType == null)
        {
            Log.Error("DragonLens.Core.Systems.PermissionHandler class not found.");
        }
        return permissionHandlerType != null;
    }

    private static bool GetLooksLikeAdminMethod(Mod mod, out MethodInfo looksLikeAdminMethod)
    {
        if (!GetPermissionHandlerClass(mod))
        {
            looksLikeAdminMethod = null;
            return false;
        }

        looksLikeAdminMethod = mod.Code.GetType("DragonLens.Core.Systems.PermissionHandler")?.GetMethod("LooksLikeAdmin", BindingFlags.Public | BindingFlags.Static);
        return looksLikeAdminMethod != null;
    }

    public static bool IsPlayerDragonLensAdmin(Player player)
    {
        if (!_isInitialized)
        {
            _isInitialized = true;
            if (GetDragonLensMod(out Mod dragonLensMod))
            {
                GetLooksLikeAdminMethod(dragonLensMod, out _cachedAdminMethod);
            }
        }

        if (_cachedAdminMethod == null)
        {
            return false;
        }

        // Invoke the static method. 
        // First argument is null because it's a static method (no instance).
        // Second argument is an array of the parameters the method requires.
        object result = _cachedAdminMethod.Invoke(null, [player]);

        if (result is bool isAdmin)
        {
            return isAdmin;
        }

        return false;
    }
}