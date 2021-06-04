using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Crestron.SimplSharp.CrestronIO;
using Crestron.SimplSharpPro;
using Crestron.SimplSharpPro.DM;
using UXAV.AVnet.Core.Models;
using UXAV.Logging;

namespace UXAV.AVnet.Core
{
    public static class Tools
    {
        /// <summary>
        /// Extends a normal System.IO.Stream and gets a Crestron.SimplSharp.CrestronIO.Stream
        /// </summary>
        /// <param name="stream"></param>
        /// <returns>Crestron.SimplSharp.CrestronIO.Stream</returns>
        public static Stream GetCrestronStream(this System.IO.Stream stream)
        {
            stream.Position = 0;
            var buffer = new byte[81920];
            int read;
            var result = new MemoryStream();
            while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
            {
                result.Write(buffer, 0, read);
            }

            result.Position = 0;
            return result;
        }

        /// <summary>
        /// Extends a normal Crestron.SimplSharp.CrestronIO.Stream and gets a System.IO.Stream
        /// </summary>
        /// <param name="stream"></param>
        /// <returns>System.IO..Stream</returns>
        public static System.IO.Stream GetNormalStream(this Stream stream)
        {
            var buffer = new byte[81920];
            int read;
            var result = new System.IO.MemoryStream();
            while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
            {
                result.Write(buffer, 0, read);
            }

            result.Position = 0;
            return result;
        }

        /// <summary>
        /// Tool to return a string showing readable representation of raw bytes in the form of "/x02/x03" etc
        /// </summary>
        /// <param name="bytes">Byte array to convert</param>
        /// <param name="startIndex">Array start index</param>
        /// <param name="length">Count of bytes to read from the start index</param>
        /// <param name="showReadable">Show readable chars as normal if true</param>
        /// <returns></returns>
        public static string GetBytesAsReadableString(byte[] bytes, int startIndex, int length, bool showReadable)
        {
            var result = string.Empty;

            for (var i = startIndex; i < length; i++)
            {
                if (showReadable && bytes[i] >= 32 && bytes[i] < 127)
                {
                    result = result + $"{(char) bytes[i]}";
                }
                else
                {
                    result = result + $"\\x{bytes[i]:X2}";
                }
            }

            return result;
        }

        /// <summary>
        /// Scale a number range
        /// </summary>
        /// <param name="value">Number to scale</param>
        /// <param name="fromMinValue">The current min value of the number</param>
        /// <param name="fromMaxValue">The current max value of the number</param>
        /// <param name="toMinValue">The new min value of the new range</param>
        /// <param name="toMaxValue">The new max value of the new range</param>
        /// <returns></returns>
        public static double ScaleRange(double value,
            double fromMinValue, double fromMaxValue,
            double toMinValue, double toMaxValue)
        {
            try
            {
                return (value - fromMinValue) *
                    (toMaxValue - toMinValue) /
                    (fromMaxValue - fromMinValue) + toMinValue;
            }
            catch
            {
                return double.NaN;
            }
        }

        public static double ScaleRange(double value,
            double fromMinValue, double fromMaxValue,
            double toMinValue, double toMaxValue, int decimalPlaces)
        {
            try
            {
                return Math.Round((value - fromMinValue) *
                    (toMaxValue - toMinValue) /
                    (fromMaxValue - fromMinValue) + toMinValue, decimalPlaces);
            }
            catch
            {
                return double.NaN;
            }
        }

        public static double GetPercentage(this ushort ushortValue, int decimalPlaces = 0)
        {
            return ScaleRange(ushortValue, ushort.MinValue, ushort.MaxValue, 0, 1, decimalPlaces);
        }

        public static SystemBase CreateSystem(this CrestronControlSystem controlSystem, Assembly assembly,
            string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
            {
                throw new ArgumentException("string cannot be empty", nameof(typeName));
            }

            var systemType = assembly.GetType(typeName);
            var ctor = systemType.GetConstructor(new[] {typeof(CrestronControlSystem)});
            if (ctor == null)
            {
                throw new Exception($"Could not get ctor for type: {systemType.FullName}");
            }

            try
            {
                return (SystemBase) ctor.Invoke(new object[] {controlSystem});
            }
            catch (TargetInvocationException e)
            {
                Logger.Error(e.InnerException);
                throw;
            }
        }

