using ReLogic.Content;
using System;
using System.Collections.Generic;
using Terraria.GameContent;
using Terraria.ID;

namespace Reese.Core.Stats;

internal static class NPCStats
{
    public static readonly NPCStatDefinition NPCName = new("NPCName", "NPC", TextureAssets.MagicPixel, npc => npc.FullName, npc => $"NPC: {npc.FullName}");
    public static readonly NPCStatDefinition Life = new("Life", "Health", TextureAssets.Heart, npc => $"{Math.Max(0, npc.life)}/{Math.Max(1, npc.lifeMax)}");
    public static readonly NPCStatDefinition Damage = new("Damage", "Damage", Ass.IconSword, npc => npc.damage.ToString());
    public static readonly NPCStatDefinition Defense = new("Defense", "Defense", TextureAssets.Extra[ExtrasID.DefenseShield], npc => npc.defense.ToString());
    public static readonly NPCStatDefinition KnockbackResist = new("Knockback", "Knockback Resist", TextureAssets.Item[ItemID.CobaltShield], npc => $"{npc.knockBackResist * 100f:0}%");
    public static readonly NPCStatDefinition Velocity = new("Velocity", "Velocity", TextureAssets.Item[ItemID.Aglet], npc => $"{npc.velocity.Length():0.0}");
    public static readonly NPCStatDefinition Friendly = new("Friendly", "Friendly", TextureAssets.Item[ItemID.PeaceCandle], GetFriendlyText, npc => $"Friendly: {GetFriendlyText(npc)}");
    public static readonly NPCStatDefinition Boss = new("Boss", "Boss", TextureAssets.Item[ItemID.SuspiciousLookingEye], GetBossText, npc => $"Boss: {GetBossText(npc)}");
    public static readonly NPCStatDefinition Position = new("Position", "Position", TextureAssets.Item[ItemID.Compass], npc => $"{npc.position.X / 16f:0}, {npc.position.Y / 16f:0}");
    public static readonly NPCStatDefinition WhoAmI = new("WhoAmI", "ID", TextureAssets.Item[ItemID.Cog], npc => npc.whoAmI.ToString(), npc => $"whoAmI: {npc.whoAmI}");

    public static readonly IReadOnlyList<NPCStatDefinition> All =
    [
        NPCName,
        WhoAmI,
        Life,
        Damage,
        Defense,
        KnockbackResist,
        Velocity,
        Friendly,
        Boss,
        Position
    ];

    private static string GetFriendlyText(NPC npc)
    {
        if (npc.friendly)
            return "Yes";

        if (npc.townNPC || npc.isLikeATownNPC)
            return "Town";

        return "No";
    }

    private static string GetBossText(NPC npc) => npc.boss ? "Yes" : "No";
}

internal sealed class NPCStatDefinition(
    string id,
    string label,
    Asset<Texture2D> icon,
    Func<NPC, string> getText,
    Func<NPC, string> getHoverText = null,
    Rectangle? iconFrame = null)
{
    public string Id { get; } = id;
    public string Label { get; } = label;
    public Asset<Texture2D> Icon { get; } = icon;
    public Rectangle? IconFrame { get; } = iconFrame;
    public Func<NPC, string> GetText { get; } = getText;
    public Func<NPC, string> GetHoverText { get; } = getHoverText;

    public NPCStatSnapshot Build(NPC npc)
    {
        string text = GetText(npc);
        string hoverText = GetHoverText == null ? $"{Label}: {text}" : GetHoverText(npc);

        return new NPCStatSnapshot(Label, text, hoverText, Icon, IconFrame);
    }
}

internal readonly record struct NPCStatSnapshot(string Label, string Text, string HoverText, Asset<Texture2D> Icon, Rectangle? IconFrame);

