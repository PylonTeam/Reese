using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace Reese.Common.Replayer;

public sealed class ReplayPlayerSnapshot
{
    public string Name { get; set; } = string.Empty;
    public bool Male { get; set; }
    public int SkinVariant { get; set; }
    public int Hair { get; set; }
    public int HairDye { get; set; }
    public int SkinDyePacked { get; set; }
    public int SelectedItem { get; set; }
    public int CurrentLoadoutIndex { get; set; }
    public byte Difficulty { get; set; }
    public byte HideMisc { get; set; }

    public ReplayColorSnapshot HairColor { get; set; }
    public ReplayColorSnapshot SkinColor { get; set; }
    public ReplayColorSnapshot EyeColor { get; set; }
    public ReplayColorSnapshot ShirtColor { get; set; }
    public ReplayColorSnapshot UnderShirtColor { get; set; }
    public ReplayColorSnapshot PantsColor { get; set; }
    public ReplayColorSnapshot ShoeColor { get; set; }

    public bool[] HideVisibleAccessory { get; set; }
    public ReplayItemSnapshot[] Inventory { get; set; }
    public ReplayItemSnapshot[] Armor { get; set; }
    public ReplayItemSnapshot[] Dye { get; set; }
    public ReplayItemSnapshot[] MiscEquips { get; set; }
    public ReplayItemSnapshot[] MiscDyes { get; set; }
    public ReplayLoadoutSnapshot[] Loadouts { get; set; }

    public static ReplayPlayerSnapshot FromPlayer(Player player)
    {
        if (player == null)
            return null;

        return new ReplayPlayerSnapshot
        {
            Name = player.name ?? string.Empty,
            Male = player.Male,
            SkinVariant = player.skinVariant,
            Hair = player.hair,
            HairDye = player.hairDye,
            SkinDyePacked = player.skinDyePacked,
            SelectedItem = player.selectedItem,
            CurrentLoadoutIndex = player.CurrentLoadoutIndex,
            Difficulty = player.difficulty,
            HideMisc = player.hideMisc,
            HairColor = ReplayColorSnapshot.FromColor(player.hairColor),
            SkinColor = ReplayColorSnapshot.FromColor(player.skinColor),
            EyeColor = ReplayColorSnapshot.FromColor(player.eyeColor),
            ShirtColor = ReplayColorSnapshot.FromColor(player.shirtColor),
            UnderShirtColor = ReplayColorSnapshot.FromColor(player.underShirtColor),
            PantsColor = ReplayColorSnapshot.FromColor(player.pantsColor),
            ShoeColor = ReplayColorSnapshot.FromColor(player.shoeColor),
            HideVisibleAccessory = Copy(player.hideVisibleAccessory),
            Inventory = ReplayItemSnapshot.FromItems(player.inventory),
            Armor = ReplayItemSnapshot.FromItems(player.armor),
            Dye = ReplayItemSnapshot.FromItems(player.dye),
            MiscEquips = ReplayItemSnapshot.FromItems(player.miscEquips),
            MiscDyes = ReplayItemSnapshot.FromItems(player.miscDyes),
            Loadouts = ReplayLoadoutSnapshot.FromLoadouts(player.Loadouts)
        };
    }

    public Player ToPlayer()
    {
        var player = new Player();
        ApplyTo(player);
        return player;
    }

    public void ApplyTo(Player player)
    {
        if (player == null)
            return;

        player.active = true;
        player.dead = false;
        player.name = Name ?? string.Empty;
        player.Male = Male;
        player.skinVariant = SkinVariant;
        player.hair = Hair;
        player.hairDye = HairDye;
        player.skinDyePacked = SkinDyePacked;
        player.selectedItem = ClampIndex(SelectedItem, player.inventory?.Length ?? 0);
        player.CurrentLoadoutIndex = ClampIndex(CurrentLoadoutIndex, player.Loadouts?.Length ?? 0);
        player.difficulty = Difficulty;
        player.hideMisc = HideMisc;
        player.hairColor = HairColor?.ToColor() ?? player.hairColor;
        player.skinColor = SkinColor?.ToColor() ?? player.skinColor;
        player.eyeColor = EyeColor?.ToColor() ?? player.eyeColor;
        player.shirtColor = ShirtColor?.ToColor() ?? player.shirtColor;
        player.underShirtColor = UnderShirtColor?.ToColor() ?? player.underShirtColor;
        player.pantsColor = PantsColor?.ToColor() ?? player.pantsColor;
        player.shoeColor = ShoeColor?.ToColor() ?? player.shoeColor;

        Apply(HideVisibleAccessory, player.hideVisibleAccessory);
        ReplayItemSnapshot.ApplyTo(Inventory, player.inventory);
        ReplayItemSnapshot.ApplyTo(Armor, player.armor);
        ReplayItemSnapshot.ApplyTo(Dye, player.dye);
        ReplayItemSnapshot.ApplyTo(MiscEquips, player.miscEquips);
        ReplayItemSnapshot.ApplyTo(MiscDyes, player.miscDyes);
        ReplayLoadoutSnapshot.ApplyTo(Loadouts, player.Loadouts);
    }

