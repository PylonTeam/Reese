using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
namespace Reese.Common.Replayer.ReplayHud.Spectate.Stats;

public sealed class PlayerStatDefinition
{
    public PlayerStatDefinition(
        string id,
        string label,
        Asset<Texture2D> icon,
        Func<Player, string> getText,
        Func<Player, string> getHoverText = null,
        Rectangle? iconFrame = null)
        : this(id, label, _ => icon, getText, getHoverText, _ => iconFrame)
    {
    }

    public PlayerStatDefinition(
        string id,
        string label,
        Func<Player, Asset<Texture2D>> getIcon,
        Func<Player, string> getText,
        Func<Player, string> getHoverText = null,
        Func<Player, Rectangle?> getIconFrame = null)
    {
        Id = id;
        Label = label;
        GetIcon = getIcon;
        GetText = getText;
        GetHoverText = getHoverText;
        GetIconFrame = getIconFrame;
    }

    public string Id { get; }
    public string Label { get; }
    public Func<Player, Asset<Texture2D>> GetIcon { get; }
    public Func<Player, string> GetText { get; }
    public Func<Player, string> GetHoverText { get; }
    public Func<Player, Rectangle?> GetIconFrame { get; }

    public PlayerStatSnapshot Build(Player player)
    {
        string text = GetText(player);
        string hoverText = GetHoverText == null ? $"{Label}: {text}" : GetHoverText(player);
        Rectangle? iconFrame = GetIconFrame == null ? null : GetIconFrame(player);

        return new PlayerStatSnapshot(Label, text, hoverText, GetIcon(player), iconFrame);
    }
}

public readonly record struct PlayerStatSnapshot(
    string Label,
    string Text,
    string HoverText,
    Asset<Texture2D> Icon,
    Rectangle? IconFrame);