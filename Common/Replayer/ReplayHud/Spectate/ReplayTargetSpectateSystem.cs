using System;
using System.Collections.Generic;
using Terraria.ID;

namespace Reese.Common.Replayer.ReplayHud.Spectate;

[Autoload(Side = ModSide.Client)]
public class ReplayTargetSpectateSystem : ModSystem
{
    private static int target = -1;
    private static int npcTarget = -1;
    private static int previewTarget = -1;
    private static int cameraTarget = -1;

    private static bool autoSpectatedFirstPlayer;

    public static bool HasLockedTarget()
    {
        return ReplayMode.IsInReplayMode(Main.LocalPlayer) && (CanTarget(target) || CanTargetNPC(npcTarget));
    }

    private static bool CanTarget(int playerId)
    {
        return playerId >= 0 &&
            playerId < Main.maxPlayers &&
            playerId != Main.myPlayer &&
            Main.player[playerId].active &&
            (ReplayMode.IsInPlayerMode(Main.player[playerId]) || ReplayMode.IsInReplayMode(Main.player[playerId]) || Main.player[playerId].ghost);
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
            SnapLocalPlayerTo(Main.player[target].Center, Main.player[target].direction, forceFullSync: true);
    }

    public static void SetNPCTarget(int slot)
    {
        npcTarget = CanTargetNPC(slot) ? slot : -1;
        target = -1;
        previewTarget = -1;

        if (CanTargetNPC(npcTarget))
            SnapLocalPlayerTo(Main.npc[npcTarget].Center, GetNPCDirection(Main.npc[npcTarget]), forceFullSync: true);
    }

    public static void TogglePlayerTarget(int slot)
    {
        if (target == slot)
            ClearTarget();
        else
            SetPlayerTarget(slot);
    }

    public static void ToggleNPCTarget(int slot)
    {
        if (npcTarget == slot)
            ClearTarget();
        else
            SetNPCTarget(slot);
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
            CameraFadeEffect.SetScreenPosition(ClampScreenPosition(local.Center - new Vector2(Main.screenWidth, Main.screenHeight) * 0.5f), allowFade: true);
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
        if (!ReplayMode.IsInReplayMode(Main.LocalPlayer))
            return null;

        if (CanTarget(previewTarget))
            return Main.player[previewTarget];

        if (CanTarget(target))
            return Main.player[target];

        return null;
    }

    public static Player GetLockedPlayerTarget()
    {
        return ReplayMode.IsInReplayMode(Main.LocalPlayer) && CanTarget(target) ? Main.player[target] : null;
    }

    public static NPC GetLockedNPCTarget()
    {
        return ReplayMode.IsInReplayMode(Main.LocalPlayer) && CanTargetNPC(npcTarget) ? Main.npc[npcTarget] : null;
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
        CameraFadeEffect.SetScreenPosition(screenPosition, targetChanged);
    }

    public override void PostUpdatePlayers()
    {
        TryAutoSpectateFirstPlayer();

        if (ShouldCancelFollowFromMovementInput())
        {
            ClearTarget();
            return;
        }

        if (GetLockedPlayerTarget() is Player player)
        {
            SnapLocalPlayerTo(player.Center, player.direction, forceFullSync: false);
            return;
        }

        if (GetLockedNPCTarget() is NPC npc)
        {
            SnapLocalPlayerTo(npc.Center, GetNPCDirection(npc), forceFullSync: false);
            return;
        }
    }

    private static bool ShouldCancelFollowFromMovementInput()
    {
        if (!CanTarget(target) && !CanTargetNPC(npcTarget))
            return false;

        Player local = Main.LocalPlayer;
        return local?.active == true && (local.controlLeft || local.controlRight || local.controlUp || local.controlDown);
    }

    private static void SnapLocalPlayerTo(Vector2 center, int direction, bool forceFullSync)
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

        if (!ReplayMode.IsInReplayMode(Main.LocalPlayer))
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