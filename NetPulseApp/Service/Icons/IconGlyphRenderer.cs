using System.Drawing.Text;
using System.Runtime.InteropServices;

namespace NetPulseApp.Service.Icons;

/// <summary>
/// Renders <see cref="IconChar"/> glyphs to bitmaps using an embedded copy of
/// Font Awesome 6 Free Solid.
///
/// This exists so the template can drop the FontAwesome.Sharp package. That
/// library ships one assembly serving both WinForms and WPF, and stores its
/// fonts as a WPF resource container reachable only through a pack URI — which
/// forced the entire WPF framework (17.6 MB compressed) into a WinForms app.
/// Here the font is a plain embedded resource and the glyph is drawn with GDI+.
/// </summary>
internal static class IconGlyphRenderer
{
    #region Font loading

    // The unmanaged block backing the font MUST outlive every Font created from
    // the collection — GDI+ reads through this pointer lazily, so freeing it
    // yields intermittently corrupt glyphs rather than a clean failure. Both
    // this and the collection are deliberately process-lifetime and never
    // disposed.
    private static readonly PrivateFontCollection FontCollection = new();
    private static readonly FontFamily IconFamily;

    static IconGlyphRenderer()
    {
        byte[] fontBytes = ReadEmbeddedFont();

        IntPtr fontData = Marshal.AllocCoTaskMem(fontBytes.Length);
        Marshal.Copy(fontBytes, 0, fontData, fontBytes.Length);
        FontCollection.AddMemoryFont(fontData, fontBytes.Length);

        IconFamily = FontCollection.Families[0];
    }

    private static byte[] ReadEmbeddedFont()
    {
        var assembly = typeof(IconGlyphRenderer).Assembly;

        // Matched by suffix rather than by the full manifest name so a change
        // of root namespace or folder can't silently break icon rendering.
        string resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith(FontFileName, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException(
                $"Embedded icon font '{FontFileName}' was not found. It must be included " +
                "as an EmbeddedResource in NetPulseApp.csproj.");

        using Stream stream = assembly.GetManifestResourceStream(resourceName)!;
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return buffer.ToArray();
    }

    private const string FontFileName = "fa-solid-900.ttf";

    #endregion

    #region Rendering

    // Rasterising a glyph is far more expensive than blitting one, and the
    // sidebar re-renders its icons on every resize tick, so keep the results.
    // Callers get a copy (see Render) because WinForms controls dispose the
    // Image they are handed.
    private static readonly Dictionary<(IconChar Icon, int Argb, int Size), Bitmap> Cache = new();
    private static readonly object CacheLock = new();
    private const int MaxCacheEntries = 256;

    /// <summary>
    /// Renders <paramref name="icon"/> centered in a transparent square bitmap
    /// of <paramref name="size"/> pixels. The caller owns the returned bitmap
    /// and is responsible for disposing it.
    /// </summary>
    public static Bitmap Render(IconChar icon, Color color, int size)
    {
        if (size < 1)
        {
            size = 1;
        }

        var key = (icon, color.ToArgb(), size);

        lock (CacheLock)
        {
            if (!Cache.TryGetValue(key, out Bitmap? cached))
            {
                // A resize sweep can mint a bitmap per intermediate size. Drop
                // the lot rather than track ages — these are cheap to rebuild.
                if (Cache.Count >= MaxCacheEntries)
                {
                    foreach (Bitmap stale in Cache.Values)
                    {
                        stale.Dispose();
                    }

                    Cache.Clear();
                }

                cached = Rasterize(icon, color, size);
                Cache[key] = cached;
            }

            return (Bitmap)cached.Clone();
        }
    }

    private static Bitmap Rasterize(IconChar icon, Color color, int size)
    {
        var bitmap = new Bitmap(size, size, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

        if (icon == IconChar.None)
        {
            return bitmap; // transparent square, same as the old default
        }

        using var graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

        // GraphicsUnit.Pixel because every call site already passes a
        // DPI-scaled pixel value from DpiAwareService.Scale(); points would
        // scale a second time.
        using var font = new Font(IconFamily, size, FontStyle.Regular, GraphicsUnit.Pixel);

        // Centering by layout box, which is what FontAwesome.Sharp did.
        using var format = new StringFormat(StringFormat.GenericTypographic)
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center,
            FormatFlags = StringFormatFlags.NoWrap | StringFormatFlags.NoClip,
            Trimming = StringTrimming.None
        };

        using var brush = new SolidBrush(color);
        graphics.DrawString(ToGlyph(icon), font, brush, new RectangleF(0, 0, size, size), format);

        return bitmap;
    }

    /// <summary>Converts a codepoint to its UTF-16 string form.</summary>
    private static string ToGlyph(IconChar icon) => char.ConvertFromUtf32((int)icon);

    #endregion
}
