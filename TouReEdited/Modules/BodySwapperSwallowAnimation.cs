using Reactor.Utilities;
using System.Collections;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TouReEdited.Modules;

// A Body Swapper decoy eating its victim: the drawn corpse (Resources/BodySwapperSwallow, 24 frames) rears up into
// a toothed maw, lunges left, gulps the victim down and lies back down. The frames are drawn in red and repainted
// in the decoy's own colours (its body and shadow colour; outlines, teeth, bone and the dark inside of the maw
// stay as drawn). The victim is the real player standing where the maw closes; it vanishes on frame 12.
public sealed class BodySwapperSwallowAnimation
{
    private const string SheetResource = "TouReEdited.Resources.BodySwapperSwallow.BodySwapper_Swallow_Anim.png";
    private const int Columns = 4;
    private const int Rows = 6;
    private const int FrameCount = 24;
    private const int VictimVanishFrame = 12;
    private const int SwallowSoundFrame = 16;
    private const float PixelsPerUnit = 100f;

    // On the cropped frames (356 x 212 px, from the top left): the lying corpse of the first frame (its centre
    // and width) and the feet of the victim the maw closes on.
    private static readonly Vector2 CorpseCentrePixel = new(254f, 170f);
    private const float FrameWidthPixels = 356f;
    private const float FrameHeightPixels = 212f;
    private const float CorpseWidthPixels = 178f;
    private static readonly Vector2 VictimFeetPixel = new(64f, 200f);

    private static readonly float[] Durations =
    [
        0.09f, 0.075f, 0.075f, 0.075f, 0.075f, 0.075f, 0.075f, 0.105f, 0.075f, 0.075f, 0.075f, 0.12f,
        0.095f, 0.075f, 0.075f, 0.12f, 0.075f, 0.075f, 0.075f, 0.095f, 0.075f, 0.075f, 0.075f, 0.12f
    ];

    // The drawn frames' pixels, read once, and the repainted frames for each decoy colour.
    private static Color32[]? _sourcePixels;
    private static int _sheetWidth;
    private static int _sheetHeight;
    private static readonly Dictionary<int, Sprite[]> Painted = [];
    private static readonly HashSet<int> Painting = [];

    public static float Duration => Durations.Sum();

    // Time from the start until the victim is gone.
    public static float SwallowTime => Durations.Take(VictimVanishFrame - 1).Sum();

    private readonly GameObject? decoy;
    private readonly PlayerControl victim;
    private readonly Sprite[] frames;
    private readonly SpriteRenderer renderer;
    private readonly List<SpriteRenderer> hiddenDecoyParts = [];
    private float time;
    private int frame = -1;
    private bool swallowed;
    private bool finished;

    public BodySwapperSwallowAnimation(GameObject? decoyVisual, PlayerControl victim, int decoyColorId)
    {
        decoy = decoyVisual;
        this.victim = victim;
        frames = FramesFor(decoyColorId);

        var holder = new GameObject("BodySwapperSwallow");
        renderer = holder.AddComponent<SpriteRenderer>();
        var bounds = DecoyBounds(decoyVisual, out var sorting);
        if (sorting)
        {
            renderer.sortingLayerID = sorting!.sortingLayerID;
            renderer.sortingOrder = sorting.sortingOrder;
        }

        // The drawn corpse is laid exactly over the real one, at its size; the maw goes in front of the victim.
        var unitsPerPixel = UnitsPerPixel(bounds);
        var centre = new Vector2(FrameWidthPixels / 2f, FrameHeightPixels / 2f);
        var offset = new Vector2(centre.x - CorpseCentrePixel.x, CorpseCentrePixel.y - centre.y) * unitsPerPixel;
        var victimZ = victim ? victim.transform.position.z : bounds.center.z;
        var z = Mathf.Min(bounds.center.z, victimZ) - 0.001f;
        holder.transform.position = new Vector3(bounds.center.x + offset.x, bounds.center.y + offset.y, z);
        var scale = unitsPerPixel * PixelsPerUnit;
        holder.transform.localScale = new Vector3(scale, scale, 1f);
        renderer.enabled = false;
    }

    // Where the victim has to stand (its true position, at the feet) for the maw to close on it.
    public static Vector2 VictimFeetFor(GameObject? decoyVisual)
    {
        var bounds = DecoyBounds(decoyVisual, out _);
        var unitsPerPixel = UnitsPerPixel(bounds);
        var fromCorpse = new Vector2(VictimFeetPixel.x - CorpseCentrePixel.x, CorpseCentrePixel.y - VictimFeetPixel.y);
        return (Vector2)bounds.center + fromCorpse * unitsPerPixel;
    }

