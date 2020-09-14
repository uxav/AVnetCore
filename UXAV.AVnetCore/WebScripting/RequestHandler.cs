using System;
using System.Reflection;
using System.Threading;
using Crestron.SimplSharp;
using Crestron.SimplSharp.WebScripting;
using UXAV.Logging;

namespace UXAV.AVnetCore.WebScripting
{
    public abstract class RequestHandler
    {
        protected RequestHandler(WebScriptingServer server, WebScriptingRequest request)
        {
            Server = server;
            Request = request;
            if (!SuppressLogging)
            {
                Logger.Debug(ToString());
            }
        }
        protected RequestHandler(WebScriptingServer server, WebScriptingRequest request, bool suppressLogging)
        {
            Server = server;
            Request = request;
            SuppressLogging = suppressLogging;
            if (!SuppressLogging)
            {
                Logger.Debug(ToString());
            }
        }

        public WebScriptingServer Server { get; }

        public Models.SystemBase System => Server.System;

        public WebScriptingRequest Request { get; }

        public HttpCwsResponse Response => Request.Response;

        public bool SuppressLogging { get; }

        protected Session ValidateSession(bool renew)
        {
            var cookie = Request.Cookies["sessionId"];
            if (cookie == null)
            {
                return null;
            }
            var session = AppAuthentication.ValidateSession(cookie.Value, renew);
            if (session == null || session.ExpiryTime < DateTime.Now)
            {
                Response.SetCookie(new HttpCwsCookie("sessionId")
                {
                    Value = string.Empty,
                    Expires = new DateTime().ToUniversalTime(),
                    Path = "/",
                });
                return null;
            }

            Response.SetCookie(new HttpCwsCookie("sessionId")
            {
                Value = cookie.Value,
                Expires = session.ExpiryTime.ToUniversalTime(),
                Path = "/",
                HttpOnly = true,
                Secure = false,
            });

            return session;
        }

        public void Process()
        {
            try
            {
                Request.Response.Headers.Add("X-App-RequestHandler", GetType().FullName);

                var method = GetType().GetMethod(Request.Method,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase | BindingFlags.Instance,
                    null,
                    new Type[] { }, null);

                if (method == null)
                {
                    HandleError(405, "Method not allowed",
                        $"{GetType().Name} does not allow method \"{Request.Method}\"");
                    return;
                }

                var secure = method.GetCustomAttribute<SecureRequestAttribute>();
                if (secure != null)
                {
                    if (!SuppressLogging)
                    {
                        Logger.Debug("Method is secure... validating");
                    }
                    var session = ValidateSession(true);
                    switch (session)
                    {
                        case null when secure.RedirectToLogin:
                            Redirect("/cws/a/login?after={0}", Request.PathAndQueryString);
                            return;
                        case null:
                            HandleError(401, "Unauthorized", "No session valid. Please login.");
                            return;
                    }
                    if (!SuppressLogging)
                    {
                        Logger.Debug("Session ok!");
                    }
                }

                try
                {
                    method.Invoke(this, new object[] { });
                }
                catch (ThreadAbortException)
                {
                }
                catch (Exception e)
                {
                    ErrorLog.Exception("Error handling request", e);
                    HandleError(e);
                }
            }
            catch (Exception e)
            {
                HandleError(e);
            }
        }

        protected void Redirect(string url, params object[] args)
        {
            Request.Response.Redirect(string.Format(url, args));
        }

        protected virtual void HandleError(Exception e)
        {
            Server.HandleError(Request, e);
        }

        protected virtual void HandleNotFound()
        {
            Server.HandleError(Request, 404, "Not Found", "The request handler could not process this request");
        }

        protected virtual void HandleNotFound(string message)
        {
            Server.HandleError(Request, 404, "Not Found", message);
        }

        protected void HandleError(int code, string title, string message)
        {
            Server.HandleError(Request, code, title, message);
        }

        public sealed override string ToString()
        {
            return $"{Request.Method} {Request.PathAndQueryString} ({GetType().Name})";
        }
    }
}