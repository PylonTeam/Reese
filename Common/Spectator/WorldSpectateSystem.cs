using Mono.Cecil.Cil;
using MonoMod.Cil;
using Reese.Common.Replayer.ReplayHud.ReplaySpectate;
using System;
using System.Collections.Generic;
using System.Reflection;
using Terraria.UI;

namespace Reese.Common.Spectator;

/// <summary>Uses native player hover text and floating heads to select a spectating target.</summary>
[Autoload(Side = ModSide.Client)]
internal sealed class WorldSpectateSystem : ModSystem
{
    private static Player hoveredHead;
    private static Vector2 hoveredHeadCenter;
    private static float hoveredHeadSize;

    public override void Load() => IL_Main.DrawMouseOver += AppendNativePlayerHoverHint;

    public override void Unload()
    {
        IL_Main.DrawMouseOver -= AppendNativePlayerHoverHint;
        BeginHeadHoverFrame();
    }

    public override void OnWorldUnload() => BeginHeadHoverFrame();

    internal static void BeginHeadHoverFrame() => hoveredHead = null;

    internal static bool TryHoverHead(Player target, Vector2 center, float size)
    {
        if (!CanInteract(target, requireFreeCursor: false) ||
            !ReplayDrawGate.ShouldDrawNameplate(target, SpectatorMode.IsSpectator(target)) || !ContainsMouse(center, size))
            return false;

        // Player names draw before inventory and HUD layers. Defer input until those
        // layers have had a chance to claim the mouse, so a head cannot click through UI.
        hoveredHead = target;
        hoveredHeadCenter = center;
        hoveredHeadSize = size;
        return true;
    }

    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
    {
        int mouseOverIndex = layers.FindIndex(layer => layer.Name == "Vanilla: Mouse Over");
        if (mouseOverIndex < 0)
            return;

        layers.Insert(mouseOverIndex, new LegacyGameInterfaceLayer("Reese: World Spectate Hover", DrawHeadHover, InterfaceScaleType.UI));
    }

    private static bool DrawHeadHover()
    {
        Player target = hoveredHead;
        hoveredHead = null;

        if (!CanInteract(target) || !ReplayDrawGate.ShouldDrawNameplate(target, SpectatorMode.IsSpectator(target)) ||
            !ContainsMouse(hoveredHeadCenter, hoveredHeadSize))
            return true;

        Main.LocalPlayer.mouseInterface = true;
        TryStartSpectating(target);
        Main.instance.MouseText(Loc.Get("ReplayHud.Spectate.SpectatePlayer", target.name));
        Main.mouseText = true;
        return true;
    }

    private static bool ContainsMouse(Vector2 center, float size)
    {
        if (!(size > 0f) || !float.IsFinite(size) || !float.IsFinite(center.X) || !float.IsFinite(center.Y))
            return false;

        Vector2 mouse = Main.MouseScreen;
        float half = size * 0.5f;
        return mouse.X >= center.X - half && mouse.X < center.X + half &&
            mouse.Y >= center.Y - half && mouse.Y < center.Y + half;
    }

    private static bool CanInteract(Player target, bool requireFreeCursor = true)
    {
        Player local = Main.LocalPlayer;
        if (Main.dedServ || local?.active != true || !SpectatorMode.CanSpectate ||
            !Main.hasFocus || Main.gameMenu || Main.mapFullscreen || Main.ingameOptionsWindow ||
            Main.drawingPlayerChat || Main.editSign || Main.editChest || Main.blockInput || Main.blockMouse ||
            Main.playerInventory || Main.mouseItem?.IsAir == false || ConfigHelper.IsAnyConfigUIOpen() ||
            requireFreeCursor && local.mouseInterface)
            return false;

        return target?.active == true && target.whoAmI >= 0 && target.whoAmI < Main.maxPlayers &&
            ReferenceEquals(Main.player[target.whoAmI], target) && SpectatorTargetSystem.CanTarget(target.whoAmI);
    }

    private static void TryStartSpectating(Player target)
    {
        if (!Main.mouseLeft || !Main.mouseLeftRelease)
            return;

        SpectatorTargetSystem.ClearPreviewTarget();
        SpectatorTargetSystem.SetPlayerTarget(target.whoAmI);
        Main.mouseLeftRelease = false;
    }

    internal static string AppendPlayerHoverHint(string text, int playerIndex)
    {
        if (playerIndex < 0 || playerIndex >= Main.maxPlayers)
            return text;

        Player target = Main.player[playerIndex];
        if (!CanInteract(target) || !ReplayDrawGate.ShouldDrawPlayer(target) ||
            target.ghost && !ReplayDrawGate.ShouldDrawNameplate(target, SpectatorMode.IsSpectator(target)))
            return text;

        Main.LocalPlayer.mouseInterface = true;
        TryStartSpectating(target);
        return text + "\n" + Loc.Get("ReplayHud.Spectate.ClickToSpectate");
    }

    private static void AppendNativePlayerHoverHint(ILContext il)
    {
        IL.Edit(il, cursor =>
        {
            int playerIndex = -1;
            if (!cursor.TryGotoNext(MoveType.After,
                instruction => instruction.MatchLdsfld<Main>(nameof(Main.player)),
                instruction => instruction.MatchLdloc(out playerIndex),
                instruction => instruction.MatchLdelemRef(),
                instruction => instruction.MatchLdfld<Entity>(nameof(Entity.active))))
                throw new InvalidOperationException("Could not find the native player hover loop for world spectating.");

            MethodInfo drawHoverText = typeof(Main).GetMethod("MouseTextHackZoom", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null, types: [typeof(string), typeof(int), typeof(byte), typeof(string)], modifiers: null);
            if (drawHoverText is null || !cursor.TryGotoNext(MoveType.Before,
                instruction => instruction.MatchLdarg(0),
                instruction => instruction.MatchLdloc(out _),
                instruction => instruction.MatchLdcI4(0),
                instruction => instruction.MatchLdsfld<Main>(nameof(Main.player)),
                instruction => instruction.MatchLdloc(playerIndex),
                instruction => instruction.MatchLdelemRef(),
                instruction => instruction.MatchLdfld<Player>(nameof(Player.difficulty)),
                instruction => instruction.MatchLdnull(),
                instruction => instruction.MatchCall(drawHoverText)))
                throw new InvalidOperationException("Could not find the completed native player hover text for world spectating.");

            // Keep vanilla's name, HP, PvP label, zoom and difficulty arguments intact.
            // At this point the stack contains [Main instance, completed hover string].
            cursor.Index += 2;
            cursor.Emit(OpCodes.Ldloc, cursor.Body.Variables[playerIndex]);
            cursor.EmitDelegate<Func<string, int, string>>(AppendPlayerHoverHint);
        });
    }
}
