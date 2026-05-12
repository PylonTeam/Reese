using Microsoft.CodeAnalysis;
using Reese.Common.Replayer.ReplayHud.ReplaySpectate.Stats;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.UI;

namespace Reese.Common.Replayer.ReplayHud.ReplayInfo;

internal sealed class ReplayInfoTab : TabPage
{
    public override Shared.Tabs.SpectatorTab Tab => Shared.Tabs.SpectatorTab.Replay;
    public override string HeaderText => "Replay";
    public override string TooltipText => "Replay info";
    public override Asset<Texture2D> Icon => Ass.Icon_CameraSmall;

    public override float IconScale => 1.25f;

    public override Vector2 IconOffset => new Vector2(0,0);

    protected override void Populate(UIList list)
    {
        AddSection(list, new ReplayInfo());
        list.Add(new ModsGrid());
    }

    private sealed class ReplayInfo : Shared.Sections.InfoSection
    {
        public override string HeaderText => "Replay Info";
        public override float Height => 222f;

        public override IReadOnlyList<Shared.Sections.SpectatorSectionRow> GetRows()
        {
            return
            [
                new("File:", GetFileText),
                new("Size:", GetFileSizeText),
                new("Recorded:", GetRecordedText),
                new("Length:", GetLengthText),
                new("World:", GetWorldText)
            ];
        }

        //private static ReplayMetadata Metadata => Replayer.ActiveMetadata;

        private static string GetFileText()
        {
            return string.IsNullOrWhiteSpace(ReplayPlayback.CurrentPath) ? "-" : Path.GetFileName(ReplayPlayback.CurrentPath);
        }

        private static string GetFileSizeText()
        {
            string path = ReplayPlayback.CurrentPath;

            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                return "-";

            long bytes = new FileInfo(path).Length;
            return FormatFileSizeText(bytes);
        }

        private static string GetRecordedText()
        {
            if (ReplayPlayback.Metadata == null || ReplayPlayback.Metadata.DateCreated == DateTime.MinValue)
                return "-";

            return ReplayPlayback.Metadata.DateCreated.ToString("d MMM yyyy HH:mm", CultureInfo.InvariantCulture);
        }

        private static string GetPlaybackText()
        {
            uint currentTick = ReplayPlayback.CurrentTick;
            uint durationTicks = GetDurationTicks();
            int tickRate = GetTickRate();

            if (durationTicks == 0)
                return FormatDuration(currentTick, tickRate);

            return $"{FormatDuration(currentTick, tickRate)} / {FormatDuration(durationTicks, tickRate)}";
        }

        private static string GetLengthText()
        {
            uint durationTicks = GetDurationTicks();
            return durationTicks == 0 ? "-" : FormatDuration(durationTicks, GetTickRate());
        }

        private static string GetWorldText()
        {
            return "-";
            //return EmptyToDash(Metadata?.WorldName);
        }

        private static uint GetDurationTicks()
        {
            return ReplayPlayback.DurationTicks;
        }

        private static int GetTickRate()
        {
            return 60;
        }

        private static string FormatDuration(uint ticks, int tickRate)
        {
            TimeSpan duration = TimeSpan.FromSeconds(ticks / (double)tickRate);

            return duration.TotalHours >= 1d
                ? $"{(int)duration.TotalHours:00}:{duration.Minutes:00}:{duration.Seconds:00}"
                : $"{duration.Minutes:00}:{duration.Seconds:00}";
        }

        private static string FormatFileSizeText(long bytes)
        {
            long kilobytes = Math.Max(1, (long)Math.Ceiling(bytes / 1024d));
            return kilobytes.ToString("N0", CultureInfo.InvariantCulture).Replace(",", " ") + " KB";
        }

        private static string EmptyToDash(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "-" : value.Trim();
        }
    }

    private sealed class ModsGrid : UIPanel
    {
        private const float HeaderHeight = 34f;
        private const float RowHeight = 30f;
        private const int ContentInset = 7;
        private const int ModSlotStep = 43;
        private const int ModSlotSize = 38;
        private const int MinimumPanelHeight = 276;

        private readonly IReadOnlyList<ModEntry> mods;

        public ModsGrid()
        {
            mods = GetModEntries();

            Width.Set(0f, 1f);
            Height.Set(GetPanelHeight(mods.Count), 0f);
            SetPadding(0f);
            BackgroundColor = new Color(28, 36, 76) * 0.92f;
            BorderColor = new Color(116, 154, 255) * 0.75f;
        }

        protected override void DrawSelf(SpriteBatch sb)
        {
            base.DrawSelf(sb);

            Rectangle box = GetDimensions().ToRectangle();
            sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(box.X + 10, box.Y + 28, box.Width - 20, 2), Color.White * 0.10f);
            Utils.DrawBorderString(sb, "Mods", new Vector2(box.X + 10, box.Y + 6), new Color(255, 228, 140), 0.9f);

            Rectangle statBox = new(box.X + ContentInset, box.Y + (int)(HeaderHeight + 4f), box.Width - ContentInset * 2, (int)RowHeight);
            string modsText = mods.Count == 0 ? "-" : $"{mods.Count:N0}";

