using System.Drawing;

namespace UXAV.AVnet.Core.UI
{
    public static class Extensions
    {
        public static string FormatWithColor(string message, Color color)
        {
            return $"<font color=\"#{color.ToArgb() & 0xFFFFFF:X6}\">{message}</font>";
        }
    }
}