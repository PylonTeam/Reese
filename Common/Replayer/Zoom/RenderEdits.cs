using Mono.Cecil.Cil;
using MonoMod.Cil;
using Mono.Cecil;
using Reese.Common.Replayer.ReplayHud;
using System;
using System.Reflection;
using Reese.Common.Spectator;

namespace Reese.Common.Replayer.Zoom;

/// <summary>
/// Edits rendering code to support zooming out beyond 100% while spectating.
/// This includes increasing the off-screen range for culling and removing the black borders that appear when zooming out.
/// </summary>
[Autoload(Side = ModSide.Client)]
public sealed class RenderEdits : ModSystem
{
    private bool wasSpectating;

    public override void Load()
    {
        On_Main.GetScreenOverdrawOffset += GetScreenOverdrawOffset;
        IL_Main.InitTargets_int_int += PatchRenderTargets;
        IL_Main.DrawBlack += PatchWorldBlackout;

        wasSpectating = SpectatorMode.CanSpectate;
        if (wasSpectating)
            ReloadRenderTargets();
    }

    public override void Unload()
    {
        On_Main.GetScreenOverdrawOffset -= GetScreenOverdrawOffset;
        IL_Main.InitTargets_int_int -= PatchRenderTargets;
        IL_Main.DrawBlack -= PatchWorldBlackout;

        ReloadRenderTargets();
        base.Unload();
    }

    public override void PostUpdateEverything()
    {
        bool isSpectating = SpectatorMode.CanSpectate;
        if (wasSpectating == isSpectating)
            return;

        wasSpectating = isSpectating;
        ReloadRenderTargets();
    }

    private static Point GetScreenOverdrawOffset(On_Main.orig_GetScreenOverdrawOffset orig)
    {
        return SpectatorMode.CanSpectate && ReplayClientSettings.ReplayZoom < 1f ? Point.Zero : orig();
    }

    private static void PatchRenderTargets(ILContext il)
    {
        IL.Edit(il, c =>
        {
            if (!c.TryGotoNext(MoveType.After, i => i.MatchStsfld<Main>("offScreenRange")))
                throw new InvalidOperationException("Could not find Main.offScreenRange assignment.");

            c.EmitDelegate(GetRenderTargetMaxSize);
            c.Emit(OpCodes.Stsfld, MainField("_renderTargetMaxSize", BindingFlags.Static | BindingFlags.NonPublic));

            c.Emit(OpCodes.Ldc_I4, 192);
            c.Emit(OpCodes.Ldarg_1);
            c.EmitDelegate(GetExtraOffscreenRange);
            c.Emit(OpCodes.Add);
            c.Emit(OpCodes.Stsfld, MainField("offScreenRange", BindingFlags.Static | BindingFlags.Public));
        });
    }

    private static void PatchWorldBlackout(ILContext il)
    {
        IL.Edit(il, c =>
        {
            ReplacePointFieldLocal(c, "X", 0, "black left edge");
            RemoveMaxTilesMinusPointField(c, "X", "black right edge");
            ReplacePointFieldLocal(c, "Y", 0, "black top edge");
            RemoveMaxTilesMinusPointField(c, "Y", "black bottom edge");
        });
    }

    private static void ReplacePointFieldLocal(ILCursor c, string fieldName, int value, string editName)
    {
        int pointLocalIndex = -1;
        int outputLocalIndex = -1;

        if (!c.TryGotoNext(MoveType.Before, i => i.MatchLdloc(out pointLocalIndex), i => i.MatchLdfld<Point>(fieldName), i => i.MatchStloc(out outputLocalIndex)))
            throw new InvalidOperationException($"Could not patch {editName}.");

        c.RemoveRange(3);
        c.Emit(OpCodes.Ldloc, c.Body.Variables[pointLocalIndex]);
        c.Emit(OpCodes.Ldfld, typeof(Point).GetField(fieldName));
        c.Emit(OpCodes.Ldc_I4, value);
        c.EmitDelegate(GetBlackoutStart);
        c.Emit(OpCodes.Stloc, c.Body.Variables[outputLocalIndex]);
    }

    private static void RemoveMaxTilesMinusPointField(ILCursor c, string fieldName, string editName)
    {
        FieldReference maxTilesField = null;
        int pointLocalIndex = -1;
        int outputLocalIndex = -1;

        if (!c.TryGotoNext(MoveType.Before, i => i.MatchLdsfld(out maxTilesField), i => i.MatchLdloc(out pointLocalIndex), i => i.MatchLdfld<Point>(fieldName), i => i.MatchSub(), i => i.MatchStloc(out outputLocalIndex)))
            throw new InvalidOperationException($"Could not patch {editName}.");

        c.RemoveRange(5);
        c.Emit(OpCodes.Ldsfld, maxTilesField);
        c.Emit(OpCodes.Ldloc, c.Body.Variables[pointLocalIndex]);
        c.Emit(OpCodes.Ldfld, typeof(Point).GetField(fieldName));
        c.EmitDelegate(GetBlackoutEnd);
        c.Emit(OpCodes.Stloc, c.Body.Variables[outputLocalIndex]);
    }

    private static int GetBlackoutStart(int vanillaValue, int replayValue)
    {
        return SpectatorMode.CanSpectate ? replayValue : vanillaValue;
    }

    private static int GetBlackoutEnd(int maxTiles, int overdrawOffset)
    {
        return SpectatorMode.CanSpectate ? maxTiles : maxTiles - overdrawOffset;
    }

    private static int GetExtraOffscreenRange(int dimension)
    {
        float zoom = GetRenderTargetZoom();
        return (int)(dimension * (1f / zoom - 1f) * 0.5f);
    }

    private static int GetRenderTargetMaxSize()
    {
        float zoom = GetRenderTargetZoom();
        return (int)(Main.maxScreenW / zoom) + 400 * Main.maxScreenW / 1920;
    }

    private static float GetRenderTargetZoom()
    {
        return SpectatorMode.CanSpectate ? Math.Min(1f, ReplayClientSettings.ReplayZoomMin) : 1f;
    }

    private static FieldInfo MainField(string name, BindingFlags flags)
    {
        return typeof(Main).GetField(name, flags) ?? throw new MissingFieldException(typeof(Main).FullName, name);
    }

    private static void ReloadRenderTargets()
    {
        if (Main.dedServ)
            return;

        Main.QueueMainThreadAction(() =>
        {
            MethodInfo initTargets = typeof(Main).GetMethod("InitTargets", BindingFlags.Instance | BindingFlags.NonPublic, null, [], null);
            FieldInfo busyField = MainField("_isResizingAndRemakingTargets", BindingFlags.Static | BindingFlags.NonPublic);

            if (initTargets == null)
            {
                Log.Chat("Could not reload zoom render targets: Main.InitTargets was not found.");
                return;
            }

            if ((bool)busyField.GetValue(null))
                return;

            try
            {
                busyField.SetValue(null, true);
                initTargets.Invoke(Main.instance, null);
            }
            catch (Exception e)
            {
                Log.Chat($"Could not reload zoom render targets: {e.Message}");
            }
            finally
            {
                busyField.SetValue(null, false);
            }
        });
    }
}
