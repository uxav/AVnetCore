using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Crestron.SimplSharp.CrestronIO;
using Crestron.SimplSharpPro;
using UXAV.AVnetCore.Models;
using UXAV.Logging;

namespace UXAV.AVnetCore
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

            return (SystemBase) ctor.Invoke(new object[] {controlSystem});
        }

        public static string DevicePortAddressCreate(string device, uint ipId, uint port)
        {
            return $"{device}:{ipId:X2}:{port:D2}";
        }

        public static DevicePortAddress DevicePortAddressParse(string addressString)
        {
            var match = Regex.Match(addressString, @"^(?:([\w\.]+)\.)?([\w\.]+):(\w{2}):(\d{1,2})$");
            if (!match.Success) throw new ArgumentException("Incorrect format for address", nameof(addressString));
            return new DevicePortAddress
            {
                NameSpace = match.Groups[1].Value,
                Device = match.Groups[2].Value,
                IpId = uint.Parse(match.Groups[3].Value, NumberStyles.HexNumber),
                Port = uint.Parse(match.Groups[4].Value)
            };
        }

        public struct DevicePortAddress
        {
            public string NameSpace;
            public string Device;
            public uint IpId;
            public uint Port;
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
    }
}