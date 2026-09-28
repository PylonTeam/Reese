using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using ReLogic.Graphics;
using System;
using System.Collections.Generic;
using Terraria.GameContent;

namespace Reese.Common.MainMenu.UI;

/// <summary>Live-editable menu text themes. Settings are evaluated for every draw, not at type initialization.</summary>
internal static class MainMenuTextThemeDrawer
{
    // EDIT HERE, then apply C# Hot Reload. Switch the enum to recall any of the previous styles.
    // Every property in MainMenuTextThemeSettings can be overridden with a `with` expression here.
    // Keep tuning values in this method (or the preset methods), never in static field initializers.
    internal static MainMenuTextThemeSettings GetSettings()
    {
        var theme = MainMenuTextThemeSettings.Create(MainMenuTextStyle.OriginalFlame, reese: true);
        return theme with
        {
            IdleBrightness = theme.IdleBrightness * 0.88f,
            IdleEffectStrength = theme.IdleEffectStrength * 0.7f,
            HoverEffectStrength = theme.HoverEffectStrength * 0.7f
        };

        // Examples: return theme with {
        //     BaseColor = new Color(255, 42, 52), Speed = 0.75f, AnimationEnabled = true,
        //     Gradient = theme.Gradient with { WaveStrength = 0.04f, HoverShine = 0.18f },
        //     Flame = theme.Flame with { Count = 7, Strength = 0.20f },
        //     Assets = theme.Assets with { Spark = "Reese/Assets/Effects/Menu/PixelTwinkle" }
        // };
    }

    // Cache only asset wrappers, keyed by the selected path. Changing a path takes effect next draw;
    // texture hot reload can replace Asset.Value without leaving us with a disposed texture.
    private static readonly Dictionary<string, Asset<Texture2D>> Textures = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, long> RetryAfter = new(StringComparer.Ordinal);
    private static readonly HashSet<string> ReportedAssets = new(StringComparer.Ordinal);

    public static void Unload()
    {
        Textures.Clear();
        RetryAfter.Clear();
        ReportedAssets.Clear();
    }

    public static void Draw(SpriteBatch batch, DynamicSpriteFont font, string text,
        Vector2 position, Color color, float rotation, Vector2 origin, Vector2 scale,
        SpriteEffects effects, float layerDepth, bool hovered)
    {
        if (Main.dedServ || string.IsNullOrEmpty(text) || color.A == 0)
            return;

        MainMenuTextThemeSettings settings = GetSettings();
        float hover = MathHelper.Clamp((color.A - 153f) / 102f, 0f, 1f);
        float opacity = MathHelper.Clamp(color.A / 153f, 0f, 1f)
            * MathHelper.Clamp(MathHelper.Lerp(settings.IdleOpacity, settings.HoverOpacity, hover), 0f, 1f);
        if (opacity <= 0f)
            return;

        if (effects != SpriteEffects.None || scale.X <= 0f || scale.Y <= 0f)
        {
            Color fallback = Color.Lerp(settings.BaseColor, settings.HoverBaseColor,
                hovered ? settings.HoverColorMix : 0f);
            batch.DrawString(font, text, position, fallback * opacity, rotation, origin, scale, effects, layerDepth);
            return;
        }

        Vector2 size = font.MeasureString(text);
        Matrix transform = Matrix.CreateScale(scale.X, scale.Y, 1f) * Matrix.CreateRotationZ(rotation);
        float top = float.MaxValue;
        float bottom = float.MinValue;
        float baseline = 0f;
        foreach (char character in text)
        {
            var glyph = GetGlyph(font, character);
            top = Math.Min(top, glyph.Padding.Y);
            bottom = Math.Max(bottom, glyph.Padding.Y + glyph.Glyph.Height);
            baseline += glyph.Padding.Y + glyph.Glyph.Height;
        }
        baseline /= text.Length;

        float time = (settings.AnimationEnabled ? Main.GlobalTimeWrappedHourly * settings.Speed : 0f) + settings.Phase;
        float pulse = 1f - settings.PulseAmount + settings.PulseAmount * MathF.Sin(time * settings.PulseSpeed);
        float strength = Math.Max(0f, MathHelper.Lerp(settings.IdleEffectStrength, settings.HoverEffectStrength, hover) * pulse);
        Frame frame = new(batch, font, text, position, origin, scale, transform,
            position - Vector2.Transform(origin, transform), rotation, layerDepth, size.X,
            top, bottom, baseline, time, opacity, hover, opacity * strength, settings);

        bool vfx = settings.EffectsEnabled && settings.Style != MainMenuTextStyle.Plain && strength > 0f;
        if (vfx)
        {
            switch (settings.Style)
            {
                case MainMenuTextStyle.OriginalFlame:
                case MainMenuTextStyle.MutedFlame:
                case MainMenuTextStyle.SoftFlame:
                    DrawFlames(frame);
                    break;
                case MainMenuTextStyle.EnergyArcs:
                    DrawEnergyArcs(frame);
                    break;
                case MainMenuTextStyle.OrbitalWisps:
                    DrawOrbitalWisps(frame);
                    break;
            }
            DrawGlow(frame);
        }

        DrawOutline(frame);
        DrawFill(frame);

        if (!vfx)
            return;
        switch (settings.Style)
        {
            case MainMenuTextStyle.OriginalFlame:
            case MainMenuTextStyle.MutedFlame:
            case MainMenuTextStyle.SoftFlame:
                DrawSparks(frame);
                break;
            case MainMenuTextStyle.EnergyArcs:
                DrawGlints(frame);
                break;
            case MainMenuTextStyle.OrbitalWisps:
                DrawSatellites(frame);
                break;
        }
    }

