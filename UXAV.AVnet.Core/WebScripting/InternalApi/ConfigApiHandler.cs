using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.Http;
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
        public async void Get()
        {
            try
            {
                if (Request.RoutePatternArgs.ContainsKey("function"))
                    switch (Request.RoutePatternArgs["function"])
                    {
                        case "plist":
                            if (!Request.RoutePatternArgs.ContainsKey("key"))
                            {
                                await WriteResponseAsync(ConfigManager.PropertyList);
                                return;
                            }

                            var key = Request.RoutePatternArgs["key"];
                            if (!ConfigManager.PropertyListContainsKey(key))
                                throw new KeyNotFoundException(
                                    $"PropertyList does not contain key with name \"{key}\"");

                            await WriteResponseAsync(ConfigManager.GetPropertyListItemWithKey(key));
                            return;
                        default:
                            await HandleNotFoundAsync();
                            return;
                    }

                var restartRequired = Server.System.ConfigCheckIfRestartIsRequired(ConfigManager.JConfig.ToString());
                var files = ConfigManager.GetFileDetails();

                await WriteResponseAsync(new
                {
                    ConfigManager.ConfigPath,
                    LastRevisionTime = ConfigManager.LastRevisionTime.ToUniversalTime(),
                    RestartRequired = restartRequired,
                    AvailableFiles = files,
                    IsDefault = ConfigManager.ConfigIsDefaultFile,
                    Config = ConfigManager.JConfig.ToObject<object>(),
                    ConfigManager.Schema
                });
            }
            catch (Exception e)
            {
                await HandleErrorAsync(e);
            }
        }

        public void Options()
        {
            Response.Headers.Append("Access-Control-Allow-Methods", "POST, GET, OPTIONS");
            Response.StatusCode = 204;
        }

        [SecureRequest]
        public async void Delete()
        {
            if (Request.RoutePatternArgs.ContainsKey("function"))
            {
                switch (Request.RoutePatternArgs["function"])
                {
                    case "current":
                        ConfigManager.DeleteCurrentFile();
                        await WriteResponseAsync(ConfigManager.ConfigPath);
                        return;
                }

                await HandleNotFoundAsync();
            }
        }

        // ReSharper disable once UnusedMember.Global
        [SecureRequest]
        public async void Post()
        {
            try
            {
                StreamReader reader;

                if (Request.RoutePatternArgs.ContainsKey("function"))
                    switch (Request.RoutePatternArgs["function"])
                    {
                        case "plist":
                            reader = new StreamReader(Request.InputStream);
                            var list = JObject.Parse(await reader.ReadToEndAsync()).ToObject<Dictionary<string, object>>();
                            foreach (var item in list) ConfigManager.SetPropertyListItemWithKey(item.Key, item.Value);

                            await WriteResponseAsync(new
                            {
                                UpdatedValues = list
                            });
                            return;
                        case "new":
                            reader = new StreamReader(Request.InputStream);
                            ConfigManager.CreateNewFileWithName(await reader.ReadToEndAsync());
                            await WriteResponseAsync(ConfigManager.ConfigPath);
                            return;
                        case "filepath":
                            reader = new StreamReader(Request.InputStream);
                            ConfigManager.SetConfigPath(await reader.ReadToEndAsync());
                            await WriteResponseAsync(ConfigManager.ConfigPath);
                            return;
                        default:
                            await HandleNotFoundAsync();
                            return;
                    }

                try
                {
                    reader = new StreamReader(Request.InputStream);
                    var json = JToken.Parse(await reader.ReadToEndAsync());
                    Logger.Debug("Json received\r\n{0}", json.ToString());
                    ConfigManager.JConfig = json;
                    await WriteResponseAsync("OK");
                }
                catch (Exception e)
                {
                    Logger.Error("Problem parsing content, {0}", e.Message);
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
                await HandleErrorAsync(e);
            }
        }
    }
}