        public static string DevicePortAddressCreate(string device, uint ipId, uint port = 0)
        {
            return $"{device}:{ipId:X2}:{port:D2}";
        }

        public static string DevicePortAddressCreate(string device, uint ipId, string ipAddress)
        {
            return $"{device}:{ipId:X2}:{ipAddress}";
        }

        public static DevicePortAddress DevicePortAddressParse(string addressString)
        {
            var match = Regex.Match(addressString, @"^(?:([\w\.]+)\.)?([\w\.]+):(\w{2})(?::(?:(\d{1,2})|([\w\.]+)))?$");
            if (!match.Success) throw new ArgumentException("Incorrect format for address", nameof(addressString));
            var result = new DevicePortAddress
            {
                NameSpace = match.Groups[1].Value,
                Device = match.Groups[2].Value,
                IpId = uint.Parse(match.Groups[3].Value, NumberStyles.HexNumber)
            };
            if (match.Groups[4].Success)
            {
                result.Port = uint.Parse(match.Groups[4].Value);
            }

            if (match.Groups[5].Success)
            {
                result.Address = match.Groups[5].Value;
            }

            return result;
        }

        public struct DevicePortAddress
        {
            public string NameSpace;
            public string Device;
            public uint IpId;
            public uint Port;
            public string Address;
        }

        #region DeviceExtenderExtensionMethods

        private static PropertyInfo GetPropertyOfExtender(this DeviceExtender extender, string propertyName)
        {
            if (extender == null) throw new ArgumentNullException(nameof(extender));
            var property = extender.GetType().GetProperty(propertyName);
            if (property != null) return property;
            Logger.Warn($"No property on {extender} for name \"{propertyName}\"");
            return null;
        }

        public static string GetSigPropertyName(this DeviceExtender extender, Sig sig)
        {
            if (extender == null) throw new ArgumentNullException(nameof(extender));
            return (from property in extender.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                let s = property.GetValue(extender) as Sig
                where s != null && s == sig
                select property.Name).FirstOrDefault();
        }

        public static void InvokeMethod(this DeviceExtender extender, string methodName)
        {
            if (extender == null) throw new ArgumentNullException(nameof(extender));
            try
            {
                extender.GetType().GetMethod(methodName)?.Invoke(extender, new object[] { });
            }
            catch (Exception e)
            {
                Logger.Warn($"Could not call method name \"{methodName}\" on {extender}, {e.Message}");
            }
        }

        public static string GetStringPropertyValue(this DeviceExtender extender, string propertyName)
        {
            if (extender == null) throw new ArgumentNullException(nameof(extender));
            try
            {
                var property = extender.GetPropertyOfExtender(propertyName);
                if (property.GetValue(extender) is StringOutputSig sig) return sig.StringValue;
                Logger.Warn($"\"{propertyName}\" property on {extender} is {property.GetType().Name}");
                return null;
            }
            catch (Exception e)
            {
                Logger.Warn($"Could not get property with name \"{propertyName}\" on {extender}, {e.Message}");
                return null;
            }
        }

        public static void SetStringPropertyValue(this DeviceExtender extender, string propertyName, string value)
        {
            if (extender == null) throw new ArgumentNullException(nameof(extender));
            try
            {
                var property = extender.GetPropertyOfExtender(propertyName);
                if (property.GetValue(extender) is StringInputSig sig)
                {
                    sig.StringValue = value;
                    return;
                }

                Logger.Warn($"\"{propertyName}\" property on {extender} is {property.GetType().Name}");
            }
            catch (Exception e)
            {
                Logger.Warn($"Could not get property with name \"{propertyName}\" on {extender}, {e.Message}");
            }
        }

        public static bool? GetBoolPropertyValue(this DeviceExtender extender, string propertyName)
        {
            if (extender == null) throw new ArgumentNullException(nameof(extender));
            try
            {
                var property = extender.GetPropertyOfExtender(propertyName);
                if (property.GetValue(extender) is BoolOutputSig sig) return sig.BoolValue;
                Logger.Warn($"\"{propertyName}\" property on {extender} is {property.GetType().Name}");
                return null;
            }
            catch (Exception e)
            {
                Logger.Warn($"Could not get property with name \"{propertyName}\" on {extender}, {e.Message}");
                return null;
            }
        }