    // Advances the animation; returns false once it is over.
    public bool Step(float deltaTime, Action onSwallow)
    {
        if (finished)
        {
            return false;
        }

        if (frame < 0)
        {
            HideDecoy();
        }

        time += deltaTime;
        var index = 0;
        var elapsed = time;
        while (index < FrameCount - 1 && elapsed >= Durations[index])
        {
            elapsed -= Durations[index];
            index++;
        }

        if (index != frame)
        {
            for (var passed = frame + 1; passed <= index; passed++)
            {
                if (passed + 1 == VictimVanishFrame && !swallowed)
                {
                    swallowed = true;
                    if (victim && victim.Visible)
                    {
                        victim.Visible = false;
                    }

                    onSwallow();
                }

                if (passed + 1 == SwallowSoundFrame)
                {
                    PlaySwallowSound();
                }
            }

            frame = index;
            if (index < frames.Length)
            {
                renderer.sprite = frames[index];
                renderer.enabled = true;
            }
        }

        if (time >= Duration)
        {
            finished = true;
            return false;
        }

        return true;
    }

    public void Dispose()
    {
        foreach (var part in hiddenDecoyParts)
        {
            if (part)
            {
                part.enabled = true;
            }
        }

        if (renderer)
        {
            Object.Destroy(renderer.gameObject);
        }
    }

    // The drawn frames include the corpse, so the real one is hidden while they play.
    private void HideDecoy()
    {
        if (!decoy)
        {
            return;
        }

        foreach (var part in decoy!.GetComponentsInChildren<SpriteRenderer>())
        {
            if (part.enabled)
            {
                part.enabled = false;
                hiddenDecoyParts.Add(part);
            }
        }
    }

    private void PlaySwallowSound()
    {
        var local = PlayerControl.LocalPlayer;
        if (local == null || !SoundManager.Instance || !victim)
        {
            return;
        }

        var volume = local.PlayerId == victim.PlayerId
            ? 1f
            : Mathf.Clamp01(1f - Vector2.Distance(local.GetTruePosition(), victim.GetTruePosition()) / 6f) * 0.8f;
        if (volume > 0.01f)
        {
            SwallowSound.Play(volume);
        }
    }

    // The corpse's sprites, without the blood splatter (which can reach far beyond the body).
    private static Bounds DecoyBounds(GameObject? decoyVisual, out SpriteRenderer? sorting)
    {
        sorting = null;
        var bounds = new Bounds(Vector3.zero, Vector3.zero);
        var found = false;
        if (decoyVisual)
        {
            foreach (var part in decoyVisual!.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (!part.sprite || part.name.Contains("Blood", StringComparison.OrdinalIgnoreCase) ||
                    part.name.Contains("Splat", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                sorting ??= part;
                if (!found)
                {
                    bounds = part.bounds;
                    found = true;
                }
                else
                {
                    bounds.Encapsulate(part.bounds);
                }
            }

            if (!found)
            {
                bounds = new Bounds(decoyVisual.transform.position, new Vector3(0.8f, 0.3f, 0f));
            }
        }

        return bounds;
    }

    // Drawn a bit larger than the real corpse: at exactly its size the maw looked too small for the player it swallows.
    // The victim's standing point (VictimFeetFor) uses the same scale, so the maw still closes right on them.
    private const float MawScale = 1.18f;

    private static float UnitsPerPixel(Bounds corpse) =>
        Mathf.Clamp(corpse.size.x / CorpseWidthPixels, 0.0035f, 0.0065f) * MawScale;

    // ---------------- Frames in the decoy's colours ----------------

    // Reads the drawn sheet ahead of time (when a Body Swapper is in the game).
    public static void Preload()
    {
        try
        {
            if (_sourcePixels != null)
            {
                return;
            }

            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(SheetResource);
            if (stream == null)
            {
                Error($"Body Swapper: swallow sheet not found: {SheetResource}");
                _sourcePixels = [];
                return;
            }

            using var memory = new MemoryStream();
            stream.CopyTo(memory);
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            ImageConversion.LoadImage(texture, memory.ToArray());
            _sheetWidth = texture.width;
            _sheetHeight = texture.height;
            var pixels = texture.GetPixels32();
            _sourcePixels = new Color32[pixels.Length];
            for (var i = 0; i < pixels.Length; i++)
            {
                _sourcePixels[i] = pixels[i];
            }

            Object.Destroy(texture);
        }
        catch (Exception ex)
        {
            Error($"Body Swapper: swallow sheet failed to load: {ex}");
            _sourcePixels = [];
        }
    }

    // Starts repainting the frames for a decoy's colour, a slice per game frame, so it is ready (without a stall)
    // by the time the decoy springs.
    public static void PrepareColour(int colorId)
    {
        Preload();
        if (Painted.ContainsKey(colorId) || !Painting.Add(colorId))
        {
            return;
        }

        Coroutines.Start(CoPaint(colorId));
    }

    private static Sprite[] FramesFor(int colorId)
    {
        Preload();
        if (Painted.TryGetValue(colorId, out var ready))
        {
            return ready;
        }

        // Not ready yet (the trap sprang right away): finish it now.
        var routine = CoPaint(colorId);
        while (routine.MoveNext())
        {
        }

        return Painted.TryGetValue(colorId, out ready) ? ready : [];
    }

    private static IEnumerator CoPaint(int colorId)
    {
        Painting.Add(colorId);
        var source = _sourcePixels;
        if (source == null || source.Length == 0)
        {
            Painted[colorId] = [];
            Painting.Remove(colorId);
            yield break;
        }

        var body = (Color)Palette.PlayerColors[Mathf.Abs(colorId) % Palette.PlayerColors.Length];
        var shadow = (Color)Palette.ShadowColors[Mathf.Abs(colorId) % Palette.ShadowColors.Length];
        var painted = new Color32[source.Length];
        const int slice = 120000;
        for (var start = 0; start < source.Length; start += slice)
        {
            if (Painted.ContainsKey(colorId))
            {
                yield break;
            }

            var end = Math.Min(source.Length, start + slice);
            for (var i = start; i < end; i++)
            {
                painted[i] = Repaint(source[i], body, shadow);
            }

            yield return null;
        }

        if (Painted.ContainsKey(colorId))
        {
            yield break;
        }

        var texture = new Texture2D(_sheetWidth, _sheetHeight, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave
        };
        texture.SetPixels32(painted);
        texture.Apply();

        var frameWidth = _sheetWidth / Columns;
        var frameHeight = _sheetHeight / Rows;
        var sprites = new Sprite[FrameCount];
        for (var i = 0; i < FrameCount; i++)
        {
            var column = i % Columns;
            var row = i / Columns;
            var rect = new Rect(column * frameWidth, (Rows - 1 - row) * frameHeight, frameWidth, frameHeight);
            sprites[i] = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), PixelsPerUnit);
            sprites[i].hideFlags = HideFlags.HideAndDontSave;
        }

        Painted[colorId] = sprites;
        Painting.Remove(colorId);
    }

