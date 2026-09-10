using System.ComponentModel;

namespace NetPulseApp.Service.Icons;

/// <summary>
/// PictureBox that displays a Font Awesome glyph. Drop-in replacement for
/// FontAwesome.Sharp's control of the same name.
///
/// Two behaviours are copied deliberately from the original, because call
/// sites depend on them:
///
///  • On resize, IconSize becomes min(Width, Height) and the glyph re-renders
///    at that size — this is what makes Dock = DockStyle.Fill work.
///
///  • The bitmap is always square and the glyph is centered within it, so
///    SizeMode can stay at the PictureBox default of Normal.
/// </summary>
public class IconPictureBox : PictureBox
{
    private IconChar _iconChar = IconChar.None;
    private Color _iconColor = SystemColors.ControlText;
    private int _iconSize = 32;

    public IconPictureBox()
    {
        Size = new Size(_iconSize, _iconSize);
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

    /// <summary>Glyph size in pixels. Call sites pass DPI-scaled values.</summary>
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

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);

        // Matches FontAwesome.Sharp: the icon tracks the control box.
        int fitted = Math.Min(Width, Height);
        if (fitted >= 1 && fitted != _iconSize)
        {
            _iconSize = fitted;
            RefreshIcon();
        }
    }

    private void RefreshIcon()
    {
        // PictureBox does not dispose the Image it is handed, and Render
        // returns a fresh copy each time, so the outgoing one has to be
        // released here or every resize tick leaks a bitmap.
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