            string tooltip = mods.Count == 0
                ? "No mods used in replay"
                : "Mods used in replay:\n" + string.Join($"\n", mods.Select(x => $"[mi:{x.InternalName}]{x.DisplayName}"));

            StatDrawer.DrawWorldStatPanel(sb, statBox, Ass.Icon_CheckmarkGreen.Value, modsText, tooltip, textColor: Color.Gray, label: "Mods used:");

            int dividerY = statBox.Bottom + 8;
            sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(box.X + 10, dividerY, box.Width - 20, 2), Color.White * 0.10f);

            DrawModGrid(sb, box, dividerY + 12);
        }

        private void DrawModGrid(SpriteBatch sb, Rectangle box, int gridY)
        {
            int gridX = box.X + ContentInset + 2;
            int gridColumns = Math.Max(1, (box.Right - ContentInset - gridX + 8) / ModSlotStep);
            bool canShowHover = IsMouseInsideSpectatorInfoPanel();

            for (int i = 0; i < mods.Count; i++)
            {
                int col = i % gridColumns;
                int row = i / gridColumns;
                ModEntry mod = mods[i];
                Rectangle slot = new(gridX + col * ModSlotStep, gridY + row * ModSlotStep, ModSlotSize, ModSlotSize);

                Utils.DrawInvBG(sb, slot, new Color(83, 97, 168) * 0.80f);

                if (mod.Icon is not null)
                    DrawModIcon(sb, mod.Icon, slot);
                else
                    DrawMissingIcon(sb, slot);

                if (canShowHover)
                    ShowHover(slot, mod.DisplayName);
            }
        }

        private static void DrawModIcon(SpriteBatch sb, Texture2D texture, Rectangle slot)
        {
            Rectangle source = texture.Bounds;
            float scale = Math.Min((slot.Width - 8f) / source.Width, (slot.Height - 8f) / source.Height);
            Vector2 position = slot.Center.ToVector2();

            sb.Draw(texture, position, source, Color.White, 0f, source.Size() * 0.5f, Math.Min(1f, scale), SpriteEffects.None, 0f);
        }

        private static void DrawMissingIcon(SpriteBatch sb, Rectangle slot)
        {
            Texture2D texture = Ass.Icon_CameraSmall.Value;
            Rectangle source = texture.Bounds;
            float scale = Math.Min((slot.Width - 8f) / source.Width, (slot.Height - 8f) / source.Height);

            sb.Draw(texture, slot.Center.ToVector2(), source, Color.White * 0.35f, 0f, source.Size() * 0.5f, Math.Min(1f, scale), SpriteEffects.None, 0f);
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

        private static IReadOnlyList<ModEntry> GetModEntries()
        {
            string[] modNames = ReplayPlayback.Metadata?.ModNames;

            if (modNames == null || modNames.Length == 0)
                return [];

            return modNames.Select(CreateModEntry).ToList();
        }

        private static ModEntry CreateModEntry(string modName)
        {
            if (!TryFindLoadedMod(modName, out Mod mod))
                return new ModEntry(modName, modName, null);

            return new ModEntry(mod.Name, mod.DisplayName, GetModIcon(mod));
        }

        private static bool TryFindLoadedMod(string modName, out Mod mod)
        {
            if (ModLoader.TryGetMod(modName, out mod))
                return true;

            foreach (Mod candidate in ModLoader.Mods)
            {
                if (string.Equals(candidate.DisplayName, modName, StringComparison.OrdinalIgnoreCase))
                {
                    mod = candidate;
                    return true;
                }
            }

            mod = null;
            return false;
        }

        private static Texture2D GetModIcon(Mod mod)
        {
            if (TryGetModIcon(mod, "icon_small", "icon_small.png", out Texture2D smallIcon))
                return smallIcon;

            if (TryGetModIcon(mod, "icon", "icon.png", out Texture2D icon))
                return icon;

            return null;
        }

        private static bool TryGetModIcon(Mod mod, string assetName, string fileName, out Texture2D texture)
        {
            texture = null;

            if (!mod.FileExists(fileName))
                return false;

            try
            {
                texture = mod.Assets.Request<Texture2D>(assetName, AssetRequestMode.ImmediateLoad).Value;
                return texture is not null;
            }
            catch (Exception e)
            {
                Log.Chat($"Could not load {fileName} for {mod.Name}: {e.Message}");
                return false;
            }
        }

        private static float GetPanelHeight(int modCount)
        {
            int rows = Math.Max(1, (int)Math.Ceiling(modCount / 6f));
            int gridHeight = rows == 0 ? ModSlotSize : ModSlotSize + (rows - 1) * ModSlotStep;
            int height = 88 + gridHeight + ContentInset;

            return Math.Max(182, height);
        }

        private readonly struct ModEntry
        {
            public readonly string InternalName;
            public readonly string DisplayName;
            public readonly Texture2D Icon;

            public ModEntry(string internalName, string displayName, Texture2D icon)
            {
                InternalName = internalName;
                DisplayName = displayName;
                Icon = icon;
            }
        }
    }
}