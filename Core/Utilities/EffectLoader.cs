using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reese.Core.Utilities;

public class EffectLoader : ModSystem
{
    private const string GrayscalePath = "Reese/Assets/Effects/Grayscale";

    private static readonly Lazy<Effect> GrayscaleEffect = new(LoadGrayscaleEffect);

    public static bool TryGetGrayscaleEffect(out Effect effect)
    {
        try
        {
            effect = GrayscaleEffect.Value;
            return effect != null;
        }
        catch (Exception e)
        {
            Log.Warn($"Failed to load grayscale effect '{GrayscalePath}': {e.Message}");
            effect = null;
            return false;
        }
    }

    private static Effect LoadGrayscaleEffect()
    {
        try
        {
            return ModContent.Request<Effect>(GrayscalePath, AssetRequestMode.ImmediateLoad).Value;
        }
        catch (Exception e)
        {
            Log.Warn($"Failed to load grayscale effect '{GrayscalePath}': {e.Message}");
            return null;
        }
    }
}