    private readonly record struct Frame(
        SpriteBatch Batch, DynamicSpriteFont Font, string Text, Vector2 Position, Vector2 Origin,
        Vector2 Scale, Matrix Transform, Vector2 TopLeft, float Rotation, float Depth, float Width,
        float Top, float Bottom, float Baseline, float Time, float Opacity, float Hover, float Strength,
        MainMenuTextThemeSettings Settings)
    {
        public Vector2 Center => new(Width * 0.5f, (Top + Baseline) * 0.5f);
    }

    private static void DrawGlow(in Frame f)
    {
        var g = f.Settings.Glow;
        if (!g.Enabled)
            return;
        int directions = Count(g.Directions, 32);
        for (int ring = 0; ring < 2; ring++)
        {
            float radius = Math.Max(0f, ring == 0 ? g.InnerRadius : g.OuterRadius);
            Color color = Light(ring == 0 ? f.Settings.BaseColor : f.Settings.BottomColor,
                f.Strength * (ring == 0 ? g.InnerOpacity : g.OuterOpacity));
            for (int i = 0; i < directions; i++)
            {
                float angle = MathHelper.TwoPi * i / directions;
                Vector2 offset = Vector2.Transform(new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius, f.Transform);
                f.Batch.DrawString(f.Font, f.Text, f.Position + offset, color,
                    f.Rotation, f.Origin, f.Scale, SpriteEffects.None, f.Depth);
            }
        }
    }

    private static void DrawOutline(in Frame f)
    {
        var g = f.Settings.Glow;
        if (!g.OutlineEnabled)
            return;
        int directions = Count(g.OutlineDirections, 32);
        for (int i = 0; i < directions; i++)
        {
            float angle = MathHelper.TwoPi * i / directions;
            Vector2 offset = Vector2.Transform(new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * Math.Max(0f, g.OutlineRadius), f.Transform);
            f.Batch.DrawString(f.Font, f.Text, f.Position + offset, f.Settings.OutlineColor * f.Opacity,
                f.Rotation, f.Origin, f.Scale, SpriteEffects.None, f.Depth);
        }
    }

