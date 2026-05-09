using Microsoft.Xna.Framework.Graphics;
using Reese.Common.ReplaySpectate.SpectatorMode;
using Reese.Common.ReplaySpectate.UI.Tabs.World;
using System;
using System.Collections.Generic;
using Terraria.GameContent;
using Terraria.Localization;

namespace Reese.Common.ReplaySpectate.UI.Tabs.World.WorldSections;

internal sealed class WorldInfoSection : WorldSectionBase
{
    public override WorldSection Section => WorldSection.WorldInfo;
    public override string HeaderText => "World info";
    public override float Height => 354+30f; // one row is 30!
    public override bool UsesCommonRowTooltips => true;

    public override IReadOnlyList<WorldSectionRow> GetRows()
    {
        return
        [
            new(GetWorldNameText(), GetWorldNameText, GetWorldSignTexture, iconScale: 1.3f),
            new(GetWorldSizeText(), GetWorldSizeText, GetWorldSizeIcon, iconScale: 1.3f),
            new(GetDifficultyText(), GetDifficultyText, GetWorldDifficultyIcon, GetDifficultyColor, iconScale: 1.3f),
            new(GetEvilText(), GetEvilText, GetWorldEvilIcon, GetEvilColor, iconScale: 1.2f),
            new(GetSeedText(), GetSeedText, GetWorldSeedIcon, iconScale: 1.3f),
            new(GetTimeText(), GetTimeText, GetTimeIcon, iconScale: 0.65f),
            new(GetWeatherText(), GetWeatherText, GetWeatherIcon, iconScale: 0.68f),
            new(GetMoonText(), GetMoonText, GetMoonIcon, iconScale: 0.75f),
            new(GetPlayersOnlineText(), GetPlayersOnlineText, GetPlayersOnlineTexutre, iconScale: 1.0f),
            new(GetSpectatorsOnlineText(), GetSpectatorsOnlineText, GetSpectatorsOnlineTexutre, iconScale: 0.75f),
        ];
    }

    private string GetSpectatorsOnlineText()
    {
        return "Spectators Online: " + SpectatorModeSystem.GetSpectatorCount().ToString();
    }

    private string GetPlayersOnlineText()
    {
        return "Players Online: " + SpectatorModeSystem.GetPlayersOnlineCount().ToString();
    }

    private static Texture2D GetPlayersOnlineTexutre()
    {
        return Ass.Icon_PlayerHead.Value;
    }

    private static Texture2D GetSpectatorsOnlineTexutre()
    {
        return Ass.GhostRight.Value;
    }

    private static Texture2D GetWorldSignTexture()
    {
        return Main.Assets.Request<Texture2D>("Images/UI/WorldCreation/IconRandomName").Value;
    }

    private static string GetWorldNameText()
    {
        return $"Name: {Main.worldName}";
    }

    private static Texture2D GetWorldSizeIcon()
    {
        string path = Main.maxTilesX switch
        {
            <= 4200 => "Images/UI/WorldCreation/IconSizeSmall",
            <= 6400 => "Images/UI/WorldCreation/IconSizeMedium",
            _ => "Images/UI/WorldCreation/IconSizeLarge"
        };

        return Main.Assets.Request<Texture2D>(path).Value;
    }

    private static string GetWorldSizeText()
    {
        if (Main.maxTilesX <= 4200)
            return "Size: Small";

        if (Main.maxTilesX <= 6400)
            return "Size: Medium";

        if (Main.maxTilesX <= 8400)
            return "Size: Large";

        return "Size: Custom";
    }

    private static Texture2D GetWorldDifficultyIcon()
    {
        string path = Main.GameMode switch
        {
            1 => "Images/UI/WorldCreation/IconDifficultyExpert",
            2 => "Images/UI/WorldCreation/IconDifficultyMaster",
            3 => "Images/UI/WorldCreation/IconDifficultyCreative",
            _ => "Images/UI/WorldCreation/IconDifficultyNormal"
        };

        return Main.Assets.Request<Texture2D>(path).Value;
    }

    private static string GetDifficultyText()
    {
        int mode = GetEffectiveDifficultyMode();

        if (mode == 0)
            return "Difficulty: Classic";

        if (mode == 1)
            return "Difficulty: Expert";

        if (mode == 2)
            return "Difficulty: Master";

        if (mode == 3)
            return "Difficulty: Journey";

        if (mode == 4)
            return "Difficulty: Legendary";

        return $"Difficulty: Mode {mode}";
    }

