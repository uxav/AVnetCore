using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using Crestron.SimplSharp.WebScripting;

namespace UXAV.AVnetCore.WebScripting.Dashboard
{
    public class LoginHander : RequestHandler
    {
        public LoginHander(WebScriptingServer server, WebScriptingRequest request)
            : base(server, request)
        {
        }

        public void Get()
        {
            Response.Write("Please login", true);
        }

        public async void Post()
        {
            try
            {
                var content = new StreamContent(Request.InputStream.GetNormalStream());
                content.Headers.ContentType = MediaTypeHeaderValue.Parse(Request.ContentType);
                var data = await content.ReadAsMultipartAsync();
                var formData = new Dictionary<string, string>();
                foreach (var httpContent in data.Contents)
                {
                    var name = httpContent.Headers.ContentDisposition.Name.Trim('\"');
                    var value = await httpContent.ReadAsStringAsync();
                    formData[name] = value;
                }

                var session = AppAuthentication.StartSession(formData["user"], formData["passwd"]);
                var cookie = new HttpCwsCookie("sessionid")
                {
                    Value = session.SessionId,
                    Expires = session.ExpiryTime,
                    Path = "/",
                    //Path = "/cws/",
                    HttpOnly = true,
                    Secure = true,
                };
                Response.SetCookie(cookie);
                Response.Write(session.SessionId, true);
            }
            catch (UnauthorizedAccessException)
            {
                HandleError(401, "Unauthorized", "Incorrect login details");
            }
            catch (Exception e)
            {
                HandleError(e);
            }
        }
    }
}