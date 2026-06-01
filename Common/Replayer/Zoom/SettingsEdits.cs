using Mono.Cecil.Cil;
using MonoMod.Cil;
using Reese.Common.Replayer.ReplayHud;
using Reese.Common.Spectator;
using System;
using System.Reflection;

namespace Reese.Common.Replayer.Zoom;

[Autoload(Side = ModSide.Client)]
internal sealed class SettingsEdits : ModSystem
{
    private static readonly MethodInfo DrawValueBarMethod = typeof(IngameOptions).GetMethod(nameof(IngameOptions.DrawValueBar));

    public override void Load()
    {
        IL_IngameOptions.Draw += PatchDraw;
    }

    public override void Unload()
    {
        IL_IngameOptions.Draw -= PatchDraw;
    }

    private static void PatchDraw(ILContext il)
    {
        IL.Edit(il, c =>
        {
            PatchZoomSliderInput(c);
            PatchZoomSliderOutput(c);
        });
    }

    private static void PatchZoomSliderInput(ILCursor c)
    {
        if (!c.TryGotoNext(MoveType.After, i => i.MatchLdarg(1), i => i.Match(OpCodes.Ldloc_S), i => i.MatchLdsfld<Main>(nameof(Main.GameZoomTarget))))
            throw new InvalidOperationException("Could not find vanilla zoom slider input.");

        c.Emit(OpCodes.Pop);
        c.EmitDelegate(GetZoomValue);

        c.Remove();
        c.EmitDelegate(GetZoomMin);

        if (!c.TryGotoNext(MoveType.After, i => i.MatchSub()))
            throw new InvalidOperationException("Could not find vanilla zoom slider normalization.");

        c.EmitDelegate(GetZoomRange);
        c.Emit(OpCodes.Div);
        c.Emit(OpCodes.Ldc_R4, 0f);
        c.Emit(OpCodes.Ldc_R4, 1f);
        c.Emit(OpCodes.Call, typeof(MathHelper).GetMethod(nameof(MathHelper.Clamp), [typeof(float), typeof(float), typeof(float)]));
    }

    private static void PatchZoomSliderOutput(ILCursor c)
    {
        if (!c.TryGotoNext(MoveType.After, i => i.MatchLdcI4(0), i => i.MatchLdnull(), i => i.MatchCall(DrawValueBarMethod), i => i.Match(OpCodes.Stloc_S)))
            throw new InvalidOperationException("Could not find vanilla zoom slider output.");

        if (!c.TryGotoNext(MoveType.Before, i => i.MatchLdcR4(1f)))
            throw new InvalidOperationException("Could not find vanilla zoom output offset.");

        c.Remove();
        c.EmitDelegate(GetZoomRange);
        c.Emit(OpCodes.Mul);
        c.EmitDelegate(GetZoomMin);

        if (!c.TryGotoNext(MoveType.After, i => i.MatchStsfld<Main>(nameof(Main.GameZoomTarget))))
            throw new InvalidOperationException("Could not find vanilla zoom assignment.");

        c.Emit(OpCodes.Ldsfld, typeof(Main).GetField(nameof(Main.GameZoomTarget)));
        c.EmitDelegate(ReplayClientSettings.ImportReplayZoomFromGame);
    }

    private static float GetZoomRange()
    {
        return SpectatorMode.CanSpectate ? ReplayClientSettings.ReplayZoomMax - ReplayClientSettings.ReplayZoomMin : 1f;
    }

    private static float GetZoomMin()
    {
        return SpectatorMode.CanSpectate ? ReplayClientSettings.ReplayZoomMin : 1f;
    }

    private static float GetZoomValue()
    {
        return SpectatorMode.CanSpectate ? ReplayClientSettings.ReplayZoom : Main.GameZoomTarget;
    }
}
