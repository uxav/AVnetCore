using System;
using Crestron.SimplSharp.CrestronIO;
using Crestron.SimplSharp.WebScripting;
using Newtonsoft.Json.Linq;

namespace UXAV.AVnetCore.WebScripting.InternalApi
{
    public class AuthenticationApiHandler : ApiRequestHandler
    {
        public AuthenticationApiHandler(WebScriptingServer server, WebScriptingRequest request) : base(server, request)
        {
        }

        public AuthenticationApiHandler(WebScriptingServer server, WebScriptingRequest request, bool suppressLogging)
            : base(server, request, suppressLogging)
        {
        }

        public void Post()
        {
            try
            {
                var reader = new StreamReader(Request.InputStream);
                var json = JToken.Parse(reader.ReadToEnd());
                var method = (json["method"] ?? throw new InvalidOperationException("No method stated"))
                    .Value<string>();
                switch (method)
                {
                    case "check":
                        try
                        {
                            WriteResponse(ValidateSession(true));
                        }
                        catch (Exception e)
                        {
                            HandleError(e);
                        }
                        break;
                    case "login":
                        try
                        {
                            var username =
                                (json["username"] ?? throw new InvalidOperationException("No username stated"))
                                .Value<string>();
                            var password =
                                (json["password"] ?? throw new InvalidOperationException("No password stated"))
                                .Value<string>();
                            var session = AppAuthentication.StartSession(username, password);
                            Response.SetCookie(new HttpCwsCookie("sessionId")
                            {
                                Value = session.SessionId,
                                Expires = session.ExpiryTime,
                                Path = "/",
                                HttpOnly = true,
                                Secure = false,
                            });
                            WriteResponse(session);
                        }
                        catch (UnauthorizedAccessException)
                        {
                            HandleError(401, "Unauthorized", "Incorrect login details");
                        }
                        catch (Exception e)
                        {
                            HandleError(401, "Unauthorized", e.Message);
                        }
                        break;
                    case "logout":
                        var token = Request.Cookies.Get("sessionId").Value;
                        AppAuthentication.InvalidateSession(token);
                        Response.SetCookie(new HttpCwsCookie("sessionId")
                        {
                            Value = string.Empty,
                            Expires = new DateTime(),
                            Path = "/",
                        });
                        WriteResponse(null);
                        break;
                }
            }
            catch (Exception e)
            {
                HandleError(e);
            }
        }
    }
}