using System;
using System.IO;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;

namespace UXAV.AVnet.Core.WebScripting.InternalApi
{
    internal class AuthenticationApiHandler : ApiRequestHandler
    {
        public AuthenticationApiHandler(WebScriptingServer server, WebScriptingRequest request) : base(server, request)
        {
        }

        public AuthenticationApiHandler(WebScriptingServer server, WebScriptingRequest request, bool suppressLogging)
            : base(server, request, suppressLogging)
        {
        }

        public async void Post()
        {
            try
            {
                var json = JToken.Parse(await Request.GetStringContentsAsync());
                var method = (json["method"] ?? throw new InvalidOperationException("No method stated"))
                    .Value<string>();
                switch (method)
                {
                    case "check":
                        try
                        {
                            await WriteResponseAsync(ValidateSession(true));
                        }
                        catch (Exception e)
                        {
                            await HandleErrorAsync(e);
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
                            var rememberme =
                                (json["rememberme"] ?? false)
                                .Value<bool>();
                            var session = AppAuthentication.StartSession(username, password, rememberme);
                            Response.Cookies.Append("sessionId", session.SessionId, new CookieOptions
                            {
                                Expires = session.ExpiryTime.ToUniversalTime(),
                                Path = "/",
                                HttpOnly = true,
                                Secure = false
                            });
                            await WriteResponseAsync(session);
                        }
                        catch (UnauthorizedAccessException)
                        {
                            await HandleErrorAsync(401, "Incorrect login details");
                        }
                        catch (Exception e)
                        {
                            await HandleErrorAsync(401, e.Message);
                        }

                        break;
                    case "logout":
                        Request.Cookies.TryGetValue("sessionId", out var token);
                        AppAuthentication.InvalidateSession(token);
                        Response.Cookies.Append("sessionId", string.Empty, new CookieOptions
                        {
                            Expires = new DateTime(),
                            Path = "/"
                        });
                        await WriteResponseAsync(null);
                        break;
                }
            }
            catch (Exception e)
            {
                await HandleErrorAsync(e);
            }
        }
    }
}