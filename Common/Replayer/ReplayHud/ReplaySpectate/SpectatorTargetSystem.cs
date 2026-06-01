using Reese.Common.Spectator;
using System;
using System.Collections.Generic;
using Terraria.ID;

namespace Reese.Common.Replayer.ReplayHud.ReplaySpectate;

[Autoload(Side = ModSide.Client)]
public class SpectatorTargetSystem : ModSystem
{
    private static int target = -1;
    private static int npcTarget = -1;
    private static int previewTarget = -1;
    private static int cameraTarget = -1;

    private static bool autoSpectatedFirstPlayer;

    public static bool HasLockedTarget()
    {
        return SpectatorMode.CanSpectate && (CanTarget(target) || CanTargetNPC(npcTarget));
    }

    private static bool CanTarget(int playerId)
    {
        if (playerId < 0 || playerId >= Main.maxPlayers || playerId == Main.myPlayer)
            return false;

        Player p = Main.player[playerId];
        bool isPlaybackClient = SpectatorMode.IsReplayClient(p);
        bool isGhost = p.ghost;
        bool result = p.active && (!isPlaybackClient || isGhost);

        return result;
    }

    private static bool CanTargetNPC(int npcId)
    {
        return npcId >= 0 && npcId < Main.maxNPCs && Main.npc[npcId]?.active == true;
    }

    public static void SetPlayerTarget(int slot)
    {
        target = CanTarget(slot) ? slot : -1;
        npcTarget = -1;

        if (CanTarget(target))
            SnapLocalPlayerTo(Main.player[target].Center, Main.player[target].direction);
    }

    public static void SetNPCTarget(int slot)
    {
        npcTarget = CanTargetNPC(slot) ? slot : -1;
        target = -1;
        previewTarget = -1;

        if (CanTargetNPC(npcTarget))
            SnapLocalPlayerTo(Main.npc[npcTarget].Center, GetNPCDirection(Main.npc[npcTarget]));
    }

    public static void TogglePlayerTarget(int slot, bool moveCameraToLocal = true)
    {
        if (target == slot)
            ClearTarget(moveCameraToLocal);
        else
            SetPlayerTarget(slot);
    }

    public static void ToggleNPCTarget(int slot, bool moveCameraToLocal = true)
    {
        if (npcTarget == slot)
            ClearTarget(moveCameraToLocal);
        else
            SetNPCTarget(slot);
    }

    public static void ResetForReplayStart()
    {
        target = -1;
        npcTarget = -1;
        previewTarget = -1;
        cameraTarget = -1;
        autoSpectatedFirstPlayer = false;
        SpectateCameraFade.Reset();
    }

    public static void PreserveTargetForSeek()
    {
        previewTarget = -1;
        cameraTarget = -1;
        autoSpectatedFirstPlayer = true;
        SpectateCameraFade.Reset();
    }

    public static void SetPreviewTarget(int slot)
    {
        previewTarget = CanTarget(slot) ? slot : -1;
    }

    public static void ClearPreviewTarget()
    {
        previewTarget = -1;
    }

    public static List<int> GetTargets(int exclude = -1)
    {
        List<int> targets = [];

        for (int i = 0; i < Main.maxPlayers; i++)
        {
            if (CanTarget(i) && i != exclude)
                targets.Add(i);
        }

        return targets;
    }

    public static void ClearTarget(bool moveCameraToLocal = true)
    {
        if (target == -1 && npcTarget == -1)
            return;

        target = -1;
        npcTarget = -1;

        if (CanTarget(previewTarget))
            return;

        cameraTarget = -1;

        Player local = Main.LocalPlayer;
        if (moveCameraToLocal && local?.active == true)
            SpectateCameraFade.SetScreenPosition(ClampScreenPosition(local.Center - new Vector2(Main.screenWidth, Main.screenHeight) * 0.5f), allowFade: true);
    }

    public static bool IsTargeting(Player player)
    {
        return player?.active == true && GetPlayerTarget()?.whoAmI == player.whoAmI;
    }

    public static bool IsLockedTargeting(Player player)
    {
        return player?.active == true && CanTarget(target) && target == player.whoAmI;
    }

    public static bool IsLockedTargeting(NPC npc)
    {
        return npc?.active == true && CanTargetNPC(npcTarget) && npcTarget == npc.whoAmI;
    }

    public static Player GetPlayerTarget()
    {
        if (!SpectatorMode.CanSpectate)
            return null;

        if (CanTarget(previewTarget))
            return Main.player[previewTarget];

        if (CanTarget(target))
            return Main.player[target];

        return null;
    }

