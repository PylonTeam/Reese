using Reese.Common.Replayer.ReplayHud.Shared.Drawers;
using ReLogic.Content;
using Terraria.GameContent;
using Terraria.Localization;
using static Reese.Common.Replayer.ReplayHud.Shared.Drawers.BiomeHelper;

namespace Reese.Core.Stats;

internal static class PlayerStats
{
    public static PlayerStatSnapshot Life(Player player)
    {
        string text = $"{player.statLife}/{player.statLifeMax2}";
        return new("Health", text, $"Health: {text}", TextureAssets.Heart, null);
    }

    public static PlayerStatSnapshot Mana(Player player)
    {
        string text = $"{player.statMana}/{player.statManaMax2}";
        return new("Mana", text, $"Mana: {text}", TextureAssets.Mana, null);
    }

    public static PlayerStatSnapshot Biome(Player player)
    {
        PlayerBiomeVisual biome = BiomeHelper.GetBiomeVisual(player);
        string text = GetBiomeText(biome);
        Asset<Texture2D> icon = Ass.IconBiome;
        Rectangle? iconFrame = null;

        if (BiomeHelper.TryGetBestiaryIconDrawData(biome.BestiaryBiome, out Asset<Texture2D> texture, out Rectangle source))
        {
            icon = texture;
            iconFrame = source;
        }

        return new("Biome", text, $"Biome: {text}", icon, iconFrame);
    }

    private static string GetBiomeText(PlayerBiomeVisual biome)
    {
        if (biome.BestiaryBiome == BiomeHelper.ShimmerBiome)
            return "Aether";

        return Language.GetTextValue(biome.BestiaryBiome.GetDisplayNameKey());
    }
}

public readonly record struct PlayerStatSnapshot(
    string Label,
    string Text,
    string HoverText,
    Asset<Texture2D> Icon,
    Rectangle? IconFrame);