    private static void DrawFill(in Frame f)
    {
        var s = f.Settings;
        var g = s.Gradient;
        float hoverMix = MathHelper.Clamp(f.Hover * s.HoverColorMix, 0f, 1f);
        Color top = Color.Lerp(s.TopColor, s.HoverTopColor, hoverMix);
        Color middle = Color.Lerp(s.BaseColor, s.HoverBaseColor, hoverMix);
        Color bottom = Color.Lerp(s.BottomColor, s.HoverBottomColor, hoverMix);
        float split = MathHelper.Clamp(g.Split, 0.01f, 0.99f);
        float brightness = Math.Max(0f, MathHelper.Lerp(s.IdleBrightness, s.HoverBrightness, f.Hover));
        float tintAmount = MathHelper.Clamp(s.IdleTintAmount * (1f - f.Hover), 0f, 1f);
        if (!s.GradientEnabled)
        {
            Color fill = Brighten(Color.Lerp(middle, s.IdleTintColor, tintAmount), brightness);
            f.Batch.DrawString(f.Font, f.Text, f.Position, fill * f.Opacity,
                f.Rotation, f.Origin, f.Scale, SpriteEffects.None, f.Depth);
            return;
        }

        int stripHeight = Math.Clamp(g.StripHeight, 1, 64);
        float cursor = 0f;
        bool first = true;
        foreach (char character in f.Text)
        {
            var glyph = GetGlyph(f.Font, character);
            cursor += first ? Math.Max(glyph.Kerning.X, 0f) : f.Font.CharacterSpacing + glyph.Kerning.X;
            float glyphX = cursor + glyph.Padding.X;
            float across = (glyphX + glyph.Glyph.Width * 0.5f) / Math.Max(f.Width, 1f);
            float wave = MathF.Sin(across * g.WaveScale - f.Time * g.WaveSpeed) * g.WaveStrength;
            float sweep = MathF.Pow(Math.Max(0f, MathF.Cos(across * g.ShineScale - f.Time * g.ShineSpeed)), Math.Max(1f, g.ShinePower));
            float shine = MathHelper.Clamp(sweep * MathHelper.Lerp(g.IdleShine, g.HoverShine, f.Hover)
                + f.Hover * g.HoverHighlight, 0f, 1f);
            for (int y = 0; y < glyph.Glyph.Height; y += stripHeight)
            {
                int height = Math.Min(stripHeight, glyph.Glyph.Height - y);
                float localY = glyph.Padding.Y + y;
                float t = MathHelper.Clamp((localY + height * 0.5f - f.Top) / Math.Max(f.Bottom - f.Top, 1f) + wave, 0f, 1f);
                Color color = t < split ? Color.Lerp(top, middle, t / split) : Color.Lerp(middle, bottom, (t - split) / (1f - split));
                color = Color.Lerp(color, top, shine);
                color = Brighten(Color.Lerp(color, s.IdleTintColor, tintAmount), brightness);
                Rectangle source = new(glyph.Glyph.X, glyph.Glyph.Y + y, glyph.Glyph.Width, height);
                Vector2 point = f.TopLeft + Vector2.Transform(new Vector2(glyphX, localY), f.Transform);
                f.Batch.Draw(glyph.Texture, point, source, color * f.Opacity,
                    f.Rotation, Vector2.Zero, f.Scale, SpriteEffects.None, f.Depth);
            }
            cursor += glyph.Kerning.Y + glyph.Kerning.Z;
            first = false;
        }
    }

    private static void DrawBackdrop(in Frame f, Texture2D glow)
    {
        var g = f.Settings.Glow;
        if (!g.Enabled)
            return;
        DrawLight(f, glow, f.Center, new Vector2(f.Width, f.Baseline - f.Top) + g.BackdropPadding,
            f.Settings.BottomColor, f.Strength * g.BackdropStrength);
    }

    private static void DrawFlames(in Frame f)
    {
        var s = f.Settings;
        var p = s.Flame;
        Texture2D glow = GetTexture(s.Assets.Glow);
        Texture2D fire = GetTexture(s.Assets.Flame);
        DrawBackdrop(f, glow);
        int count = Count(p.Count);
        int steps = Count(p.Steps, 64);
        for (int i = 0; i < count; i++)
        {
            float seed = Hash(i + 11);
            float phase = f.Time * (p.MotionSpeed + seed * p.MotionVariation) + i * 2.4f;
            float x = (i + 0.5f) / count * f.Width;
            float root = f.Baseline - p.RootOffset - Hash(i + 57) * p.RootJitter;
            float height = Math.Max(0f, root - f.Top + p.Rise + seed * p.RiseVariation + MathF.Sin(phase) * p.HeightFlicker);
            for (int step = 0; step < steps; step++)
            {
                float t = step / (float)Math.Max(1, steps - 1);
                float bend = MathF.Sin(phase - t * 3.3f) * (1f + t * p.Bend);
                Vector2 point = new(x + bend, root - height * t);
                Vector2 size = new(MathHelper.Lerp(p.Width, p.TipWidth, t) * (0.85f + seed * 0.3f), height / Math.Max(1, steps - 1) + 6f);
                Color tint = Color.Lerp(s.BaseColor, s.TopColor, (1f - t) * 0.45f);
                float fade = p.SmoothTips ? 1f - MathHelper.SmoothStep(0f, 1f, t) : MathHelper.Lerp(1f, p.TipOpacity, t);
                DrawLight(f, glow, point, size, tint, fade * Falloff(f, point) * p.Strength * f.Strength);
            }
        }

        int swirls = Count(p.SwirlCount, 32);
        for (int i = 0; i < swirls; i++)
        {
            float phase = f.Time * p.SwirlSpeed + i * 1.9f;
            Vector2 point = new(f.Width * (i + 0.5f) / swirls + MathF.Sin(phase) * p.SwirlDrift, f.Top + 9f);
            DrawLight(f, fire, point, new Vector2(f.Width * p.SwirlWidthFactor, p.SwirlHeight + MathF.Sin(phase) * p.SwirlHeightWave),
                i % 2 == 0 ? s.BaseColor : s.BottomColor, p.SwirlOpacity * f.Strength * Falloff(f, point), MathF.Sin(phase * 0.7f) * p.SwirlSpin);
        }
    }

