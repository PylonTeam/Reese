using ReLogic.Content;
using System;
using Terraria.GameContent;
using Terraria.ID;

namespace Reese.Core.Stats;

internal static class NPCStats
{
    public static NPCStatSnapshot Life(NPC npc)
    {
        string text = $"{Math.Max(0, npc.life)}/{Math.Max(1, npc.lifeMax)}";
        return new("Health", text, $"Health: {text}", TextureAssets.Heart, null);
    }

    public static NPCStatSnapshot Damage(NPC npc)
    {
        string text = npc.damage.ToString();
        return new("Damage", text, $"Damage: {text}", Ass.IconSword, null);
    }

    public static NPCStatSnapshot Defense(NPC npc)
    {
        string text = npc.defense.ToString();
        return new("Defense", text, $"Defense: {text}", TextureAssets.Extra[ExtrasID.DefenseShield], null);
    }
}

internal readonly record struct NPCStatSnapshot(
    string Label,
    string Text,
    string HoverText,
    Asset<Texture2D> Icon,
    Rectangle? IconFrame);