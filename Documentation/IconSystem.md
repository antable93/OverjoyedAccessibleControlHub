# Icon System

The icon system rasterizes SVG assets into platform images at the exact pixel size needed by the view displaying them. Widgets render their own icons ad hoc at whatever size their geometry calls for; menus and the controller editor instead pull pre-generated icons from `IconManager` by name.

---

## Files

| File | Location |
|---|---|
| `ISvgRasterizer.cs` | `Icons/ISvgRasterizer.cs` |
| `SkiaSharpSvgRasterizer.cs` | `Icons/SkiaSharpSvgRasterizer.cs` |
| `Icon.cs` | `Icons/Icon.cs` |
| `IconManager.cs` | `Icons/IconManager.cs` |

---

## Classes

### `ISvgRasterizer`

Interface for converting an SVG stream into a MAUI `IImage`.

```csharp
IImage? ResterizeSvg(Stream svgStream, int height, int width, int r, int g, int b, int a);
```

| Parameter | Description |
|---|---|
| `svgStream` | Readable stream containing SVG markup |
| `height` / `width` | Output pixel dimensions |
| `r`, `g`, `b`, `a` | Tint color applied to the rasterized image (0–255 each, `a` defaults to 255) |

The interface exists so alternative rasterizers (e.g. a test stub or a future platform-native renderer) can be swapped in without touching `Icon` or any view.

---

### `SkiaSharpSvgRasterizer`

Concrete implementation backed by **SkiaSharp** (`SkiaSharp 2.88.x`) and **Svg.Skia** for SVG parsing.

**Pipeline:**

1. `Svg.Skia.SKSvg` parses the SVG stream into an `SKPicture`
2. The picture is drawn into an `SKBitmap` at the requested `width` × `height`, scaled to fit
3. A second pass draws that bitmap through an `SKPaint` with `SKColorFilter.CreateBlendMode(tint, SrcIn)` — this replaces every pixel's color with the tint while preserving the original alpha channel, producing a correctly shaped silhouette in the chosen color
4. The tinted bitmap is encoded as PNG and loaded into a MAUI `IImage` via `PlatformImage.FromStream`

**Color conversion (`ConvertColor`):**

```csharp
private SKColor ConvertColor(int r, int g, int b, int a)
```

Converts the RGBA integers to a MAUI `Color`, extracts HSL components (note: MAUI returns S and L in 0–1, so both are multiplied by 100 before passing to `SKColor.FromHsl`), then applies alpha via `.WithAlpha`.

---

### `Icon`

Pairs an SVG source path with its loaded `IImage`. Construction and loading are merged into a single async factory so a caller can never hold an unloaded `Icon`.

```csharp
public static async Task<Icon> GenerateAsync(
    string svgSource,
    int width = 64, int height = 64,
    int r = 255, int g = 255, int b = 255, int a = 255)
```

The rasterizer is a static `SkiaSharpSvgRasterizer` shared across all instances. Color and size default to a 64×64 white icon.

The resulting `IImage` is stored on `Visual`. If loading fails, `Visual` remains `null` and the error is written to debug output — the view simply skips rendering that icon.

---

### `IconManager`

Static class (same pattern as `SettingsManager`) that pre-generates and caches every icon used by menus and the controller editor, tinted with the current `FontColor`. Widgets don't use this cache — they render ad hoc via `Icon.GenerateAsync` directly, since their icons are arbitrary user-chosen SVGs sized to the widget's own geometry (see below).

```csharp
public static Task Initialize();
public static Icon? GetIcon(string name);
public static string GetSvgPath(string name);
public static Task RecolorAllAsync();
public static event EventHandler? IconsRecolored;
```

