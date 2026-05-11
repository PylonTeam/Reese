using Microsoft.Xna.Framework.Graphics;
using Reese.Common.Replayer.ReplayHud.Shared.Tabs;
using Reese.Common.Replayer.ReplayHud.Spectate;
using ReLogic.Content;
using System.Collections.Generic;
using Terraria.GameContent.UI.Elements;

internal sealed class NPCTab : TabPage
{
    private readonly List<(int WhoAmI, int Type, string Name)> npcSnapshot = [];

    public override SpectatorTab Tab => SpectatorTab.NPCs;
    public override string HeaderText => "NPCs";
    public override string TooltipText => "Spectate NPCs";
    public override Asset<Texture2D> Icon => Ass.Icon_NPC;

    protected override float ScrollbarLeft => -24f;
    protected override float ScrollbarTop => 8f;
    protected override float ScrollbarHeight => -16f;
    protected override float ListTop => 8f;
    protected override float ListLeft => 8f;
    protected override float ListWidth => -40f;
    protected override float ListHeight => -16f;
    protected override float ListPadding => 8f;

    public override float IconScale => 1f;

    public override Vector2 IconOffset => new Vector2(0, -2);

    public override void Refresh()
    {
        RememberNPCList();
        base.Refresh();
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        RefreshIfNPCListChanged();
    }

    protected override void Populate(UIList list)
    {
        int filteredCount = CountFilteredNPCs();
        int listIndex = 0;

        list.Add(new NPCFilterSummaryPanel(filteredCount));

        for (int i = 0; i < Main.maxNPCs; i++)
        {
            NPC npc = Main.npc[i];
            if (!ShouldShowNPC(npc))
                continue;

            UINPCCard card = new(i, listIndex++);
            card.Width.Set(UINPCCard.CardWidth, 0f);
            card.Height.Set(UINPCCard.CardHeight, 0f);
            list.Add(card);
        }

        if (listIndex == 0)
        {
            UIText emptyText = new("No NPCs are available to spectate.")
            {
                TextColor = Color.LightGray
            };
            emptyText.Width.Set(0f, 1f);
            emptyText.Height.Set(40f, 0f);
            list.Add(emptyText);
        }
    }

    private void RefreshIfNPCListChanged()
    {
        List<(int WhoAmI, int Type, string Name)> current = [];
        BuildNPCSnapshot(current);

        if (MatchesNPCSnapshot(current))
            return;

        npcSnapshot.Clear();
        npcSnapshot.AddRange(current);
        base.Refresh();
    }

    private void RememberNPCList()
    {
        BuildNPCSnapshot(npcSnapshot);
    }

    private static void BuildNPCSnapshot(List<(int WhoAmI, int Type, string Name)> snapshot)
    {
        snapshot.Clear();

        for (int i = 0; i < Main.maxNPCs; i++)
        {
            NPC npc = Main.npc[i];

            if (ShouldShowNPC(npc))
                snapshot.Add((npc.whoAmI, npc.type, npc.FullName));
        }
    }

    private bool MatchesNPCSnapshot(List<(int WhoAmI, int Type, string Name)> current)
    {
        if (current.Count != npcSnapshot.Count)
            return false;

        for (int i = 0; i < current.Count; i++)
        {
            if (!current[i].Equals(npcSnapshot[i]))
                return false;
        }

        return true;
    }

    private static bool ShouldShowNPC(NPC npc)
    {
        return npc?.active == true;
    }

    private static int CountFilteredNPCs()
    {
        int count = 0;

        for (int i = 0; i < Main.maxNPCs; i++)
        {
            if (ShouldShowNPC(Main.npc[i]))
                count++;
        }

        return count;
    }

    private sealed class NPCFilterSummaryPanel : UIPanel
    {
        public NPCFilterSummaryPanel(int count)
        {
            Width.Set(UINPCCard.CardWidth, 0f);
            Height.Set(32f, 0f);
            SetPadding(0f);
            BackgroundColor = new Color(28, 36, 76) * 0.92f;
            BorderColor = Color.Black;

            Append(new UIText($"{count} NPCs filtered", textScale: 0.85f)
            {
                HAlign = 0.5f,
                VAlign = 0.5f,
                TextColor = Color.White
            });
        }
    }
}