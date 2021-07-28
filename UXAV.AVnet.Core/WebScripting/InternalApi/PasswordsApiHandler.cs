using System;
using Crestron.SimplSharp.CrestronIO;
using Newtonsoft.Json.Linq;
using UXAV.AVnet.Core.Config;
using UXAV.Logging;

namespace UXAV.AVnet.Core.WebScripting.InternalApi
{
    public class PasswordsApiHandler : ApiRequestHandler
    {
        public PasswordsApiHandler(WebScriptingServer server, WebScriptingRequest request)
            : base(server, request)
        {
        }

        public PasswordsApiHandler(WebScriptingServer server, WebScriptingRequest request, bool suppressLogging)
            : base(server, request, suppressLogging)
        {
        }

        [SecureRequest]
        public void Get()
        {
            try
            {
                var passwords = ConfigManager.PasswordsGetAll();
                WriteResponse(passwords);
            }
            catch (Exception e)
            {
                Logger.Error(e);
                HandleError(e);
            }
        }

        [SecureRequest]
        public void Post()
        {
            try
            {
                var reader = new StreamReader(Request.InputStream);
                var json = JToken.Parse(reader.ReadToEnd());
                //Logger.Debug("Json received\r\n{0}", json.ToString());
                if (json["method"] == null)
                {
                    throw new ArgumentException("No method stated in payload");
                }

                var method = json["method"].Value<string>();
                switch (method)
                {
                    case "SetPassword":
                        var passwordKey =
                            (json["passwordKey"] ?? throw new InvalidOperationException("No passwordKey included"))
                            .Value<string>();
                        var passwordValue =
                            (json["passwordValue"] ?? throw new InvalidOperationException("No passwordValue included"))
                            .Value<string>();
                        ConfigManager.PasswordSet(passwordKey, passwordValue);
                        break;
                    default:
                        throw new ArgumentException($"\"{method}\" not known");
                }

                WriteResponse("OK");
            }
            catch (Exception e)
            {
                Logger.Error(e);
                HandleError(e);
            }
        }
    }
}