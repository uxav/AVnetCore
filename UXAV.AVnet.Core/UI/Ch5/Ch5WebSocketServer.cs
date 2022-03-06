using System;
using Crestron.SimplSharp;
using UXAV.AVnet.Core.Models;
using WebSocketSharp;
using WebSocketSharp.Server;
using Logger = UXAV.Logging.Logger;

namespace UXAV.AVnet.Core.UI.Ch5
{
    public static class Ch5WebSocketServer
    {
        private static WebSocketServer _server;

        static Ch5WebSocketServer()
        {
            CrestronEnvironment.ProgramStatusEventHandler += CrestronEnvironmentOnProgramStatusEventHandler;
        }

        public static string WebSocketBaseUrl =>
            $"ws{(_server.IsSecure ? "s" : "")}://{SystemBase.IpAddress}:{_server.Port}";

        public static void Init(int port)
        {
            _server = new WebSocketServer(port, false)
            {
                KeepClean = true,
                WaitTime = TimeSpan.FromSeconds(30)
            };

            _server.Log.Output += OnLogOutput;
            _server.Log.Level = LogLevel.Trace;
        }

        private static void CrestronEnvironmentOnProgramStatusEventHandler(eProgramStatusEventType eventType)
        {
            if (eventType == eProgramStatusEventType.Stopping && _server.IsListening)
                _server.Stop(1012, "Processor is restarting / stopping");
        }

        internal static void AddDeviceService<THandler>(Ch5UIController<THandler> controller)
            where THandler : Ch5ApiHandlerBase
        {
            var path = $"/ui/{controller.Device.ID:x2}";
            _server.AddWebSocketService(path, () =>
            {
                var ctor = typeof(THandler).GetConstructor(new[] { typeof(Core3ControllerBase) });
                if (ctor == null)
                    throw new InvalidOperationException($"Could not get ctor for type: {typeof(THandler).FullName}");
                var handler = (Ch5ApiHandlerBase)ctor.Invoke(new object[] { controller });
                return new Ch5ConnectionInstance(handler);
            });
            controller.WebSocketUrl = $"{WebSocketBaseUrl}{path}";
            Logger.Highlight($"Websocket URL for UI Controller {controller.Id} set to: {controller.WebSocketUrl}");
        }

        public static void AddWebService<THandler>(string path) where THandler : Ch5ApiHandlerBase
        {
            _server.AddWebSocketService(path, () =>
            {
                var ctor = typeof(THandler).GetConstructor(new Type[] { });
                if (ctor == null)
                    throw new InvalidOperationException($"Could not get ctor for type: {typeof(THandler).FullName}");
                var handler = (Ch5ApiHandlerBase)ctor.Invoke(new object[] { });
                return new Ch5ConnectionInstance(handler);
            });
        }

        public static void Start()
        {
            _server.Start();
        }

        private static void OnLogOutput(LogData data, string s)
        {
            switch (data.Level)
            {
                case LogLevel.Trace:
                case LogLevel.Debug:
                    Logger.Debug("WS Log: " + data.Message);
                    break;
                case LogLevel.Info:
                    Logger.Log("WS Log: " + data.Message);
                    break;
                case LogLevel.Warn:
                    Logger.Warn("WS Log: " + data.Message);
                    break;
                case LogLevel.Error:
                case LogLevel.Fatal:
                    Logger.Error("WS Log: " + data.Message);
                    break;
            }
        }
    }
}