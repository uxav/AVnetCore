using System.Drawing;

namespace UXAV.AVnetCore.UI
{
    public static class Extensions
    {
        private static string FormatWithColor(string message, Color color)
        {
            return $"<font color=\"#{color.ToArgb() & 0xFFFFFF:X6}\">{message}</font>";
        }
    }
}