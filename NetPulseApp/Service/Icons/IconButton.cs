using System.ComponentModel;

namespace NetPulseApp.Service.Icons;

/// <summary>
/// Button that displays a Font Awesome glyph alongside its text. Drop-in
/// replacement for FontAwesome.Sharp's control of the same name.
///
/// Unlike <see cref="IconPictureBox"/> the glyph does NOT track the control
/// size — a button is usually much wider than its icon. It renders at
/// IconSize and nothing else, matching FontAwesome.Sharp's own behavior.
/// </summary>
public class IconButton : Button
{
    private IconChar _iconChar = IconChar.None;
    private Color _iconColor = Color.Black;
    private int _iconSize = 48;

    public IconButton()
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

    private void RefreshIcon()
    {
        // Button does not dispose the Image it is handed, and Render returns a
        // fresh copy each time, so release the outgoing one here.
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