    // Bright reds become the body colour, mid reds its shadow and dark reds a darker shadow; pinkish flesh (tongue),
    // near-black, outlines and white teeth or bone are kept as drawn. Lighter, whiter reds keep their lightness.
    private static Color32 Repaint(Color32 pixel, Color body, Color shadow)
    {
        int r = pixel.r, g = pixel.g, b = pixel.b;
        if (pixel.a == 0 || r < g + 40 || r < b + 40 || b - g > 25)
        {
            return pixel;
        }

        var value = r / 255f;
        if (value < 0.22f)
        {
            return pixel;
        }

        float red, green, blue;
        if (value < 0.5f)
        {
            // Dark reds (deep shading, the inside of the maw) become a darkened shadow colour.
            var dark = 0.35f + 0.65f * (value - 0.22f) / 0.28f;
            red = shadow.r * dark;
            green = shadow.g * dark;
            blue = shadow.b * dark;
        }
        else
        {
            var shade = Math.Clamp((value - 0.5f) / 0.38f, 0f, 1f);
            red = shadow.r + (body.r - shadow.r) * shade;
            green = shadow.g + (body.g - shadow.g) * shade;
            blue = shadow.b + (body.b - shadow.b) * shade;
        }

        var white = Math.Clamp(Math.Min(g, b) / (float)r, 0f, 1f);
        red += (1f - red) * white;
        green += (1f - green) * white;
        blue += (1f - blue) * white;

        var result = pixel;
        result.r = (byte)(Math.Clamp(red, 0f, 1f) * 255f);
        result.g = (byte)(Math.Clamp(green, 0f, 1f) * 255f);
        result.b = (byte)(Math.Clamp(blue, 0f, 1f) * 255f);
        return result;
    }

    // The swallow sound lives in the extension's audio (Pelican); both are merged into one assembly.
    private static class SwallowSound
    {
        private static PropertyInfo? _property;

        public static void Play(float volume)
        {
            try
            {
                _property ??= typeof(BodySwapperSwallowAnimation).Assembly
                    .GetType("TouReEdited.Assets.TouExtensionAudio")
                    ?.GetProperty("SwallowSound", BindingFlags.Public | BindingFlags.Static);
                if (_property?.GetValue(null) is LoadableAsset<AudioClip> asset && asset.LoadAsset() is { } clip)
                {
                    SoundManager.Instance.PlaySound(clip, false, volume);
                }
            }
            catch (Exception ex)
            {
                Warning($"Body Swapper: swallow sound failed: {ex.Message}");
            }
        }
    }
}
