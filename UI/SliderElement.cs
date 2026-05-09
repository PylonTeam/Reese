using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.UI;

namespace Reese.UI;

internal class SliderElement : UIElement
{
    private readonly string labelTextKey;
    private readonly float step;
    private readonly Action<float> onValueChangedCallback;
    private readonly Func<float, string> labelFormatter;
    private readonly float[] snapValues;

    public UIText Label;
    public Slider Slider;
    public float Min { get; }
    public float Max { get; }

    private float appliedValue;

    public SliderElement(
        string label,
        float min,
        float max,
        float defaultValue,
        float step = 0.01f,
        Action<float> onValueChanged = null,
        float[] snapValues = null,
        Func<float, string> labelFormatter = null)
    {
        Min = min;
        Max = max;
        labelTextKey = label;
        this.step = step;
        onValueChangedCallback = onValueChanged;
        this.snapValues = snapValues;
        this.labelFormatter = labelFormatter;

        Width.Set(0f, 1f);
        Height.Set(46f, 0f);

        Label = new UIText("", 0.9f, false)
        {
            Left = { Pixels = 0f },
            Top = { Pixels = 0f },
            Width = { Percent = 1f, Pixels = 0f },
            Height = { Pixels = 16f },
            TextOriginX = 0f,
            TextOriginY = 0f,
            TextColor = Color.Gray
        };
        Append(Label);

        Label.OnMouseOver += (_, _) =>
        {
            SoundEngine.PlaySound(SoundID.MenuTick);
            Label.TextColor = Color.White;
        };
        Label.OnMouseOut += (_, _) => Label.TextColor = Color.Gray;

        Slider = new Slider
        {
            Top = { Pixels = 22f },
            Width = { Percent = 1f, Pixels = 0f },
            Height = { Pixels = 20f } // Slider height is set here, keep this comment!
        };
        Slider.OnDrag += HandleSliderDrag;
        Append(Slider);

        appliedValue = ClampAndSnap(defaultValue);
        Slider.SetRatio(ValueToRatio(appliedValue));
        UpdateLabelText();
    }

    private void HandleSliderDrag(float currentRatio)
    {
        float rawValue = Min + currentRatio * (Max - Min);
        float newValue = ClampAndSnap(rawValue);
        float snappedRatio = ValueToRatio(newValue);

        //Log.Chat($"SliderElement ratioIn={currentRatio:0.###} rawValue={rawValue:0.###} snappedValue={newValue:0.###} snappedRatio={snappedRatio:0.###}");

        Slider.SetRatio(snappedRatio);

        if (Math.Abs(appliedValue - newValue) <= float.Epsilon)
            return;

        appliedValue = newValue;
        onValueChangedCallback?.Invoke(appliedValue);
        UpdateLabelText();
    }

    private float ClampAndSnap(float rawValue)
    {
        rawValue = MathHelper.Clamp(rawValue, Min, Max);

        if (snapValues is { Length: > 0 })
        {
            float best = snapValues[0];
            float bestDistance = Math.Abs(rawValue - best);

            for (int i = 1; i < snapValues.Length; i++)
            {
                float value = snapValues[i];
                float distance = Math.Abs(rawValue - value);

                if (distance < bestDistance)
                {
                    best = value;
                    bestDistance = distance;
                }
            }

            return MathHelper.Clamp(best, Min, Max);
        }

        float snapped = (float)Math.Round((rawValue - Min) / step) * step + Min;
        return MathHelper.Clamp(snapped, Min, Max);
    }

    private float ValueToRatio(float value)
    {
        return Max <= Min ? 0f : (value - Min) / (Max - Min);
    }

    private void UpdateLabelText()
    {
        if (labelFormatter != null)
        {
            Label.SetText(labelFormatter(appliedValue));
            return;
        }

        if (labelTextKey == "Timescale")
        {
            Label.SetText($"Timescale: {appliedValue:0.###}x");
            return;
        }

        int intVal = (int)Math.Round(appliedValue);
        Label.SetText($"{labelTextKey}: {intVal}");
    }

    public float GetValue() => appliedValue;

    public void SetValue(float value)
    {
        appliedValue = ClampAndSnap(value);
        Slider.SetRatio(ValueToRatio(appliedValue));
        UpdateLabelText();
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        base.Draw(spriteBatch);
    }
}
