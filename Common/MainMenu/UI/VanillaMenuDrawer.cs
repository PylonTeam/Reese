using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using System;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.UI.Gamepad;

namespace Reese.Common.MainMenu.UI;

public static class VanillaMenuDrawer
{
    // Call after you fill the menu arrays; it draws items and updates focus/click indices exactly like vanilla.
    public static void DrawVanillaMenuLoop(
        SpriteBatch spriteBatch,
        string[] labels,
        bool[] isHeader,
        bool[] isDisabled,
        bool[] isDimmed,
        int[] itemYOffset,
        int[] itemXOffset,
        byte[] itemColorType,
        float[] itemScale,
        bool[] isLeftAligned,
        int menuTop,
        int menuCenterX,
        int menuSpacing,
        int itemCount,
        int menuMode,
        int netMode,
        float mouseTextColor,
        Color baseColor,
        Color mediumcoreColor,
        Color hardcoreColor,
        Color highVersionColor,
        Color errorColor,
        ref int focusedIndex,
        ref int clickedIndex,
        ref int rightClickedIndex,
        int previousFocusedIndex,
        bool[] menuWide,
        float[] itemScaleAnim,
        int mouseX,
        int mouseY,
        bool hasFocus,
        bool mouseLeftRelease,
        bool mouseLeft,
        bool mouseRightRelease,
        bool mouseRight)
    {
        bool focusChanged = false;
        focusedIndex = -1;

        for (int itemIndex = 0; itemIndex < itemCount; itemIndex++)
        {
            if (labels[itemIndex] == null) continue;

            Vector2 origin = FontAssets.DeathText.Value.MeasureString(labels[itemIndex]);
            origin.X *= 0.5f;
            origin.Y *= 0.5f;

            for (int shadowIndex = 0; shadowIndex < 5; shadowIndex++)
            {
                Color textColor = Color.Black;
                if (shadowIndex == 4)
                {
                    switch (itemColorType[itemIndex])
                    {
                        case 0: textColor = baseColor; break;
                        case 1: textColor = mediumcoreColor; break;
                        case 2: textColor = hardcoreColor; break;
                        case 3: textColor = highVersionColor; break;
                        case 4:
                        case 5:
                        case 6: textColor = errorColor; break;
                        default: textColor = baseColor; break;
                    }
                    textColor.R = (byte)((255 + textColor.R) / 2);
                    textColor.G = (byte)((255 + textColor.G) / 2);
                    textColor.B = (byte)((255 + textColor.B) / 2);
                }

                int alpha = (int)(255f * (itemScaleAnim[itemIndex] * 2f - 1f));
                if (isHeader[itemIndex]) alpha = 255;

                int r = textColor.R - (255 - alpha); if (r < 0) r = 0;
                int g = textColor.G - (255 - alpha); if (g < 0) g = 0;
                int b = textColor.B - (255 - alpha); if (b < 0) b = 0;

                if (previousFocusedIndex == itemIndex && shadowIndex == 4)
                {
                    float t = (float)alpha / 255f;
                    r = (int)(r * (1f - t) + 255f * t);
                    g = (int)(g * (1f - t) + 215f * t);
                    b = (int)(b * (1f - t) + 0f * t);
                }

                textColor = new Color((byte)r, (byte)g, (byte)b, (byte)alpha);

                if (isDimmed[itemIndex])
                {
                    if (shadowIndex == 4)
                    {
                        textColor.R = (byte)(textColor.R * mouseTextColor / 300);
                        textColor.G = (byte)(textColor.G * mouseTextColor / 300);
                        textColor.B = (byte)(textColor.B * mouseTextColor / 300);
                        textColor.A = (byte)(textColor.A * mouseTextColor / 300);
                    }
                    else
                    {
                        textColor.A -= (byte)((int)mouseTextColor / 5);
                    }
                }

                int shadowX = 0, shadowY = 0;
                if (shadowIndex == 0) shadowX = -2;
                if (shadowIndex == 1) shadowX = 2;
                if (shadowIndex == 2) shadowY = -2;
                if (shadowIndex == 3) shadowY = 2;

                float scale = itemScaleAnim[itemIndex];
                if (menuMode == 15 && itemIndex == 0) scale *= 0.35f;
                else if (menuMode == 1000000 && itemIndex == 0) scale *= 0.75f;
                else if (netMode == 2) scale *= 0.5f;
                scale *= itemScale[itemIndex];

                Vector2 drawPos = new Vector2(
                    menuCenterX + shadowX + itemXOffset[itemIndex],
                    (float)(menuTop + menuSpacing * itemIndex + shadowY) + origin.Y * itemScale[itemIndex] + (float)itemYOffset[itemIndex]);

                if (!isLeftAligned[itemIndex])
                    spriteBatch.DrawString(FontAssets.DeathText.Value, labels[itemIndex], drawPos, textColor, 0f, origin, scale, SpriteEffects.None, 0f);
                else
                    spriteBatch.DrawString(FontAssets.DeathText.Value, labels[itemIndex], drawPos, textColor, 0f, new Vector2(0f, origin.Y), scale, SpriteEffects.None, 0f);
            }

            if (!isHeader[itemIndex] && !isDisabled[itemIndex])
            {
                GamepadMainMenuHandler.MenuItemPositions.Add(new Vector2(
                    menuCenterX + itemXOffset[itemIndex],
                    (float)(menuTop + menuSpacing * itemIndex) + origin.Y * itemScale[itemIndex] + (float)itemYOffset[itemIndex]));
            }

            if (!isLeftAligned[itemIndex])
            {
                int extraWidth = 0;
                menuWide[itemIndex] = false;
                Vector2 size = FontAssets.DeathText.Value.MeasureString(labels[itemIndex]) * itemScale[itemIndex];
                if (!((float)mouseX > (float)menuCenterX - size.X * 0.5f + (float)itemXOffset[itemIndex] - (float)extraWidth) ||
                    !((float)mouseX < (float)menuCenterX + size.X * 0.5f * itemScale[itemIndex] + (float)itemXOffset[itemIndex] + (float)extraWidth) ||
                    mouseY <= menuTop + menuSpacing * itemIndex + itemYOffset[itemIndex] ||
                    !((float)mouseY < (float)(menuTop + menuSpacing * itemIndex + itemYOffset[itemIndex]) + 50f * itemScale[itemIndex]) ||
                    !hasFocus)
                    continue;

                focusedIndex = itemIndex;
                if (isHeader[itemIndex] || isDisabled[itemIndex]) { focusedIndex = -1; continue; }
                if (previousFocusedIndex != focusedIndex) focusChanged = true;
                if (mouseLeftRelease && mouseLeft) clickedIndex = itemIndex;
                if (mouseRightRelease && mouseRight) rightClickedIndex = itemIndex;
                continue;
            }

            Vector2 sizeLeft = FontAssets.DeathText.Value.MeasureString(labels[itemIndex]) * itemScale[itemIndex];
            if (mouseX <= menuCenterX + itemXOffset[itemIndex] ||
                !((float)mouseX < (float)menuCenterX + sizeLeft.X + (float)itemXOffset[itemIndex]) ||
                mouseY <= menuTop + menuSpacing * itemIndex + itemYOffset[itemIndex] ||
                !((float)mouseY < (float)(menuTop + menuSpacing * itemIndex + itemYOffset[itemIndex]) + 50f * itemScale[itemIndex]) ||
                !hasFocus)
                continue;

            focusedIndex = itemIndex;
            if (isHeader[itemIndex] || isDisabled[itemIndex]) { focusedIndex = -1; continue; }
            if (previousFocusedIndex != focusedIndex) focusChanged = true;
            if (mouseLeftRelease && mouseLeft) clickedIndex = itemIndex;
            if (mouseRightRelease && mouseRight) rightClickedIndex = itemIndex;
        }

        if (focusChanged && previousFocusedIndex != focusedIndex)
            SoundEngine.PlaySound(SoundID.MenuTick);

        if (GamepadMainMenuHandler.MenuItemPositions.Count == 0)
        {
            Vector2 wobble = new Vector2(
                (float)Math.Cos(Main.GlobalTimeWrappedHourly * ((float)Math.PI * 2f)),
                (float)Math.Sin(Main.GlobalTimeWrappedHourly * ((float)Math.PI * 2f) * 2f)) * new Vector2(30f, 15f) + Vector2.UnitY * 20f;
            UILinkPointNavigator.SetPosition(2000, new Vector2(Main.screenWidth, Main.screenHeight) / 2f + wobble);
        }

        for (int i = 0; i < itemScaleAnim.Length; i++)
        {
            if (i == focusedIndex)
            {
                if (itemScaleAnim[i] < 1f) itemScaleAnim[i] += 0.02f;
                if (itemScaleAnim[i] > 1f) itemScaleAnim[i] = 1f;
            }
            else if ((double)itemScaleAnim[i] > 0.8)
            {
                itemScaleAnim[i] -= 0.02f;
            }
        }
    }
}
