using System.ComponentModel;

namespace NetPulseApp.Service.Icons;

/// <summary>
/// ToolStripMenuItem that displays a Font Awesome glyph. Drop-in replacement
/// for FontAwesome.Sharp's control of the same name — used by
/// SystemTrayService's context menu. Same rendering approach as
/// <see cref="IconButton"/>, just on a menu item instead of a button.
/// </summary>
public class IconMenuItem : ToolStripMenuItem
{
    private IconChar _iconChar = IconChar.None;
    private Color _iconColor = Color.Black;
    private int _iconSize = 16;

    public IconMenuItem()
    {
        RefreshIcon();
    }

    /// <summary>Glyph to display.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public IconChar IconChar
    {
        get => _iconChar;
        set
        {
            if (_iconChar == value)
            {
                return;
            }

            _iconChar = value;
            RefreshIcon();
        }
    }

    /// <summary>Glyph colour.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color IconColor
    {
        get => _iconColor;
        set
        {
            if (_iconColor == value)
            {
                return;
            }

            _iconColor = value;
            RefreshIcon();
        }
    }

    /// <summary>Glyph size in pixels.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int IconSize
    {
        get => _iconSize;
        set
        {
            if (_iconSize == value || value < 1)
            {
                return;
            }

            _iconSize = value;
            RefreshIcon();
        }
    }

    private void RefreshIcon()
    {
        // ToolStripItem does not dispose the Image it is handed, and Render
        // returns a fresh copy each time, so release the outgoing one here.
        Image? previous = Image;
        Image = IconGlyphRenderer.Render(_iconChar, _iconColor, _iconSize);
        previous?.Dispose();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Image? current = Image;
            Image = null;
            current?.Dispose();
        }

        base.Dispose(disposing);
    }
}