    private static bool[] Copy(bool[] source)
    {
        if (source == null)
            return null;

        var copy = new bool[source.Length];
        Array.Copy(source, copy, source.Length);
        return copy;
    }

    private static void Apply(bool[] source, bool[] target)
    {
        if (source == null || target == null)
            return;

        int count = Math.Min(source.Length, target.Length);
        for (var i = 0; i < count; i++)
            target[i] = source[i];
    }

    private static int ClampIndex(int index, int length)
    {
        if (length <= 0)
            return 0;

        return Math.Clamp(index, 0, length - 1);
    }
}

public sealed class ReplayLoadoutSnapshot
{
    public ReplayItemSnapshot[] Armor { get; set; }
    public ReplayItemSnapshot[] Dye { get; set; }
    public bool[] Hide { get; set; }

    public static ReplayLoadoutSnapshot[] FromLoadouts(EquipmentLoadout[] loadouts)
    {
        if (loadouts == null)
            return null;

        var snapshots = new ReplayLoadoutSnapshot[loadouts.Length];
        for (var i = 0; i < loadouts.Length; i++)
        {
            var loadout = loadouts[i];
            if (loadout == null)
                continue;

            snapshots[i] = new ReplayLoadoutSnapshot
            {
                Armor = ReplayItemSnapshot.FromItems(loadout.Armor),
                Dye = ReplayItemSnapshot.FromItems(loadout.Dye),
                Hide = Copy(loadout.Hide)
            };
        }

        return snapshots;
    }

    public static void ApplyTo(ReplayLoadoutSnapshot[] snapshots, EquipmentLoadout[] loadouts)
    {
        if (snapshots == null || loadouts == null)
            return;

        int count = Math.Min(snapshots.Length, loadouts.Length);
        for (var i = 0; i < count; i++)
        {
            var snapshot = snapshots[i];
            var loadout = loadouts[i];
            if (snapshot == null || loadout == null)
                continue;

            ReplayItemSnapshot.ApplyTo(snapshot.Armor, loadout.Armor);
            ReplayItemSnapshot.ApplyTo(snapshot.Dye, loadout.Dye);
            Apply(snapshot.Hide, loadout.Hide);
        }
    }

    private static bool[] Copy(bool[] source)
    {
        if (source == null)
            return null;

        var copy = new bool[source.Length];
        Array.Copy(source, copy, source.Length);
        return copy;
    }

    private static void Apply(bool[] source, bool[] target)
    {
        if (source == null || target == null)
            return;

        int count = Math.Min(source.Length, target.Length);
        for (var i = 0; i < count; i++)
            target[i] = source[i];
    }
}

public sealed class ReplayItemSnapshot
{
    public int Type { get; set; }
    public int NetId { get; set; }
    public int Stack { get; set; }
    public int Prefix { get; set; }
    public int Dye { get; set; }
    public byte Paint { get; set; }
    public bool Favorited { get; set; }

    public static ReplayItemSnapshot FromItem(Item item)
    {
        if (item == null || item.type <= ItemID.None || item.stack <= 0)
            return null;

        return new ReplayItemSnapshot
        {
            Type = item.type,
            NetId = item.netID,
            Stack = item.stack,
            Prefix = item.prefix,
            Dye = item.dye,
            Paint = item.paint,
            Favorited = item.favorited
        };
    }

    public static ReplayItemSnapshot[] FromItems(Item[] items)
    {
        if (items == null)
            return null;

        var snapshots = new ReplayItemSnapshot[items.Length];
        for (var i = 0; i < items.Length; i++)
            snapshots[i] = FromItem(items[i]);

        return snapshots;
    }

    public Item ToItem()
    {
        var item = new Item();
        int id = NetId != 0 ? NetId : Type;
        if (id <= ItemID.None)
            return item;

        try
        {
            item.netDefaults(id);
        }
        catch
        {
            if (Type > ItemID.None)
            {
                try
                {
                    item.SetDefaults(Type);
                }
                catch
                {
                    return new Item();
                }
            }
        }

        item.stack = Math.Max(1, Stack);
        item.dye = Dye;
        item.paint = Paint;
        item.favorited = Favorited;

        if (Prefix > 0)
        {
            try
            {
                item.Prefix(Prefix);
            }
            catch
            {
                item.prefix = Prefix;
            }
        }

        return item;
    }

    public static void ApplyTo(ReplayItemSnapshot[] snapshots, Item[] target)
    {
        if (target == null)
            return;

        for (var i = 0; i < target.Length; i++)
            target[i] = i < (snapshots?.Length ?? 0) && snapshots[i] != null ? snapshots[i].ToItem() : new Item();
    }
}

public sealed class ReplayColorSnapshot
{
    public byte R { get; set; }
    public byte G { get; set; }
    public byte B { get; set; }
    public byte A { get; set; } = 255;

    public static ReplayColorSnapshot FromColor(Color color) => new()
    {
        R = color.R,
        G = color.G,
        B = color.B,
        A = color.A
    };

    public Color ToColor() => new(R, G, B, A);
}