        public static void SetBoolPropertyValue(this DeviceExtender extender, string propertyName, bool value)
        {
            if (extender == null) throw new ArgumentNullException(nameof(extender));
            try
            {
                var property = extender.GetPropertyOfExtender(propertyName);
                if (property.GetValue(extender) is BoolInputSig sig)
                {
                    sig.BoolValue = value;
                    return;
                }

                Logger.Warn($"\"{propertyName}\" property on {extender} is {property.GetType().Name}");
            }
            catch (Exception e)
            {
                Logger.Warn($"Could not get property with name \"{propertyName}\" on {extender}, {e.Message}");
            }
        }

        public static ushort? GetUShortPropertyValue(this DeviceExtender extender, string propertyName)
        {
            if (extender == null) throw new ArgumentNullException(nameof(extender));
            try
            {
                var property = extender.GetPropertyOfExtender(propertyName);
                if (property.GetValue(extender) is UShortOutputSig sig) return sig.UShortValue;
                Logger.Warn($"\"{propertyName}\" property on {extender} is {property.GetType().Name}");
                return null;
            }
            catch (Exception e)
            {
                Logger.Warn($"Could not get property with name \"{propertyName}\" on {extender}, {e.Message}");
                return null;
            }
        }

        public static void SetUShortPropertyValue(this DeviceExtender extender, string propertyName, ushort value)
        {
            if (extender == null) throw new ArgumentNullException(nameof(extender));
            try
            {
                var property = extender.GetPropertyOfExtender(propertyName);
                if (property.GetValue(extender) is UShortInputSig sig)
                {
                    sig.UShortValue = value;
                    return;
                }

                Logger.Warn($"\"{propertyName}\" property on {extender} is {property.GetType().Name}");
            }
            catch (Exception e)
            {
                Logger.Warn($"Could not get property with name \"{propertyName}\" on {extender}, {e.Message}");
            }
        }

        #endregion

        private static readonly string[] SizeSuffixes =
            {"bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB"};

        /// <summary>
        /// Get a string showing size from bytes
        /// </summary>
        /// <param name="value">number of bytes</param>
        /// <param name="decimalPlaces">number of decimal places</param>
        public static string PrettyByteSize(Int64 value, int decimalPlaces)
        {
            var i = 0;
            var dValue = (decimal) value;
            while (Math.Round(dValue, decimalPlaces) >= 1000)
            {
                dValue /= 1024;
                i++;
            }

            return string.Format("{0:n" + decimalPlaces + "} {1}", dValue, SizeSuffixes[i]);
        }

        public static string GetDmInputEventIdName(int eventIdValue)
        {
            var fields = typeof(DMInputEventIds).GetFields();
            foreach (var field in fields)
            {
                if (field.FieldType != typeof(int)) continue;
                var v = (int) field.GetValue(null);
                if (v == eventIdValue) return field.Name;
            }

            return "Unknown ID " + eventIdValue;
        }
    }

    public class DefaultDateFormatter : IFormatProvider, ICustomFormatter
    {
        #region Implementation of IFormatProvider

        public object GetFormat(Type formatType)
        {
            return formatType == typeof(ICustomFormatter) ? this : null;
        }

        #endregion

        #region Implementation of ICustomFormatter

        public string Format(string format, object arg, IFormatProvider formatProvider)
        {
            if (!(arg is DateTime)) throw new NotSupportedException();

            var dt = (DateTime) arg;

            string suffix;

            if (new[] {11, 12, 13}.Contains(dt.Day))
            {
                suffix = "th";
            }
            else
                switch (dt.Day % 10)
                {
                    case 1:
                        suffix = "st";
                        break;
                    case 2:
                        suffix = "nd";
                        break;
                    case 3:
                        suffix = "rd";
                        break;
                    default:
                        suffix = "th";
                        break;
                }

            return string.Format("{0:dddd} {1}{2} {0:MMMM}", arg, dt.Day, suffix);
        }

        #endregion
    }
}