using System;
using Reese.Common.MainMenu;
using Terraria.GameContent.UI.States;

namespace Reese.Common.MainMenu.UI;

public sealed class ConfirmRenameState : UIVirtualKeyboard
{
    public ConfirmRenameState(string currentName, KeyboardSubmitEvent onSubmit, Action onCancel)
        : base(Loc.Get("MainMenu.ReplayBrowser.EnterReplayName"), currentName, onSubmit, onCancel, 0, allowEmpty: false)
    {
        // Keep the input bounded like player/world names while leaving room for numbered replay names.
        SetMaxInputLength(40);
    }
}
