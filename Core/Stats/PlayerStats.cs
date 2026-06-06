using Reese.Common.Replayer.ReplayHud;
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
        return new(Language.GetTextValue("BestiaryInfo.Life"), text, Language.GetTextValue("LegacyInterface.0") + " " + text, TextureAssets.Heart, null);
    }

    public static PlayerStatSnapshot Mana(Player player)
    {
        string text = $"{player.statMana}/{player.statManaMax2}";
        return new(Language.GetTextValue("LegacyInterface.2"), text, Language.GetTextValue("LegacyInterface.2") + ": " + text, TextureAssets.Mana, null);
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

        return new(Loc.Get("ReplayHud.Spectate.Stats.Biome"), text, Loc.Get("ReplayHud.Spectate.Stats.BiomeText", text), icon, iconFrame);
    }

    private static string GetBiomeText(PlayerBiomeVisual biome)
    {
        if (biome.BestiaryBiome == BiomeHelper.ShimmerBiome)
            return Loc.Get("ReplayHud.Spectate.Biome.Aether");

        return Language.GetTextValue(biome.BestiaryBiome.GetDisplayNameKey());
    }
}

public readonly record struct PlayerStatSnapshot(
    string Label,
    string Text,
    string HoverText,
    Asset<Texture2D> Icon,
    Rectangle? IconFrame);
