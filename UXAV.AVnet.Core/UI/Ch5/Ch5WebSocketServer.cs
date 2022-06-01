using System;
using System.IO;
using System.Text;
using UXAV.AVnet.Core.Models;
using WebSocketSharp;
using WebSocketSharp.Net;
using WebSocketSharp.Server;
using Logger = UXAV.Logging.Logger;

namespace UXAV.AVnet.Core.UI.Ch5
{
    public static class Ch5WebSocketServer
    {
        private static bool _initCalled;
        private static HttpServer _server;
        private static string _workingDirectory;
        private static string _runtimeGuid;

        public static string WebSocketBaseUrl =>
            $"ws://{SystemBase.IpAddress}:{_server.Port}";

        public static void Init(int port, string workingDirectory = "./ch5")
        {
            if (_initCalled)
            {
                Logger.Warn($"Init already called, will create new server!");
            }

            _initCalled = true;

            _workingDirectory = workingDirectory;
            _server = new HttpServer(port, false)
            {
                KeepClean = true,
                WaitTime = TimeSpan.FromSeconds(30),
                RootPath = "/"
            };
            _server.OnGet += HttpServerOnOnGet;
            try
            {
                /*var cert = new X509Certificate2("/opt/crestron/virtualcontrol/data/ssl/certs/server.pfx", "password");
                Logger.Highlight($"Loaded cert for websocket: {cert}");
                _server.SslConfiguration.ServerCertificate = cert;
                _server.SslConfiguration.EnabledSslProtocols = SslProtocols.Tls12;
                _server.SslConfiguration.ClientCertificateRequired = false;
                _server.AuthenticationSchemes = AuthenticationSchemes.None;
                _server.UserCredentialsFinder = id => id.Name == "wsuser"
                    ? new WebSocketSharp.Net.NetworkCredential(id.Name, "wsuser")
                    : null;*/
            }
            catch (Exception e)
            {
                Logger.Error($"Error loading cert: {e.Message}");
            }
        }

        public static int Port => _server.Port;

        private static void HttpServerOnOnGet(object sender, HttpRequestEventArgs e)
        {
            var req = e.Request;
            var res = e.Response;

            var path = req.Url.LocalPath;
            Logger.Debug($"GET {path}");
            if (path == "/")
                path += "index.html";

            byte[] contents;

            path = _workingDirectory + path;

            if (!TryReadFile(path, out contents))
            {
                res.StatusCode = (int)HttpStatusCode.NotFound;
                return;
            }

            if (path.EndsWith(".html"))
            {
                res.ContentType = "text/html";
                res.ContentEncoding = Encoding.UTF8;
            }
            else if (path.EndsWith(".js"))
            {
                res.ContentType = "application/javascript";
                res.ContentEncoding = Encoding.UTF8;
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

        internal static bool InitCalled => _initCalled;

        public static string RuntimeGuid
        {
            get
            {
                if (string.IsNullOrEmpty(_runtimeGuid))
                {
                    _runtimeGuid = Guid.NewGuid().ToString();
                }

                return _runtimeGuid;
            }
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

        internal static void Start()
        {
            if (_server.IsListening)
            {
                Logger.Warn("Server already started!");
                return;
            }
            try
            {
                //_server.Log.Output += OnLogOutput;
                //_server.Log.Level = LogLevel.Trace;
                _server?.Start();
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        internal static bool Running => _server != null && _server.IsListening;

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