    private static void DrawSparks(in Frame f)
    {
        var s = f.Settings;
        var p = s.Sparks;
        Texture2D glow = GetTexture(s.Assets.Glow);
        Texture2D star = GetTexture(s.Assets.Spark);
        for (int i = 0; i < Count(p.Count, 256); i++)
        {
            float seed = Hash(i + 107);
            float life = Fraction(f.Time * (p.Speed + seed * p.SpeedVariation) + Hash(i + 231));
            float x = -p.Margin + Hash(i + 419) * (f.Width + p.Margin * 2f);
            x += MathF.Sin(f.Time * 2.2f + i * 1.7f) * (p.Wobble + life * p.LifeWobble);
            float y = f.Baseline + 3f - life * (f.Baseline - f.Top + p.Rise + seed * p.RiseVariation);
            Vector2 point = new(x, y);
            Color tint = Color.Lerp(s.BaseColor, s.TopColor, seed * 0.7f);
            float alpha = MathF.Sin(life * MathHelper.Pi) * f.Strength * Falloff(f, point);
            DrawLight(f, glow, point, p.GlowSize + p.GlowSizeVariation * seed, tint, alpha * p.GlowOpacity);
            DrawLight(f, p.HardCore ? TextureAssets.MagicPixel.Value : glow, point,
                p.CoreSize + p.CoreSizeVariation * seed, tint, alpha * p.CoreOpacity);
            DrawLight(f, glow, point + new Vector2(0f, p.TrailOffset), p.TrailSize, s.BaseColor, alpha * p.TrailOpacity);
        }
        for (int i = 0; i < Count(p.StarCount, 64); i++)
        {
            float phase = f.Time * (p.StarSpeed + Hash(i + 719) * p.StarSpeedVariation) + i * 2.1f;
            float twinkle = MathF.Pow(Math.Max(0f, MathF.Sin(phase)), 5f);
            Vector2 point = new(-7f + Hash(i + 811) * (f.Width + 14f), f.Top + Hash(i + 929) * (f.Baseline - f.Top));
            DrawLight(f, star, point, new Vector2(p.StarSize + twinkle * p.StarSizeWave), s.TopColor,
                twinkle * f.Strength * p.StarOpacity * Falloff(f, point), MathF.Sin(phase * 0.4f) * p.StarSpin);
        }
    }

    private static void DrawEnergyArcs(in Frame f)
    {
        var s = f.Settings;
        var p = s.Arcs;
        Texture2D glow = GetTexture(s.Assets.Glow);
        Texture2D ribbon = GetTexture(s.Assets.Ribbon);
        DrawBackdrop(f, glow);
        if (ribbon is null)
            return;
        int segments = Math.Min(Count(p.Segments, 128), ribbon.Width);
        float width = Math.Max(1f, f.Width + p.WidthPadding);
        for (int arc = 0; arc < Count(p.Count, 16); arc++)
        {
            bool upper = arc % 2 == 0;
            float y = upper ? f.Top + p.TopOffset : f.Baseline + p.BottomOffset;
            float height = Math.Max(0f, upper ? p.TopHeight : p.BottomHeight);
            for (int i = 0; i < segments; i++)
            {
                int left = ribbon.Width * i / segments;
                int right = ribbon.Width * (i + 1) / segments;
                float u = (i + 0.5f) / segments;
                float wave = MathF.Sin(u * MathHelper.Pi + f.Time * p.WaveSpeed + arc * MathHelper.Pi) * p.WaveHeight;
                Vector2 localPoint = new(-p.WidthPadding * 0.5f + u * width, y + wave);
                float fade = MathF.Pow(MathF.Sin(u * MathHelper.Pi), 2f) * Falloff(f, localPoint);
                Color tint = Color.Lerp(s.BaseColor, s.TopColor, upper ? 0.22f : 0.08f);
                DrawLight(f, glow, localPoint, new Vector2(width / segments * 2.4f, 6f), s.BaseColor,
                    f.Strength * fade * p.GlowOpacity);
                Rectangle source = new(left, 0, right - left, ribbon.Height);
                Vector2 point = f.TopLeft + Vector2.Transform(localPoint, f.Transform);
                Vector2 scale = new Vector2(width / segments / source.Width, height / source.Height) * f.Scale;
                f.Batch.Draw(ribbon, point, source, Light(tint, f.Strength * fade * p.RibbonOpacity), f.Rotation,
                    new Vector2(source.Width, source.Height) * 0.5f, scale, SpriteEffects.None, f.Depth);
            }
        }
    }

