using System;
using System.IO;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using System.Web;
using UXAV.AVnet.Core.Models;
using WebSocketSharp;
using WebSocketSharp.Net;
using WebSocketSharp.Server;
using Logger = UXAV.Logging.Logger;

namespace UXAV.AVnet.Core.UI.Ch5
{
    public static class Ch5WebSocketServer
    {
        private static HttpServer _server;
        private static string _workingDirectory;

        public static string WebSocketBaseUrl =>
            $"ws{(Secure ? "s" : "")}://{SystemBase.IpAddress}:{_server.Port}";

        public static int Port => _server?.Port ?? 0;

        public static bool Secure => _server.IsSecure;

        internal static bool InitCalled => _server != null;

        internal static bool Running => _server != null && _server.IsListening;

        public static void Init(int port, string workingDirectory = "./ch5", X509Certificate2 cert = null)
        {
            Logger.AddCommand((argString, args, connection, respond) => { DebugIsOn = true; }, "WebSocketServerDebug",
                "Set the debugging on the websocket server to on");

            if (_server != null)
            {
                Logger.Warn("Init already called, will create new server!");
                _server.Stop();
            }

            _workingDirectory = workingDirectory;
            _server = new HttpServer(port, cert != null)
            {
                KeepClean = true,
                WaitTime = TimeSpan.FromSeconds(30),
                DocumentRootPath = "./"
            };
            _server.OnGet += HttpServerOnOnGet;
            try
            {
                if (cert != null)
                {
                    Logger.Highlight($"Loaded cert for websocket: {cert.FriendlyName}");
                    _server.SslConfiguration.ServerCertificate = cert;
                    _server.SslConfiguration.EnabledSslProtocols = SslProtocols.Tls12;
                }
            }
            catch (Exception e)
            {
                Logger.Error($"Error loading cert: {e.Message}");
            }
        }

        private static void HttpServerOnOnGet(object sender, HttpRequestEventArgs e)
        {
            var req = e.Request;
            var res = e.Response;

            var path = req.Url.LocalPath;
            if (DebugIsOn)
                Logger.Debug($"GET {path}");
            if (path == "/")
                path += "index.html";

            res.AddHeader("Access-Control-Allow-Origin", "*");

            if (path.StartsWith("/user/") && OnUserGet != null)
            {
                if (DebugIsOn)
                    Logger.Debug("User request: " + path);
                OnUserGet?.Invoke(sender, e);
                return;
            }

            byte[] contents;

            path = _workingDirectory + path;

            if (!File.Exists(path) && File.Exists(_workingDirectory + "/index.html"))
            {
                if (DebugIsOn) Logger.Debug("File not found, using index.html");
                path = _workingDirectory + "/index.html";
            }

            if (!TryReadFile(path, out contents))
            {
                if (DebugIsOn)
                    Logger.Debug($"File not found: {path}");
                res.StatusCode = (int)HttpStatusCode.NotFound;
                res.Close();
                return;
            }

            var match = Regex.Match(path, @"^.*\.(\w+)$");
            if (match.Success && match.Groups[1].Success)
            {
                var extension = match.Groups[1].Value;
                res.ContentType = MimeMapping.GetMimeMapping(path);
                if (DebugIsOn)
                    Logger.Debug($"Setting content type to {res.ContentType}");
            }

            res.ContentLength64 = contents.LongLength;

            res.Close(contents, true);
        }

        private static bool TryReadFile(string path, out byte[] contents)
        {
            contents = null;

            if (!File.Exists(path))
                return false;

            try
            {
                contents = File.ReadAllBytes(path);
            }
            catch
            {
                return false;
            }

            return true;
        }

        internal static void Stop()
        {
            try
            {
                if (_server.IsListening)
                {
                    //_server.Log.Output -= OnLogOutput;
                    Logger.Warn("Shutting down websocket server for UI");
                    _server?.Stop();
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        internal static void AddDeviceService<THandler>(Ch5UIController<THandler> controller)
            where THandler : Ch5ApiHandlerBase
        {
            Logger.Debug($"Adding device service for {controller}");
            var path = $"/ui/{controller.Device.ID:x2}";
            Logger.Debug("Path = " + path);

#pragma warning disable CS0618
            _server.AddWebSocketService(path, () =>
            {
                var ctor = typeof(THandler).GetConstructor(new[] { typeof(Core3ControllerBase) });
                if (ctor == null)
                    throw new InvalidOperationException($"Could not get ctor for type: {typeof(THandler).FullName}");
                var handler = (Ch5ApiHandlerBase)ctor.Invoke(new object[] { controller });
                return new Ch5ConnectionInstance(handler, controller);
            });
#pragma warning restore CS0618
            controller.WebSocketUrl = $"{WebSocketBaseUrl}{path}";
            Logger.Highlight($"Websocket URL for UI Controller {controller.Id} set to: {controller.WebSocketUrl}");
        }

        public static void AddWebService<THandler>(string path = "/ui/web") where THandler : Ch5ApiHandlerBase
        {
#pragma warning disable CS0618
            _server.AddWebSocketService(path, () =>
            {
                var ctor = typeof(THandler).GetConstructor(new Type[] { });
                if (ctor == null)
                    throw new InvalidOperationException($"Could not get ctor for type: {typeof(THandler).FullName}");
                var handler = (Ch5ApiHandlerBase)ctor.Invoke(new object[] { });
                return new Ch5ConnectionInstance(handler);
            });
#pragma warning restore CS0618
        }

        internal static void Start()
        {
            if (_server.IsListening)
            {
                Logger.Warn("Server already started!");
                return;
            }

            try
            {
                _server.Log.Output += OnLogOutput;
                _server.Log.Level = LogLevel.Trace;
                _server?.Start();
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        private static void OnLogOutput(LogData data, string s)
        {
            if(!DebugIsOn) return;
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

        internal static bool DebugIsOn { get; private set; }

        public static event EventHandler<HttpRequestEventArgs> OnUserGet;
    }
}