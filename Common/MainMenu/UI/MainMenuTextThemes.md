# Main menu text themes

Both menu labels default to `OriginalFlame` with idle text 12% darker and VFX strength reduced by 30% from the original: the first bright fiery gradient, purple/pink for Pylon and red for Reese. Menu order remains Multiplayer, Pylon, Reese.

## Change a theme

Edit `GetSettings()` at the top of `MainMenuTextThemeDrawer.cs` in the mod whose label you want to change. Replace `MainMenuTextStyle.OriginalFlame` in the `Create(...)` call with:

| Style | Appearance |
| --- | --- |
| `OriginalFlame` | First bright flames, glowing gradient, sparks and stars |
| `MutedFlame` | Slower flames with grey-tinted idle text and stronger hover effects |
| `SoftFlame` | Darker colored idle text, gentle flames and soft outward fading |
| `EnergyArcs` | Lightning ribbons and orbiting diamond glints; Reese has a bright red hover |
| `OrbitalWisps` | Side swirls, smoke and orbiting stars with fading trails |
| `Plain` | Solid colored text and outline, without animation or VFX |

Each mod owns its drawer and the same style enum/settings API so it also works alone. Changing Pylon's settings does not change Reese's. Every listed style and its assets are available in both mods.

## Override any setting

Keep the `Create(...)` call and edit the returned `with` expression. For example:

```csharp
return theme with
{
    BaseColor = new Color(255, 42, 52),
    BottomColor = new Color(174, 8, 30),
    TopColor = new Color(255, 205, 174),
    HoverColorMix = 0f, // Keep these same colors on hover; brightness still changes.
    IdleBrightness = 0.86f,
    HoverBrightness = 1f,
    Speed = 0.75f,
    AnimationEnabled = true,
    IdleEffectStrength = 0.12f,
    HoverEffectStrength = 0.5f,
    Gradient = theme.Gradient with { WaveStrength = 0.04f, HoverShine = 0.18f },
    Flame = theme.Flame with { Count = 7, Strength = 0.2f },
    Falloff = theme.Falloff with { Enabled = true, Padding = new Vector2(10f, 12f) }
};
```

All editable properties and preset values are in `MainMenuTextThemeSettings.cs`:

| Settings | Controls |
| --- | --- |
| Root color/opacity properties | Base, top, bottom, hover targets, outline, idle tint, brightness and text opacity |
| `Speed`, `Phase`, `AnimationEnabled` | Global animation rate and phase; disabling animation freezes it |
| `EffectsEnabled`, effect strength and pulse | VFX visibility, idle/hover intensity and pulsing |
| `Gradient` | Gradient split, waves, shimmer rate/strength and strip resolution |
| `Glow` | Inner/outer glow, backdrop size/opacity and outline |
| `Flame` | Flame count, steps, width, height, bend, motion, fade and fire swirls |
| `Sparks` | Spark count, motion, size, opacity, trails and star twinkles |
| `Arcs` | Ribbon count, segments, dimensions, motion, opacity and glints |
| `Orbit` | Wisps, smoke, swirls, satellites, trail geometry and motion |
| `Falloff` | Soft outward fade: padding and the fraction of each half-extent where fading starts |
| `Assets` | Full texture paths for glow, flames, sparks, swirls, smoke, ribbons and diamonds |

Colors use 0-255 channels. Opacities and fade-start fractions usually use 0-1. Sizes are font-local pixels before the menu's existing scale; rates are multiplied by `Speed`. Only the selected style uses its corresponding effect group. Counts of zero disable that particle layer. `EffectsEnabled = false` retains the text gradient and outline; `GradientEnabled = false` uses the base color.

## Swap assets

Set an asset property using a full mod path without an extension:

```csharp
Assets = theme.Assets with { Spark = "Pylon/Assets/Effects/VFX/PixelTwinkle" }
```

For Reese use `Reese/Assets/Effects/Menu/PixelTwinkle`. Existing assets are retained, and missing assets needed by the additional presets have been copied into each mod. Empty paths disable that texture. Missing paths skip that effect, log once per path, and retry once per second; the text remains visible. Asset wrappers are cached by path, so switching paths takes effect on the next draw and supported texture hot reload can replace the wrapper's value.

## Apply changes while running

Build/reload both mods once to introduce the new classes and assets. In a debug session that supports C# Hot Reload, edit `GetSettings()` (or a preset method body), then apply the IDE's Hot Reload action. The next menu draw reads the new values; reopening the menu is unnecessary.

Keep tuning overrides inside `GetSettings()`, rather than static field initializers. Editing existing method bodies is the intended live workflow. New enum members, properties, types or newly packaged assets can require another build/reload. Saving a source file by itself does not attach a debugger or apply C# Hot Reload; outside a supported debug session, use the normal mod build/reload.