- **Manifest** — a private `Dictionary<string, string>` mapping icon *name* (e.g. `"gamepad"`, `"keyboard_a"`, `"xbox_stick_l"`) to its app-package SVG path (e.g. `"Icons/Misc/gamepad.svg"`). Names are the SVG filename without directory or extension.
- **Size** — every icon is generated at 64×64 by default; `dial` and `joystick` (the widget-type cards) are the only entries overridden to 128×128. Smaller UI (24px `IconButton` nav icons, 32px icon-list thumbnails) just displays the 64px bitmap scaled down — downscaling doesn't blur, so one size covers every menu/editor use case.
- **`Initialize()`** — generates every manifest entry once, tinted with `SettingsManager.FontColor`, and subscribes to `SettingsManager.FontColorChanged` to trigger `RecolorAllAsync()` automatically from then on. Called from `MainPage.OnAppearing`, before `ControllerManager.Initialize()`.
- **`GetIcon(name)`** — returns the cached `Icon`, or `null` if the name isn't in the manifest.
- **`GetSvgPath(name)`** — resolves a name to its SVG path without touching the cache. This is what widgets use to turn a slot's stored icon name into a real path for their own ad hoc `Icon.GenerateAsync` call. Falls back to the `"question"` icon's path for unknown names.
- **`RecolorAllAsync()`** — regenerates every cached icon with the current `FontColor` and raises `IconsRecolored`. Menus/editor UI subscribe to this event to reassign their `Image.Source` to the freshly-generated icon (the old `ImageSource` doesn't update in place).

---

## How a View Uses the Icon System

The `Dial` is the reference example. The key principle is that **the view knows the display size, so the view is responsible for loading icons at that size**.

### 1. Bindings carry only the icon name

`SlotData.SvgSource` is an icon name like `"keyboard_a"` — the same name used as a key into `IconManager`'s manifest. No `Icon` object is created at binding time. The default value is `"question"` — a question mark glyph rendered when no icon has been chosen yet. Widgets resolve the name to a real SVG path via `IconManager.GetSvgPath(slotData.SvgSource)` right before calling `Icon.GenerateAsync`.

### 2. Icons are cached per slot

`Dial` maintains a `Dictionary<string, Icon?>` keyed by slot ID (e.g. `"Quadrant_0"`, `"UpperRing"`). Each slot has its own `Icon` because different regions display icons at different sizes.

### 3. Loading is triggered by construction and binding changes

```
constructor()                      LoadIconForSlotAsync(slotId)
        │                                        │
_ = LoadIconsAsync()               icons.Remove(slotId)
        │                                        │
        └──── per slot ───────────────────────────┘
                      │
              GetSlotIconSize(slotId)   ← geometry-based size per slot
                      │
              IconManager.GetSvgPath(slotData.SvgSource)
                      │
              Icon.GenerateAsync(svgPath, size, size)
                      │
              icons[slotId] = icon
                      │
              Invalidate()             ← redraws as each icon becomes ready
```

Icons are loaded once when the widget is constructed, since there's only one `Widget` instance per descriptor shared by both the live and editor canvases. `LoadIconForSlotAsync` handles single-slot refreshes after a binding change in the settings UI (e.g. picking a different icon in `DialOptionsPanel`'s icon list).

### 4. Size is computed from dial geometry

Each slot's icon size matches the space available for it in the rendered layout:

| Slot | Size formula |
|---|---|
| `Quadrant_N` | `Min(dialR − deadZoneR, 2r·sin(sweep/2)) × 0.55` where `r` is the midpoint radius |

The same formulas appear in both `GetSlotIconSize` (used at load time) and `DrawIcons` (used at draw time), so the rasterized pixel dimensions always match the draw dimensions exactly.

### 5. Drawing uses the image's own dimensions

`DrawIcons` calls `canvas.DrawImage` directly, reading `icon.Width` and `icon.Height` from the already-rasterized image rather than recomputing the size:

```csharp
canvas.DrawImage(v, cx - v.Width / 2f, cy - v.Height / 2f, v.Width, v.Height);
```

No scaling happens at draw time — the bitmap was produced at the right size upfront.

---

## Adding Icon Support to a New View

1. Inherit from `Widget` — `icons` and the other shared fields are already provided
2. Override `LoadIconsAsync` — iterate all bound slots, compute the display size for each, resolve `slotData.SvgSource` to a path via `IconManager.GetSvgPath`, and call `Icon.GenerateAsync` (fire-and-forget with `_ =`; call `Invalidate()` after all slots are loaded)
3. Override `LoadIconForSlotAsync` — remove the old entry from `icons`, load the new icon for just that slot, and call `Invalidate()`
4. Call `_ = LoadIconsAsync()` at the end of each concrete subclass constructor
5. In your draw method, look up `icons[slotId]` and call `canvas.DrawImage` directly using the image's own width/height

If your new view needs icons in a menu or editor panel rather than on the widget canvas itself, use `IconManager.GetIcon(name)` instead — don't generate them ad hoc.
