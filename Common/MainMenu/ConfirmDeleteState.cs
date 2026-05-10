using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.UI;
using Terraria.UI.Gamepad;

namespace Reese.Common.MainMenu;

public sealed class ConfirmDeleteState : UIState
{
    private const int Capacity = 3;
    private readonly string[] _labels = new string[Capacity];
    private readonly bool[] _isHeader = new bool[Capacity], _isDisabled = new bool[Capacity], _isDimmed = new bool[Capacity], _leftAligned = new bool[Capacity], _menuWide = new bool[Capacity];
    private readonly int[] _yOffset = new int[Capacity], _xOffset = new int[Capacity];
    private readonly byte[] _colorType = new byte[Capacity];
    private readonly float[] _scale = new float[Capacity], _scaleAnim = new float[Capacity];
    private int _focusedIndex = -1, _clickedIndex = -1, _rightClickedIndex = -1;

    public string TargetName;
    public Action OnYes;
    public Action OnNo;

    public override void OnActivate()
    {
        for (int i = 0; i < Capacity; i++)
        {
            _scaleAnim[i] = 0.8f;
            _scale[i] = 1f;
        }
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        _labels[0] = Lang.menu[46].Value + " " + TargetName + "?";
        _labels[1] = Lang.menu[104].Value;
        _labels[2] = Lang.menu[105].Value;
        _isHeader[0] = true;
        _clickedIndex = _rightClickedIndex = -1;

        bool backPressed = UILinkPointNavigator.Shortcuts.BackButtonInUse && !UILinkPointNavigator.Shortcuts.BackButtonLock;
        int previousFocusedIndex = _focusedIndex;
        VanillaMenuDrawer.DrawVanillaMenuLoop(spriteBatch, _labels, _isHeader, _isDisabled, _isDimmed, _yOffset, _xOffset,
            _colorType, _scale, _leftAligned, 250, Main.screenWidth / 2, 80, Capacity, Main.menuMode, Main.netMode,
            Main.mouseTextColor, Main.ColorOfTheSkies, Main.mcColor, Main.hcColor, Main.highVersionColor, Main.errorColor,
            ref _focusedIndex, ref _clickedIndex, ref _rightClickedIndex, previousFocusedIndex, _menuWide, _scaleAnim,
            Main.mouseX, Main.mouseY, Main.hasFocus, Main.mouseLeftRelease, Main.mouseLeft, Main.mouseRightRelease, Main.mouseRight);

        if (_clickedIndex == 1)
            OnYes?.Invoke();
        else if (_clickedIndex == 2 || backPressed)
            OnNo?.Invoke();
    }
}
