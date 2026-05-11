using Microsoft.Xna.Framework.Graphics;
using Reese.Common.Replayer.ReplayHud.ReplaySpectate.Stats;
using Reese.Common.Replayer.ReplayHud.Shared.Sections;
using Reese.Common.Replayer.ReplayHud.Shared.Tabs;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.UI;

namespace Reese.Common.Replayer.ReplayHud.ReplayInfo;

internal sealed class WorldInfoTab : TabPage
{
    public override SpectatorTab Tab => SpectatorTab.World;
    public override string HeaderText => "World";
    public override string TooltipText => "World stats";
    public override Asset<Texture2D> Icon => Ass.Icon_World;
    public override float IconScale => 1f;
    public override Vector2 IconOffset => new Vector2(0, 0);

    protected override void Populate(UIList list)
    {
        AddSection(list, new WorldInfo());
        //AddSection(list, new MiscInfo());
        list.Add(new BossesDefeated());
    }

    private sealed class WorldInfo : InfoSection
    {
        public override string HeaderText => "World";
        public override float Height => 316f;

        public override IReadOnlyList<SpectatorSectionRow> GetRows()
        {
            return
            [
                new("World:", () => Main.worldName, GetWorldSignTexture, iconScale: 1.3f),
                new("Size:", GetWorldSizeText, GetWorldSizeIcon, iconScale: 1.3f),
                new("Difficulty:", GetDifficultyText, GetWorldDifficultyIcon, GetDifficultyColor, iconScale: 1.3f),
                new("Evil:", GetEvilText, GetWorldEvilIcon, GetEvilColor, iconScale: 1.2f),
                new("Seed:", GetSeedText, GetWorldSeedIcon, iconScale: 1.3f),
                new("Time:", GetTimeText, GetTimeIcon, iconScale: 0.65f),
                new("Weather:", GetWeatherText, GetWeatherIcon, iconScale: 0.68f),
                new("Moon:", GetMoonText, GetMoonIcon, iconScale: 0.75f)
            ];
        }

        private static Texture2D GetWorldSignTexture()
        {
            return Main.Assets.Request<Texture2D>("Images/UI/WorldCreation/IconRandomName").Value;
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
                return "Small";

            if (Main.maxTilesX <= 6400)
                return "Medium";

            if (Main.maxTilesX <= 8400)
                return "Large";

            return "Unknown";
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

    private sealed class MiscInfo : InfoSection
    {
        public override string HeaderText => "Players";
        public override float Height => 112f;

        public override IReadOnlyList<SpectatorSectionRow> GetRows()
        {
            return
            [
                new(GetPlayersOnlineText(), GetPlayersOnlineText, GetPlayersOnlineTexture, iconScale: 1f),
                new(GetSpectatorsOnlineText(), GetSpectatorsOnlineText, GetSpectatorsOnlineTexture, iconScale: 0.75f)
            ];
        }

        private static string GetPlayersOnlineText()
        {
            int count = 0;

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];

                if (player?.active == true && ReplayMode.IsInPlayerMode(player))
                    count++;
            }

            return "Players Online: " + count;
        }

        private static string GetSpectatorsOnlineText()
        {
            int count = 0;

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];

                if (ReplayMode.IsInReplayMode(player))
                    count++;
            }

