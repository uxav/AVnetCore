using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace UXAV.AVnetCore.Logging
{
    public class LoggerMessage
    {
        private readonly Exception _exception;
        private readonly StackTrace _stackTrace;
        private string _toString;
        private readonly Type _tracedType;

        internal LoggerMessage(Logger.LoggerLevel level, StackTrace stackTrace, Logger.MessageType messageType,
            string message)
        {
            _stackTrace = stackTrace;
            Level = level;
            Time = DateTime.Now;
            MessageType = messageType;
            Message = message;
            _tracedType = stackTrace.GetFrame(0).GetMethod().DeclaringType;
        }

        internal LoggerMessage(StackTrace stackTrace, Exception e)
        {
            _stackTrace = stackTrace;
            _exception = e;
            Level = Logger.LoggerLevel.Error;
            Time = DateTime.Now;
            MessageType = Logger.MessageType.Exception;
            Message = e.ToString();
            _tracedType = stackTrace.GetFrame(0).GetMethod().DeclaringType;
            /*var linePadding = Ansi.BackgroundRed + " " + "\u001b[48;5;$236m";
            linePadding = linePadding + " " + GetPaddedLineName(string.Empty) + " ";

            linePadding = linePadding + "\u001b[48;5;$235m\u001b[38;5;$246m";
            for (var i = 0; i < 14; i++)
            {
                linePadding = linePadding + " ";
            }

            linePadding = linePadding + Ansi.Reset + "     ";

            _contents = Ansi.BackgroundRed + "  " + GetPaddedLineName("Exception") + " " +
                        "\u001b[48;5;$124m\u001b[38;5;$246m" + " " + Time.ToString("HH:mm:ss.fff") + " " + Ansi.Reset +
                        " 🔥  " + Ansi.BrightRed + e.GetType().Name + ": " + Ansi.Reset + Ansi.Red + e.Message +
                        Ansi.Reset;
            foreach (var line in e.StackTrace.Split(new []{Environment.NewLine}, StringSplitOptions.RemoveEmptyEntries))
            {
                _contents = _contents + Environment.NewLine + linePadding + Ansi.White + line.TrimStart();
            }
            _contents = _contents + Ansi.Reset;*/
        }

        public DateTime Time { get; }

        [JsonConverter(typeof(StringEnumConverter))]
        public Logger.LoggerLevel Level { get; }

        [JsonConverter(typeof(StringEnumConverter))]
        public Logger.MessageType MessageType { get; }

        public string ColorClass
        {
            get
            {
                switch (MessageType)
                {
                    case Logger.MessageType.Normal:
                        return "basic";
                    case Logger.MessageType.Debug:
                        return "info";
                    case Logger.MessageType.Highlight:
                        return "control";
                    case Logger.MessageType.Success:
                        return "success";
                    case Logger.MessageType.Warning:
                        return "warning";
                    case Logger.MessageType.Error:
                    case Logger.MessageType.Exception:
                        return "danger";
                    default:
                        return string.Empty;
                }
            }
        }

        public string TracedName => _tracedType == null ? string.Empty : _tracedType.Name;

        public string TracedNameFull => _tracedType == null ? string.Empty : _tracedType.FullName;

        public string FileName =>
            _stackTrace.GetFrame(0) == null ? string.Empty : _stackTrace.GetFrame(0).GetFileName();

        public int FileLineNumber => _stackTrace?.GetFrame(0)?.GetFileLineNumber() ?? 0;
        public string Message { get; }

        public string StackTrace => _stackTrace.ToString();

        public string GetFormattedForConsole()
        {
            using (var writer = new StringWriter())
            {
                switch (MessageType)
                {
                    case Logger.MessageType.Debug:
                        writer.Write(Ansi.BackgroundMagenta + " ");
                        break;
                    case Logger.MessageType.Success:
                        writer.Write(Ansi.BackgroundGreen + " ");
                        break;
                    case Logger.MessageType.Warning:
                        writer.Write(Ansi.BackgroundYellow + " ");
                        break;
                    case Logger.MessageType.Exception:
                    case Logger.MessageType.Error:
                        writer.Write(Ansi.BackgroundRed + " ");
                        break;
                    default:
                        writer.Write(Ansi.BackgroundWhite + " ");
                        break;
                }

                writer.Write(Ansi.Reset + " " + Ansi.White);
                writer.Write(Time.ToString("dd-MMM HH:mm:ss.ffff"));
                writer.Write(" " + Ansi.Blue + " " + GetPaddedLineName(TracedName) + " " + Ansi.Reset);

                switch (MessageType)
                {
                    case Logger.MessageType.Highlight:
                        writer.Write(Ansi.Bold);
                        break;
                    case Logger.MessageType.Success:
                        writer.Write(Ansi.BrightGreen);
                        break;
                    case Logger.MessageType.Warning:
                        writer.Write(Ansi.BrightYellow);
                        break;
                    case Logger.MessageType.Exception:
                        writer.Write(Ansi.Red);
                        break;
                    case Logger.MessageType.Error:
                        writer.Write(Ansi.BrightRed + "Error: " + Ansi.Reset +
                                     _stackTrace.GetFrame(1).GetMethod().Name +
                                     "() " + Ansi.Red);
                        break;
                    default:
                        writer.Write(Ansi.White);
                        break;
                }

                writer.Write(" " + Message + Ansi.Reset);
                return writer.ToString();
            }
        }

        public string GetFormattedForText()
        {
            if (_toString != null) return _toString;
            using (var writer = new StringWriter())
            {
                writer.Write(Time.ToString("O"));
                writer.Write(" " + GetPaddedLineName(TracedName) + " ");

                switch (MessageType)
                {
                    case Logger.MessageType.Debug:
                        writer.Write("  Debug: ");
                        break;
                    case Logger.MessageType.Highlight:
                        writer.Write(" Notice: ");
                        break;
                    case Logger.MessageType.Success:
                        writer.Write("     OK: ");
                        break;
                    case Logger.MessageType.Warning:
                        writer.Write("Warning: ");
                        break;
                    case Logger.MessageType.Error:
                        writer.Write("  Error: " + _stackTrace.GetFrame(0).GetMethod().Name + "() ");
                        break;
                    case Logger.MessageType.Normal:
                        writer.Write("   Info: ");
                        break;
                    case Logger.MessageType.Exception:
                        writer.Write("  Error: ");
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                writer.Write(Message);
                _toString = writer.ToString();
            }

            return _toString;
        }

        private static string GetPaddedLineName(string contents)
        {
            const int size = 20;
            var result = contents ?? string.Empty;
            if (result.Length > size)
            {
                result = result.Substring(0, size - 2);
                result = result + "..";
            }
            else
            {
                for (var i = result.Length; i < size; i++)
                {
                    result = result + " ";
                }
            }

            return result;
        }

        public override string ToString()
        {
            return _exception != null
                ? $"Exception | {_exception.Message}{Environment.NewLine}{_exception.StackTrace}"
                : $"{Level} | {(string.IsNullOrEmpty(TracedName) ? string.Empty : TracedName + " | ")}{Message}";
        }
    }
}