    public static Player GetLockedPlayerTarget()
    {
        bool inSpectateMode = SpectatorMode.CanSpectate;
        bool canTarget = CanTarget(target);
        return inSpectateMode && canTarget ? Main.player[target] : null;
    }

    public static NPC GetLockedNPCTarget()
    {
        return SpectatorMode.CanSpectate && CanTargetNPC(npcTarget) ? Main.npc[npcTarget] : null;
    }

    public static string GetLockedTargetStatusText()
    {
        if (GetLockedNPCTarget() is NPC npc)
            return $"Spectating \"{npc.FullName}\"";

        if (GetLockedPlayerTarget() is Player player)
            return $"Spectating {player.name}";

        return null;
    }

    public override void ModifyScreenPosition()
    {
        if (!TryGetCameraTarget(out Vector2 center, out int nextCameraTarget))
        {
            cameraTarget = -1;
            return;
        }

        bool targetChanged = cameraTarget != nextCameraTarget;
        cameraTarget = nextCameraTarget;

        Vector2 screenPosition = ClampScreenPosition(center - new Vector2(Main.screenWidth, Main.screenHeight) * 0.5f);
        SpectateCameraFade.SetScreenPosition(screenPosition, targetChanged);
    }

    public override void PostUpdatePlayers()
    {
        TryAutoSpectateFirstPlayer();

        if (ShouldCancelFollowFromUserInput())
        {
            ClearTarget();
            return;
        }

        if (GetLockedPlayerTarget() is Player player)
        {
            SnapLocalPlayerTo(player.Center, player.direction);
            return;
        }

        if (GetLockedNPCTarget() is NPC npc)
        {
            SnapLocalPlayerTo(npc.Center, GetNPCDirection(npc));
            return;
        }
    }

    private static bool ShouldCancelFollowFromUserInput()
    {
        if (!CanTarget(target) && !CanTargetNPC(npcTarget))
            return false;

        Player local = Main.LocalPlayer;

        if (local?.active != true)
            return false;

        return local.controlLeft ||
            local.controlRight ||
            local.controlUp ||
            local.controlDown ||
            Main.mouseRight && Main.mouseRightRelease && !local.mouseInterface;
    }

    private static void SnapLocalPlayerTo(Vector2 center, int direction)
    {
        Player local = Main.LocalPlayer;
        if (local?.active != true)
            return;

        int normalizedDirection = direction == 0 ? 1 : direction;

        local.Center = center;
        local.velocity = Vector2.Zero;
        local.direction = normalizedDirection;
        local.ghostDir = normalizedDirection;
        local.fallStart = (int)(local.position.Y / 16f);
    }

    private static int GetNPCDirection(NPC npc)
    {
        if (npc.spriteDirection != 0)
            return npc.spriteDirection;

        return npc.direction == 0 ? 1 : npc.direction;
    }

    private static bool TryGetCameraTarget(out Vector2 center, out int id)
    {
        if (GetLockedPlayerTarget() is Player player)
        {
            center = player.Center;
            id = player.whoAmI;
            return true;
        }

        if (GetLockedNPCTarget() is NPC npc)
        {
            center = npc.Center;
            id = Main.maxPlayers + npc.whoAmI;
            return true;
        }

        if (CanTarget(previewTarget))
        {
            center = Main.player[previewTarget].Center;
            id = Main.maxPlayers + Main.maxNPCs + previewTarget;
            return true;
        }

        center = Vector2.Zero;
        id = -1;
        return false;
    }

    private static Vector2 ClampScreenPosition(Vector2 screenPosition)
    {
        float maxX = Math.Max(0f, Main.maxTilesX * 16f - Main.screenWidth);
        float maxY = Math.Max(0f, Main.maxTilesY * 16f - Main.screenHeight);

        screenPosition.X = MathHelper.Clamp(screenPosition.X, 0f, maxX);
        screenPosition.Y = MathHelper.Clamp(screenPosition.Y, 0f, maxY);

        return screenPosition;
    }

    private static void TryAutoSpectateFirstPlayer()
    {
        if (autoSpectatedFirstPlayer || target != -1 || npcTarget != -1)
            return;

        if (!SpectatorMode.CanSpectate)
            return;

        for (int i = 0; i < Main.maxPlayers; i++)
        {
            if (!CanTarget(i))
                continue;

            autoSpectatedFirstPlayer = true;
            SetPlayerTarget(i);
            return;
        }
    }
}