    private static void DrawGlints(in Frame f)
    {
        var s = f.Settings;
        var p = s.Arcs;
        Texture2D glow = GetTexture(s.Assets.Glow);
        Texture2D diamond = GetTexture(s.Assets.Diamond);
        int count = Count(p.GlintCount, 64);
        for (int i = 0; i < count; i++)
        {
            float phase = f.Time * p.GlintSpeed + i * MathHelper.TwoPi / count;
            float pulse = 0.5f + 0.5f * MathF.Sin(f.Time * p.GlintPulseSpeed + i * 2f);
            Vector2 point = new(f.Width * (0.5f + MathF.Cos(phase) * 0.45f),
                f.Center.Y + MathF.Sin(phase) * (f.Baseline - f.Top) * 0.48f);
            Color tint = Color.Lerp(s.BaseColor, s.TopColor, 0.45f + pulse * 0.3f);
            float alpha = (0.45f + pulse * 0.55f) * f.Strength * Falloff(f, point);
            DrawLight(f, glow, point, new Vector2(12f, 14f), s.BaseColor, alpha * 0.30f);
            DrawLight(f, diamond, point, p.GlintSize + p.GlintSizeWave * pulse, tint,
                alpha * p.GlintOpacity, MathF.Sin(phase) * 0.18f);
        }
    }

    private static void DrawOrbitalWisps(in Frame f)
    {
        var s = f.Settings;
        var p = s.Orbit;
        Texture2D glow = GetTexture(s.Assets.Glow);
        Texture2D swirl = GetTexture(s.Assets.Swirl);
        Texture2D smoke = GetTexture(s.Assets.Smoke);
        DrawBackdrop(f, glow);
        for (int side = 0; side < Count(p.WispCount, 16); side++)
        {
            float direction = side % 2 == 0 ? -1f : 1f;
            float phase = f.Time * p.DriftSpeed + side * MathHelper.Pi;
            Vector2 point = new(direction < 0 ? -p.SideOffset : f.Width + p.SideOffset,
                f.Center.Y + MathF.Sin(phase) * p.Drift);
            float breathe = 1f - p.BreatheAmount + p.BreatheAmount * MathF.Sin(f.Time * p.BreatheSpeed + side * 2f);
            float fade = Falloff(f, point);
            Color tint = direction < 0 ? s.BottomColor : s.BaseColor;
            DrawLight(f, smoke, point, p.SmokeSize * breathe, tint, f.Strength * fade * p.SmokeOpacity, direction * f.Time * p.SmokeSpeed);
            DrawLight(f, glow, point, p.WispGlowSize, tint, f.Strength * fade * p.WispGlowOpacity);
            DrawLight(f, swirl, point, p.SwirlSize * breathe, Color.Lerp(tint, s.TopColor, 0.22f),
                f.Strength * fade * p.SwirlOpacity, direction * f.Time * p.SwirlSpeed);
        }
    }

