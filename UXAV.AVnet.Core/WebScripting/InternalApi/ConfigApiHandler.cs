using System;
using System.Collections.Generic;
using Crestron.SimplSharp.CrestronIO;
using Newtonsoft.Json.Linq;
using UXAV.AVnet.Core.Config;
using UXAV.Logging;

namespace UXAV.AVnet.Core.WebScripting.InternalApi
{
    internal class ConfigApiHandler : ApiRequestHandler
    {
        public ConfigApiHandler(WebScriptingServer server, WebScriptingRequest request)
            : base(server, request)
        {
        }

        // ReSharper disable once UnusedMember.Global
        [SecureRequest]
        public void Get()
        {
            try
            {
                if (Request.RoutePatternArgs.ContainsKey("function"))
                    switch (Request.RoutePatternArgs["function"])
                    {
                        case "plist":
                            if (!Request.RoutePatternArgs.ContainsKey("key"))
                            {
                                WriteResponse(ConfigManager.PropertyList);
                                return;
                            }

                            var key = Request.RoutePatternArgs["key"];
                            if (!ConfigManager.PropertyListContainsKey(key))
                                throw new KeyNotFoundException(
                                    $"PropertyList does not contain key with name \"{key}\"");

                            WriteResponse(ConfigManager.GetPropertyListItemWithKey(key));
                            return;
                        default:
                            HandleNotFound();
                            return;
                    }

                var restartRequired = Server.System.ConfigCheckIfRestartIsRequired(ConfigManager.JConfig.ToString());
                var files = ConfigManager.GetFileDetails();

                WriteResponse(new
                {
                    ConfigManager.ConfigPath,
                    LastRevisionTime = ConfigManager.LastRevisionTime.ToUniversalTime(),
                    RestartRequired = restartRequired,
                    AvailableFiles = files,
                    IsDefault = ConfigManager.ConfigIsDefaultFile,
                    Config = ConfigManager.JConfig,
                    ConfigManager.Schema
                });
            }
            catch (Exception e)
            {
                HandleError(e);
            }
        }

        public void Options()
        {
            Response.Headers.Add("Access-Control-Allow-Methods", "POST, GET, OPTIONS");
            Response.StatusCode = 204;
            Response.StatusDescription = "No Content";
            Response.Flush();
        }

        [SecureRequest]
        public void Delete()
        {
            if (Request.RoutePatternArgs.ContainsKey("function"))
            {
                switch (Request.RoutePatternArgs["function"])
                {
                    case "current":
                        ConfigManager.DeleteCurrentFile();
                        WriteResponse(ConfigManager.ConfigPath);
                        return;
                }

                HandleNotFound();
            }
        }

        // ReSharper disable once UnusedMember.Global
        [SecureRequest]
        public void Post()
        {
            try
            {
                StreamReader reader;

                if (Request.RoutePatternArgs.ContainsKey("function"))
                    switch (Request.RoutePatternArgs["function"])
                    {
                        case "plist":
                            reader = new StreamReader(Request.InputStream);
                            var list = JObject.Parse(reader.ReadToEnd()).ToObject<Dictionary<string, object>>();
                            foreach (var item in list) ConfigManager.SetPropertyListItemWithKey(item.Key, item.Value);

                            WriteResponse(new
                            {
                                UpdatedValues = list
                            });
                            return;
                        case "new":
                            reader = new StreamReader(Request.InputStream);
                            ConfigManager.CreateNewFileWithName(reader.ReadToEnd());
                            WriteResponse(ConfigManager.ConfigPath);
                            return;
                        case "filepath":
                            reader = new StreamReader(Request.InputStream);
                            ConfigManager.SetConfigPath(reader.ReadToEnd());
                            WriteResponse(ConfigManager.ConfigPath);
                            return;
                        default:
                            HandleNotFound();
                            return;
                    }

                try
                {
                    reader = new StreamReader(Request.InputStream);
                    var json = JToken.Parse(reader.ReadToEnd());
                    Logger.Debug("Json received\r\n{0}", json.ToString());
                    ConfigManager.JConfig = json;
                    WriteResponse("OK");
                }
                catch (Exception e)
                {
                    Logger.Error("Problem parsing content, {0}", e.Message);
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
                HandleError(e);
            }
        }
    }
}