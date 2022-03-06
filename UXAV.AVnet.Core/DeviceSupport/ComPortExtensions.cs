using System.Linq;

namespace UXAV.AVnet.Core.DeviceSupport
{
    public static class ComPortExtensions
    {
        public static void Send(this IComPortDevice port, byte[] bytes, int index, int count)
        {
            var str = string.Empty;
            for (var i = index; i < count; i++) str += (char)bytes[i];

            port.Send(str);
        }

        public static void Send(this IComPortDevice port, byte[] bytes)
        {
            var str = bytes.Aggregate(string.Empty, (current, t) => current + (char)t);
            port.Send(str);
        }
    }
}