namespace NetPulseApp.Service.Icons;

/// <summary>
/// Font Awesome 6 Free (Solid) glyph codepoints.
///
/// Drop-in replacement for FontAwesome.Sharp's enum of the same name, holding
/// only the icons this template actually uses. Values are the real codepoints,
/// read directly out of the FontAwesome.Sharp 6.6.0 assembly's IconChar enum,
/// so the glyphs render identically to before.
///
/// To add an icon, look up its Unicode value in the Font Awesome 6 Free
/// cheatsheet and confirm it exists in the Solid face — this template ships
/// Solid only, not Regular or Brands.
/// </summary>
public enum IconChar
{
    /// <summary>No icon. Renders as a transparent square.</summary>
    None = 0,

    AddressCard = 0xF2BB,
    Bars = 0xF0C9,
    CheckCircle = 0xF058,
    Copy = 0xF0C5,
    Download = 0xF019,
    Gauge = 0xF624,
    Gear = 0xF013,
    Globe = 0xF0AC,
    HardDrive = 0xF0A0,
    InfoCircle = 0xF05A,
    MagnifyingGlass = 0xF002,
    Moon = 0xF186,
    Play = 0xF04B,
    Plug = 0xF1E6,
    PowerOff = 0xF011,
    SatelliteDish = 0xF7BF,
    Server = 0xF233,
    Star = 0xF005,
    Stop = 0xF04D,
    Trash = 0xF1F8,
    Sun = 0xF185,
    Times = 0xF00D,
    TimesCircle = 0xF057,
    TriangleExclamation = 0xF071,
    WindowRestore = 0xF2D2,
}
