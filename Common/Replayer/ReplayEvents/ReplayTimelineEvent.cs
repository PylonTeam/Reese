using System;

namespace Reese.Common.Replayer.ReplayEvents;

public enum ReplayEventCategory : byte
{
    BossDefeated = 1,
    PlayerDeath = 2,
    InvasionStarted = 3,
    PlayerJoined = 4,
    PlayerLeft = 5,
    PlayerKill = 6,
    BossSummoned = 7,
    Custom = 255
}

public enum ReplayEventIconKind : byte
{
    None = 0,
    BossHead = 1,
    MapDeath = 2,
    Item = 3,
    PlayerHead = 4
}

public readonly record struct ReplayTimelineEvent(
    uint Tick,
    ReplayEventCategory Category,
    string Key,
    string Text,
    ReplayEventIconKind IconKind = ReplayEventIconKind.None,
    int IconId = 0,
    ReplayPlayerHeadSnapshot? PlayerHead = null);

public readonly record struct ReplayPlayerHeadSnapshot(
    int PlayerIndex,
    int Team,
    int Hair,
    int HairDye,
    int SkinVariant,
    int Head,
    int Body,
    int Legs,
    int CHead,
    int CBody,
    int CLegs,
    int Face,
    int Neck,
    int Front,
    int Back,
    int Waist,
    int Shield,
    int Shoe,
    int Balloon,
    int Beard,
    int HandOn,
    int HandOff,
    uint HairColor,
    uint SkinColor,
    uint EyeColor,
    uint ShirtColor,
    uint UnderShirtColor,
    uint PantsColor,
    uint ShoeColor)
{
    public static ReplayPlayerHeadSnapshot FromPlayer(Player player)
    {
        return new ReplayPlayerHeadSnapshot(
            player?.whoAmI ?? -1,
            player?.team ?? 0,
            player?.hair ?? 0,
            player?.hairDye ?? 0,
            player?.skinVariant ?? 0,
            player?.head ?? -1,
            player?.body ?? -1,
            player?.legs ?? -1,
            player?.cHead ?? 0,
            player?.cBody ?? 0,
            player?.cLegs ?? 0,
            player?.face ?? -1,
            player?.neck ?? -1,
            player?.front ?? -1,
            player?.back ?? -1,
            player?.waist ?? -1,
            player?.shield ?? -1,
            player?.shoe ?? -1,
            player?.balloon ?? -1,
            player?.beard ?? -1,
            player?.handon ?? -1,
            player?.handoff ?? -1,
            Pack(player?.hairColor ?? Color.White),
            Pack(player?.skinColor ?? Color.White),
            Pack(player?.eyeColor ?? Color.White),
            Pack(player?.shirtColor ?? Color.White),
            Pack(player?.underShirtColor ?? Color.White),
            Pack(player?.pantsColor ?? Color.White),
            Pack(player?.shoeColor ?? Color.White));
    }

    public void ApplyTo(Player player)
    {
        if (player == null)
            return;

        player.active = true;
        player.whoAmI = PlayerIndex >= 0 && PlayerIndex < Main.maxPlayers ? PlayerIndex : 0;
        player.team = Team >= 0 && Team < Main.teamColor.Length ? Team : 0;
        player.hair = ToByte(Hair);
        player.hairDye = ToByte(HairDye);
        player.skinVariant = ToByte(SkinVariant);
        player.head = Head;
        player.body = Body;
        player.legs = Legs;
        player.cHead = CHead;
        player.cBody = CBody;
        player.cLegs = CLegs;
        player.face = Face;
        player.neck = Neck;
        player.front = Front;
        player.back = Back;
        player.waist = Waist;
        player.shield = Shield;
        player.shoe = Shoe;
        player.balloon = Balloon;
        player.beard = Beard;
        player.handon = HandOn;
        player.handoff = HandOff;
        player.hairColor = Unpack(HairColor);
        player.skinColor = Unpack(SkinColor);
        player.eyeColor = Unpack(EyeColor);
        player.shirtColor = Unpack(ShirtColor);
        player.underShirtColor = Unpack(UnderShirtColor);
        player.pantsColor = Unpack(PantsColor);
        player.shoeColor = Unpack(ShoeColor);
        player.dead = false;
        player.ghost = false;
        player.statLife = Math.Max(1, player.statLife);
        player.statLifeMax = Math.Max(1, player.statLifeMax);
        player.gravDir = 1f;

        if (player.bodyFrame.Width <= 0 || player.bodyFrame.Height <= 0)
            player.bodyFrame = new Rectangle(0, 0, 40, 56);

        if (player.legFrame.Width <= 0 || player.legFrame.Height <= 0)
            player.legFrame = new Rectangle(0, 0, 40, 56);

        if (player.headFrame.Width <= 0 || player.headFrame.Height <= 0)
            player.headFrame = new Rectangle(0, 0, 40, 56);
    }

    private static uint Pack(Color color)
    {
        return color.PackedValue;
    }

    private static Color Unpack(uint packedValue)
    {
        Color color = default;
        color.PackedValue = packedValue;
        return color;
    }

    private static byte ToByte(int value)
    {
        return (byte)Math.Clamp(value, byte.MinValue, byte.MaxValue);
    }
}