    private static void DrawSatellites(in Frame f)
    {
        var s = f.Settings;
        var p = s.Orbit;
        Texture2D glow = GetTexture(s.Assets.Glow);
        Texture2D star = GetTexture(s.Assets.Spark);
        Vector2 radius = new Vector2(f.Width, f.Baseline - f.Top) * 0.5f + p.RadiusPadding;
        int count = Count(p.SatelliteCount, 32);
        int trailSteps = Count(p.TrailSteps, 64);
        for (int satellite = 0; satellite < count; satellite++)
        {
            float phase = f.Time * p.SatelliteSpeed + satellite * MathHelper.TwoPi / count;
            Color tint = satellite % 2 == 0 ? s.BaseColor : s.TopColor;
            for (int step = trailSteps - 1; step >= 0; step--)
            {
                float trailPhase = phase - step * p.TrailSpacing;
                Vector2 point = f.Center + new Vector2(MathF.Cos(trailPhase), MathF.Sin(trailPhase)) * radius;
                float tail = 1f - step / (float)trailSteps;
                float fade = tail * tail * Falloff(f, point);
                DrawLight(f, glow, point, p.TrailSize + p.TrailSizeWave * tail, tint, f.Strength * fade * p.TrailOpacity);
            }
            Vector2 head = f.Center + new Vector2(MathF.Cos(phase), MathF.Sin(phase)) * radius;
            float pulse = 1f - p.StarPulseAmount + p.StarPulseAmount * MathF.Sin(f.Time * p.StarPulseSpeed + satellite);
            DrawLight(f, star, head, new Vector2(p.StarSize * pulse), s.TopColor,
                f.Strength * Falloff(f, head) * p.StarOpacity, f.Time * p.StarSpin);
        }
    }

    private static float Falloff(in Frame f, Vector2 point)
    {
        var p = f.Settings.Falloff;
        if (!p.Enabled)
            return 1f;
        Vector2 distance = point - f.Center;
        float x = MathF.Abs(distance.X) / Math.Max(f.Width * 0.5f + p.Padding.X, 1f);
        float y = MathF.Abs(distance.Y) / Math.Max((f.Baseline - f.Top) * 0.5f + p.Padding.Y, 1f);
        float sx = MathHelper.Clamp(p.Start.X, 0f, 0.99f);
        float sy = MathHelper.Clamp(p.Start.Y, 0f, 0.99f);
        return (1f - MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp((x - sx) / (1f - sx), 0f, 1f)))
            * (1f - MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp((y - sy) / (1f - sy), 0f, 1f)));
    }

    private static void DrawLight(in Frame f, Texture2D texture, Vector2 localPoint, Vector2 size,
        Color color, float strength, float rotation = 0f)
    {
        if (texture is null || strength <= 0f || size.X <= 0f || size.Y <= 0f)
            return;
        Vector2 point = f.TopLeft + Vector2.Transform(localPoint, f.Transform);
        Vector2 scale = size * f.Scale / new Vector2(texture.Width, texture.Height);
        f.Batch.Draw(texture, point, null, Light(color, strength), f.Rotation + rotation,
            new Vector2(texture.Width, texture.Height) * 0.5f, scale, SpriteEffects.None, f.Depth);
    }

    private static Texture2D GetTexture(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;
        if (Textures.TryGetValue(path, out Asset<Texture2D> asset))
            return asset.Value;
        long now = Environment.TickCount64;
        if (RetryAfter.TryGetValue(path, out long retry) && now < retry)
            return null;
        try
        {
            asset = ModContent.Request<Texture2D>(path, AssetRequestMode.ImmediateLoad);
            Textures[path] = asset;
            RetryAfter.Remove(path);
            return asset.Value;
        }
        catch (Exception ex)
        {
            RetryAfter[path] = now + 1000;
            if (ReportedAssets.Add(path) && ModLoader.TryGetMod("Reese", out Mod mod))
                mod.Logger.Warn($"Menu text theme asset '{path}' could not load; text rendering continues. {ex.Message}");
            return null;
        }
    }

    private static DynamicSpriteFont.SpriteCharacterData GetGlyph(DynamicSpriteFont font, char character)
        => font.SpriteCharacters.TryGetValue(character, out var glyph) ? glyph : font.DefaultCharacterData;

    private static Color Brighten(Color color, float brightness)
    {
        byte alpha = color.A;
        color *= brightness;
        color.A = alpha;
        return color;
    }

    private static Color Light(Color color, float strength)
    {
        color *= MathHelper.Clamp(strength, 0f, 1f);
        color.A = 0;
        return color;
    }

    private static int Count(int value, int max = 128) => Math.Clamp(value, 0, max);
    private static float Fraction(float value) => value - MathF.Floor(value);
    private static float Hash(int seed) => Fraction(MathF.Sin(seed * 127.1f + 311.7f) * 43758.5453f);
}
