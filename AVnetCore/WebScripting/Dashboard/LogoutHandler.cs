using System;
using Crestron.SimplSharp.WebScripting;

namespace UXAV.AVnetCore.WebScripting.Dashboard
{
    public class LogoutHandler : RequestHandler
    {
        public LogoutHandler(WebScriptingServer server, WebScriptingRequest request)
            : base(server, request)
        {
        }

        public void Get()
        {
            AppAuthentication.InvalidateSession(Request.Cookies["sessionid"].Value);
            Response.SetCookie(new HttpCwsCookie("sessionid", "deleted")
            {
                Path = "/cws/",
                Expires = new DateTime(),
                HttpOnly = true,
                Secure = true
            });
            Response.Write("OK", true);
        }
    }
}