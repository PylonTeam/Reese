using Reese.Common.Replay.ReplayHud.Shared.Drawers;
using Reese.Core.Stats;

namespace Reese.Common.Replay.ReplayHud.ReplaySpectate;

internal sealed class UINPCCard : UIEntityCard<NPC>
{
    public int NPCIndex => EntityIndex;

    public UINPCCard(int npcIndex, int listIndex, float scale = 1f) : base(npcIndex, listIndex, scale) { }

    protected override bool TryGetEntity(int index, out NPC npc)
    {
        npc = index >= 0 && index < Main.maxNPCs ? Main.npc[index] : null;
        return npc?.active == true;
    }

    protected override bool IsSelected(NPC npc)
    {
        return SpectatorTargetSystem.IsLockedTargeting(npc);
    }

    protected override string GetDisplayName(NPC npc)
    {
        return npc.FullName;
    }

    protected override Color GetTextColor(NPC npc)
    {
        return Color.White;
    }

    protected override Color GetDistanceColor(NPC npc)
    {
        return Color.LightGray;
    }

    protected override void DrawPreview(SpriteBatch sb, NPC npc, Rectangle area)
    {
        EntityDrawer.DrawNPCPreview(sb, npc, area);
    }

    protected override void DrawHeadIcon(SpriteBatch sb, NPC npc, Rectangle area)
    {
        // Custom artificially increase area for NPCs
        area.Inflate(6, 6);
        area.X += 6;

        EntityDrawer.DrawNPCPreview(sb, npc, area);
    }

    protected override void DrawStats(SpriteBatch sb, NPC npc, Rectangle stat, int statGap, float scale)
    {
        StatDrawer.DrawNPCStat(sb, stat, NPCStats.Life(npc), scale);
        stat = NextStat(stat, statGap);

        StatDrawer.DrawNPCStat(sb, stat, NPCStats.Damage(npc), scale);
        stat = NextStat(stat, statGap);

        StatDrawer.DrawNPCStat(sb, stat, NPCStats.Defense(npc), scale);
    }

    internal static bool IsValidNPC(int npcIndex)
    {
        return npcIndex >= 0 && npcIndex < Main.maxNPCs && Main.npc[npcIndex]?.active == true;
    }
}
