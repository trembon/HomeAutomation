using System;
using System.Drawing;

namespace HomeAutomation.Base.Extensions;

public static class ColorExtensions
{
    public static string ToHSVString(this Color color)
    {
        int max = Math.Max(color.R, Math.Max(color.G, color.B));
        int min = Math.Min(color.R, Math.Min(color.G, color.B));

        double hue = color.GetHue();
        double saturation = ((max == 0) ? 0 : 1d - (1d * min / max)) * 1000;
        double value = max / 255d * 1000;

        return $"{(int)hue:X4}{(int)saturation:X4}{(int)value:X4}";
    }
}
