using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reese.Content;

internal class GhostItem : ModItem
{
    public override void SetDefaults()
    {
        Item.width = 30;
        Item.height = 40;
    }
}