    private static Color GetDifficultyColor()
    {
        int mode = GetEffectiveDifficultyMode();

        if (mode == 1)
            return Main.mcColor;

        if (mode == 2)
            return Main.hcColor;

        if (mode == 3)
            return Main.creativeModeColor;

        if (mode == 4)
            return Main.hcColor;

        return Color.White;
    }

    private static Texture2D GetWorldEvilIcon()
    {
        return Main.Assets.Request<Texture2D>(WorldGen.crimson ? "Images/UI/WorldCreation/IconEvilCrimson" : "Images/UI/WorldCreation/IconEvilCorruption").Value;
    }

    private static string GetEvilText()
    {
        return $"Evil: {(WorldGen.crimson ? "Crimson" : "Corruption")}";
    }

    private static Color GetEvilColor()
    {
        return WorldGen.crimson ? new Color(255, 120, 120) : new Color(170, 120, 255);
    }

    private static Texture2D GetWorldSeedIcon()
    {
        return Main.Assets.Request<Texture2D>("Images/UI/WorldCreation/IconRandomSeed").Value;
    }

    private static string GetSeedText()
    {
        string seed = Main.ActiveWorldFileData?.SeedText;
        return $"Seed: {(string.IsNullOrWhiteSpace(seed) ? "-" : seed)}";
    }

    private static Texture2D GetTimeIcon()
    {
        return TextureAssets.InfoIcon[0].Value;
    }

    private static Texture2D GetWeatherIcon()
    {
        return TextureAssets.InfoIcon[1].Value;
    }

    private static string GetTimeText()
    {
        string amPm = Language.GetTextValue("GameUI.TimeAtMorning");
        double time = Main.time;
        if (!Main.dayTime)
            time += 54000.0;

        time = time / 86400.0 * 24.0;
        time = time - 7.5 - 12.0;
        if (time < 0.0)
            time += 24.0;

        if (time >= 12.0)
            amPm = Language.GetTextValue("GameUI.TimePastMorning");

        int hoursString = (int)time;
        double secondRemainder = time - hoursString;
        secondRemainder = (int)(secondRemainder * 60.0);
        string minutesString = secondRemainder.ToString();
        if (secondRemainder < 10.0)
            minutesString = "0" + minutesString;

        if (hoursString > 12)
            hoursString -= 12;

        if (hoursString == 0)
            hoursString = 12;

        return Language.GetTextValue("CLI.Time", hoursString + ":" + minutesString + " " + amPm);
    }

    private static string GetWeatherText()
    {
        string name = GetWeatherName();
        int wind = (int)Math.Round(Math.Abs(Main.windSpeedCurrent) * 60f);
        string direction = Main.windSpeedCurrent >= 0f ? "E" : "W";

        return $"Weather: {name} ({wind} mph {direction})";
    }

    private static Texture2D GetMoonIcon()
    {
        int index = (Main.bloodMoon && !Main.dayTime) || (Main.eclipse && Main.dayTime) ? 8 : 7;
        return TextureAssets.InfoIcon[index].Value;
    }

    private static string GetMoonText()
    {
        string name = Main.moonPhase switch
        {
            0 => "Full Moon",
            1 => "Waning Gibbous",
            2 => "Third Quarter",
            3 => "Waning Crescent",
            4 => "New Moon",
            5 => "Waxing Crescent",
            6 => "First Quarter",
            _ => "Waxing Gibbous"
        };

        return $"Moon Phase: {name} ({Main.moonPhase + 1}/8)";
    }

    private static int GetEffectiveDifficultyMode()
    {
        int modeNumber = Main.GameMode;

        if (Main.getGoodWorld && modeNumber > 0)
            modeNumber++;

        return modeNumber;
    }

    private static string GetWeatherName()
    {
        if (Main.maxRaining >= 0.6f)
            return "Heavy Rain";

        if (Main.maxRaining >= 0.2f)
            return "Rain";

        if (Main.maxRaining > 0f)
            return "Light Rain";

        if (Main.cloudAlpha >= 0.8f)
            return "Overcast";

        if (Main.cloudAlpha >= 0.6f)
            return "Mostly Cloudy";

        if (Main.cloudAlpha >= 0.35f)
            return "Cloudy";

        if (Main.cloudAlpha >= 0.15f)
            return "Partly Cloudy";

        return "Clear";
    }
}
