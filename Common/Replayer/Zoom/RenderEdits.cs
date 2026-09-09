using Mono.Cecil.Cil;
using MonoMod.Cil;
using Mono.Cecil;
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
    private static int allocatedExtraOffscreenRange;
    private static bool reloadQueued;

    public override void Load()
    {
        On_Main.GetScreenOverdrawOffset += GetScreenOverdrawOffset;
        IL_Main.InitTargets_int_int += PatchRenderTargets;
        IL_Main.DrawBlack += PatchWorldBlackout;

        allocatedExtraOffscreenRange = 0;
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
        var presentation = Main.instance.GraphicsDevice.PresentationParameters;
        int requiredExtra = GetExtraOffscreenRange(presentation.BackBufferWidth, presentation.BackBufferHeight);
        if (allocatedExtraOffscreenRange == requiredExtra)
            return;

        ReloadRenderTargets();
    }

    public override void OnWorldUnload()
    {
        if (allocatedExtraOffscreenRange > 0)
            ReloadRenderTargets();
    }

    private static Point GetScreenOverdrawOffset(On_Main.orig_GetScreenOverdrawOffset orig)
    {
        return GetRenderTargetZoom() < 1f ? Point.Zero : orig();
    }

    private static void PatchRenderTargets(ILContext il)
    {
        IL.Edit(il, c =>
        {
            if (!c.TryGotoNext(MoveType.After, i => i.MatchStsfld<Main>("offScreenRange")))
                throw new InvalidOperationException("Could not find Main.offScreenRange assignment.");

            c.Emit(OpCodes.Ldarg_1);
            c.Emit(OpCodes.Ldarg_2);
            c.EmitDelegate(GetRenderTargetMaxSize);
            c.Emit(OpCodes.Stsfld, MainField("_renderTargetMaxSize", BindingFlags.Static | BindingFlags.NonPublic));

            c.Emit(OpCodes.Ldc_I4, 192);
            c.Emit(OpCodes.Ldarg_1);
            c.Emit(OpCodes.Ldarg_2);
            c.EmitDelegate(AllocateExtraOffscreenRange);
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

    private static int GetExtraOffscreenRange(int width, int height)
    {
        return ZoomRenderSizing.GetExtraOffscreenRange(width, height, GetRenderTargetZoom());
    }

    private static int AllocateExtraOffscreenRange(int width, int height)
    {
        allocatedExtraOffscreenRange = GetExtraOffscreenRange(width, height);
        return allocatedExtraOffscreenRange;
    }

    private static int GetRenderTargetMaxSize(int width, int height)
    {
        int vanillaMaxSize = Main.maxScreenW + 400 * Main.maxScreenW / 1920;
        int requiredSize = Math.Max(width, height) + 2 * (192 + GetExtraOffscreenRange(width, height));
        return Math.Max(vanillaMaxSize, requiredSize);
    }

    private static float GetRenderTargetZoom()
    {
        // Normal zoom needs normal targets, even while a ghost. Preallocating
        // for the minimum slider value costs several GB of world targets at 4K.
        return !Main.gameMenu && SpectatorMode.CanSpectate ? Math.Min(1f, CameraSystem.WorldZoom) : 1f;
    }

    private static FieldInfo MainField(string name, BindingFlags flags)
    {
        return typeof(Main).GetField(name, flags) ?? throw new MissingFieldException(typeof(Main).FullName, name);
    }

    private static void ReloadRenderTargets()
    {
        if (Main.dedServ || reloadQueued)
            return;

        reloadQueued = true;
        Main.QueueMainThreadAction(() =>
        {
            reloadQueued = false;
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
