using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Crestron.SimplSharp;
using Crestron.SimplSharp.CrestronAuthentication;
using Microsoft.AspNetCore.Http;
using UXAV.AVnet.Core.Models;
using UXAV.Logging;

namespace UXAV.AVnet.Core.WebScripting
{
    public abstract class RequestHandler
    {
        protected RequestHandler(WebScriptingServer server, WebScriptingRequest request)
        {
            Server = server;
            Request = request;
            try
            {
                if (!SuppressLogging) Logger.Debug(ToString());
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        protected RequestHandler(WebScriptingServer server, WebScriptingRequest request, bool suppressLogging)
        {
            Server = server;
            Request = request;
            SuppressLogging = suppressLogging;
            try
            {
                if (!SuppressLogging) Logger.Debug(ToString());
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        public WebScriptingServer Server { get; }

        public SystemBase System => Server.System;

        public WebScriptingRequest Request { get; }

        public HttpResponse Response => Request.Response;

        public bool SuppressLogging { get; }

        protected Session ValidateSession(bool renew)
        {
            var cookie = Request.Cookies["sessionId"];
            if (cookie == null) return null;
            var session = AppAuthentication.ValidateSession(cookie, renew);
            if (session == null || session.ExpiryTime < DateTime.Now)
            {
                Response.Cookies.Append("sessionId", string.Empty, new CookieOptions
                {
                    Expires = new DateTime().ToUniversalTime(),
                    Path = "/"
                });
                return null;
            }

            Response.Cookies.Append("sessionId", cookie, new CookieOptions
            {
                Expires = session.ExpiryTime.ToUniversalTime(),
                Path = "/",
                HttpOnly = true,
                Secure = false
            });
            return session;
        }

        public async Task ProcessAsync()
        {
            try
            {
                Request.Response.Headers.Append("X-App-RequestHandler", GetType().FullName);

                var method = GetType().GetMethod(Request.Method,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase | BindingFlags.Instance, null, [], null);

                if (method == null)
                {
                    method = GetType().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                        .FirstOrDefault(method => method.GetCustomAttribute<RequestHandlerMethodAttribute>() != null
                            && method.GetCustomAttribute<RequestHandlerMethodAttribute>().MethodType.ToString().ToUpper() == Request.Method);
                }

                if (method == null)
                {
                    await HandleErrorAsync(405, $"{GetType().Name} does not allow method \"{Request.Method}\"");
                    return;
                }

                var secure = method.GetCustomAttribute<SecureRequestAttribute>();
                if (secure != null && Authentication.Enabled)
                {
                    if (!SuppressLogging) Logger.Debug("Method is secure... validating");
                    var session = ValidateSession(true);
                    switch (session)
                    {
                        case null when secure.RedirectToLogin:
                            Redirect("/cws/a/login?after={0}", Request.PathAndQueryString);
                            return;
                        case null:
                            await HandleErrorAsync(401, "No session valid. Please login.");
                            return;
                    }

                    if (!SuppressLogging) Logger.Debug("Session ok!");
                }

                try
                {

                    if (method.ReturnType.GetMethod(nameof(Task.GetAwaiter)) != null)
                    {
                        //Logger.Debug("Invoking async method: {0}", method.Name);
                        await (Task)method.Invoke(this, []);
                    }
                    else
                    {
                        //Logger.Debug("Invoking sync method: {0}", method.Name);
                        method.Invoke(this, []);
                    }
                }
                catch (TargetInvocationException e)
                {
                    await HandleErrorAsync(e.InnerException);
                }
                catch (ThreadAbortException)
                {
                }
                catch (Exception e)
                {
                    ErrorLog.Exception("Error handling request", e);
                    await HandleErrorAsync(e);
                }
            }
            catch (Exception e)
            {
                await HandleErrorAsync(e);
            }
        }

        protected void Redirect(string url, params object[] args)
        {
            Request.Response.Redirect(string.Format(url, args));
        }

        protected virtual async Task HandleErrorAsync(Exception e)
        {
            await Server.HandleErrorAsync(Request, e);
        }

        protected virtual async Task HandleNotFoundAsync()
        {
            await Server.HandleErrorAsync(Request, 404, "Not Found");
        }

        protected virtual async Task HandleNotFoundAsync(string message)
        {
            await Server.HandleErrorAsync(Request, 404, message);
        }

        protected async Task HandleErrorAsync(int code, string message)
        {
            await Server.HandleErrorAsync(Request, code, message);
        }

        public sealed override string ToString()
        {
            try
            {
                return $"{Request.Method} {Request.PathAndQueryString} ({GetType().Name})";
            }
            catch
            {
                return $"{Request.Method} {Request.Path} ({GetType().Name})";
            }
        }
    }
}