            return "Spectators Online: " + count;
        }

        private static Texture2D GetPlayersOnlineTexture()
        {
            return Ass.Icon_PlayerHead.Value;
        }

        private static Texture2D GetSpectatorsOnlineTexture()
        {
            return Ass.GhostRight.Value;
        }
    }

    private sealed class BossesDefeated : UIPanel
    {
        private const float HeaderHeight = 34f;
        private const float RowHeight = 30f;
        private const float RowStep = 34f;
        private const int ContentInset = 7;
        private const int BossSlotStep = 43;
        private const int BossSlotSize = 38;

        public BossesDefeated()
        {
            Width.Set(0f, 1f);
            Height.Set(276f, 0f);
            SetPadding(0f);
            BackgroundColor = new Color(28, 36, 76) * 0.92f;
            BorderColor = new Color(116, 154, 255) * 0.75f;
        }

        protected override void DrawSelf(SpriteBatch sb)
        {
            base.DrawSelf(sb);

            Rectangle box = GetDimensions().ToRectangle();
            sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(box.X + 10, box.Y + 28, box.Width - 20, 2), Color.White * 0.10f);
            Utils.DrawBorderString(sb, "Bosses", new Vector2(box.X + 10, box.Y + 6), new Color(255, 228, 140), 0.9f);

            Rectangle statBox = new(box.X + ContentInset, box.Y + (int)(HeaderHeight + 4f), box.Width - ContentInset * 2, (int)RowHeight);
            string bossesDefeatedText = GetBossesDefeatedText();
            StatDrawer.DrawWorldStatPanel(sb, statBox, Ass.Icon_CheckmarkGreen.Value, bossesDefeatedText, $"Bosses Defeated: {bossesDefeatedText}", textColor: Color.Gray, label: "Bosses Defeated:");

            int dividerY = statBox.Bottom + 8;
            sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(box.X + 10, dividerY, box.Width - 20, 2), Color.White * 0.10f);

            DrawBossGrid(sb, box, dividerY + 12);
        }

        private void DrawBossGrid(SpriteBatch sb, Rectangle box, int gridY)
        {
            IReadOnlyList<BossEntry> bosses = GetBossEntries();
            int gridX = box.X + ContentInset + 2;
            int gridColumns = Math.Max(1, (box.Right - ContentInset - gridX + 8) / BossSlotStep);
            bool canShowHover = IsMouseInsideSpectatorInfoPanel();

            for (int i = 0; i < bosses.Count; i++)
            {
                int col = i % gridColumns;
                int row = i / gridColumns;
                BossEntry boss = bosses[i];
                Rectangle slot = new(gridX + col * BossSlotStep, gridY + row * BossSlotStep, BossSlotSize, BossSlotSize);

                Utils.DrawInvBG(sb, slot, new Color(83, 97, 168) * 0.80f);

                int headNpc = boss.HeadNpcId;
                if (headNpc >= 0 && headNpc < NPCID.Sets.BossHeadTextures.Length && NPCID.Sets.BossHeadTextures[headNpc] != -1)
                    Main.BossNPCHeadRenderer.DrawWithOutlines(null, NPCID.Sets.BossHeadTextures[headNpc], slot.Center.ToVector2(), boss.Downed ? Color.White : Color.White * 0.25f, 0f, 0.78f, SpriteEffects.None);

                if (boss.Downed)
                {
                    Texture2D checkTexture = Ass.Icon_CheckmarkGreen.Value;
                    sb.Draw(checkTexture, slot.Center.ToVector2(), null, Color.White, 0f, checkTexture.Size() * 0.5f, 1f, SpriteEffects.None, 0f);
                }

                if (canShowHover)
                    ShowHover(slot, $"{boss.Name} ({(boss.Downed ? "Downed" : "Not downed")})");
            }
        }

        private bool IsMouseInsideSpectatorInfoPanel()
        {
            for (UIElement element = this; element is not null; element = element.Parent)
            {
                if (element is InfoHud panel)
                    return panel.ContainsPoint(Main.MouseScreen);
            }

            return true;
        }

        private static void ShowHover(Rectangle area, string text)
        {
            if (!area.Contains(Main.MouseScreen.ToPoint()))
                return;

            Main.LocalPlayer.mouseInterface = true;
            Main.instance.MouseText(text);
        }

        private static IReadOnlyList<BossEntry> GetBossEntries()
        {
            int evilBossId;
            string evilBossName;

            if (WorldGen.crimson)
            {
                evilBossId = NPCID.BrainofCthulhu;
                evilBossName = "Brain of Cthulhu";
            }
            else
            {
                evilBossId = NPCID.EaterofWorldsHead;
                evilBossName = "Eater of Worlds";
            }

            return
            [
                new(NPCID.KingSlime, "King Slime", NPC.downedSlimeKing),
                new(NPCID.EyeofCthulhu, "Eye of Cthulhu", NPC.downedBoss1),
                new(evilBossId, evilBossName, NPC.downedBoss2),
                new(NPCID.QueenBee, "Queen Bee", NPC.downedQueenBee),
                new(NPCID.Deerclops, "Deerclops", NPC.downedDeerclops),
                new(NPCID.SkeletronHead, "Skeletron", NPC.downedBoss3),
                new(NPCID.WallofFlesh, "Wall of Flesh", Main.hardMode),
                new(NPCID.QueenSlimeBoss, "Queen Slime", NPC.downedQueenSlime),
                new(NPCID.TheDestroyer, "The Destroyer", NPC.downedMechBoss1),
                new(NPCID.Retinazer, "The Twins", NPC.downedMechBoss2),
                new(NPCID.SkeletronPrime, "Skeletron Prime", NPC.downedMechBoss3),
                new(NPCID.Plantera, "Plantera", NPC.downedPlantBoss),
                new(NPCID.Golem, "Golem", NPC.downedGolemBoss),
                new(NPCID.DukeFishron, "Duke Fishron", NPC.downedFishron),
                new(NPCID.HallowBoss, "Empress of Light", NPC.downedEmpressOfLight),
                new(NPCID.CultistBoss, "Lunatic Cultist", NPC.downedAncientCultist),
                new(NPCID.LunarTowerSolar, "Lunar Pillars", NPC.downedTowerSolar && NPC.downedTowerVortex && NPC.downedTowerNebula && NPC.downedTowerStardust),
                new(NPCID.MoonLordCore, "Moon Lord", NPC.downedMoonlord)
                ];
        }

        private static string GetBossesDefeatedText()
        {
            IReadOnlyList<BossEntry> bosses = GetBossEntries();
            int defeated = 0;

            foreach (BossEntry boss in bosses)
                defeated += boss.Downed ? 1 : 0;

            return $"{defeated}/{bosses.Count}";
        }

        private readonly struct BossEntry
        {
            public readonly int NpcId;
            public readonly string Name;
            public readonly bool Downed;

            public BossEntry(int npcId, string name, bool downed)
            {
                NpcId = npcId;
                Name = name;
                Downed = downed;
            }

            public int HeadNpcId => NpcId switch
            {
                NPCID.Golem => NPCID.GolemHead,
                NPCID.MoonLordCore => NPCID.MoonLordHead,
                _ => NpcId
            };
        }
    }
}