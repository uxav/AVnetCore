using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Crestron.SimplSharp;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using UXAV.AVnet.Core.Models;
using UXAV.AVnet.Core.Web;
using Logger = UXAV.Logging.Logger;

namespace UXAV.AVnet.Core.WebScripting
{
    public class WebScriptingServer
    {
        private readonly string _directory;
        private readonly Dictionary<string, Type> _handlers = new Dictionary<string, Type>();
        private readonly Dictionary<string, List<string>> _keyNames = new Dictionary<string, List<string>>();
        private readonly Dictionary<string, string> _originalPatterns = new Dictionary<string, string>();
        private readonly Dictionary<string, string> _redirects = new Dictionary<string, string>();

        public WebScriptingServer(SystemBase system, string directory)
        {
            System = system;
            _directory = directory;
            WebServer.AddRoute($"/cws/{directory}", CwsOnReceivedRequestEvent);
            WebServer.AddRoute($"/cws/{directory}/{{*rest}}", CwsOnReceivedRequestEvent);
        }

        public SystemBase System { get; }

        public void AddRedirect(string routePattern, string redirectUrl)
        {
            var finalPattern = Regex.Replace(routePattern, @"\/([^\s<\/]+)|\/<(\w*)(?::([^\s>]+))?>|\/",
                delegate (Match match)
                {
                    if (!string.IsNullOrEmpty(match.Groups[1].Value)) return @"\/" + match.Groups[1].Value;

                    if (!string.IsNullOrEmpty(match.Groups[3].Value)) return @"\/(" + match.Groups[3].Value + ")";

                    return !string.IsNullOrEmpty(match.Groups[2].Value) ? @"\/(\w+)" : @"\/";
                });

            finalPattern = "^" + finalPattern + "$";

            _redirects[finalPattern] = redirectUrl;
        }

        public virtual void AddRoute(string routePattern, Type handlerType)
        {
            if (!handlerType.IsSubclassOf(typeof(RequestHandler)))
                throw new Exception($"Type \"{handlerType.Name}\" is not derived from {typeof(RequestHandler).Name}");

            var keyNames =
                (from Match match in Regex.Matches(routePattern, @"\/<(\w*)(?::([^\s>]+))?>")
                 select match.Groups[1].Value).ToList();

            var finalPattern = Regex.Replace(routePattern, @"\/([^\s<\/]+)|\/<(\w*)(?::([^\s>]+))?>|\/",
                delegate (Match match)
                {
                    if (!string.IsNullOrEmpty(match.Groups[1].Value)) return @"\/" + match.Groups[1].Value;

                    if (!string.IsNullOrEmpty(match.Groups[3].Value)) return @"\/(" + match.Groups[3].Value + ")";

                    return !string.IsNullOrEmpty(match.Groups[2].Value) ? @"\/(\w+)" : @"\/";
                });

            finalPattern = "^" + finalPattern + "$";

            _originalPatterns[finalPattern] = routePattern;

            _handlers[finalPattern] = handlerType;
            _keyNames[finalPattern] = keyNames;

            Logger.Debug("Added handler type {0} for {1} at \"{2}\"", handlerType.Name, GetType().Name, routePattern);
        }

        private async Task CwsOnReceivedRequestEvent(HttpContext context)
        {
            try
            {
                var request = new WebScriptingRequest(context, $"/{_directory}/{context.Request.RouteValues["rest"]}");
                var remoteAddress = context.Request.HttpContext.Connection.RemoteIpAddress.MapToIPv4();

                //Logger.Highlight($"New WebScripting Request from {remoteAddress} ({request.Method}) {request.PathAndQueryString}");
                //    request.Method, request.PathAndQueryString);
                /*var headerContents = args.Context.Request.Headers.Cast<string>().Aggregate(string.Empty,
                    (current, header) =>
                        current + $"{Environment.NewLine}{header}: {args.Context.Request.Headers[header]}");*/

                //Logger.Debug("Headers:" + headerContents);

                foreach (var redirect in from redirect in _redirects
                                         let pattern = redirect.Key
                                         let match = Regex.Match(request.Path, pattern)
                                         where match.Success
                                         select redirect)
                {
                    try
                    {
                        Logger.Debug("Redirect found!, Redirected to: \"{0}\"", redirect.Value);
                        Logger.Debug($"Query is {context.Request.QueryString}");
                        context.Response.Redirect(redirect.Value + context.Request.QueryString);
                    }
                    catch (Exception e)
                    {
                        Logger.Error("Error with redirect. {0}", e.Message);
                        await HandleErrorAsync(request, e);
                    }

                    return;
                }

                var processed = false;

                foreach (var keyValuePair in _handlers)
                {
                    var pattern = keyValuePair.Key;

                    var match = Regex.Match(request.Path, pattern);

                    if (!match.Success) continue;

                    request.RoutePattern = _originalPatterns[keyValuePair.Key];

                    try
                    {
                        var keyNames = _keyNames[pattern];
                        request.RoutePatternArgs = new Dictionary<string, string>();
                        var index = 0;
                        foreach (var keyName in keyNames)
                        {
                            if (keyName.Length > 0) request.RoutePatternArgs[keyName] = match.Groups[index + 1].Value;

                            index++;
                        }

                        var requestType = keyValuePair.Value;

                        var ctor = requestType.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null,
                            [typeof(WebScriptingServer), typeof(WebScriptingRequest)], null);

                        if (ctor == null)
                        {
                            await HandleErrorAsync(request, 500,
                                "Could not load ctor for handler type: " + requestType.FullName);
                            return;
                        }


                        if (ctor.Invoke(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null,
                            [this, request], CultureInfo.InvariantCulture) is not RequestHandler instance)
                        {
                            await HandleErrorAsync(request, 500,
                                "Could not invoke ctor for handler type: " + requestType.FullName);
                            return;
                        }

                        await instance.ProcessAsync();
                        await context.Response.CompleteAsync();
                        processed = true;
                    }
                    catch (Exception e)
                    {
                        await HandleErrorAsync(request, e);
                        processed = true;
                    }
                }

                if (!processed)
                {
                    Logger.Warn(Logger.LoggerLevel.Debug, "No handler found for request");
                    await HandleErrorAsync(request, 404, "No handler found on this path to deal with the request");
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
                context.Response.Clear();
                context.Response.StatusCode = 500;
            }
        }

        public virtual async Task HandleErrorAsync(WebScriptingRequest request, Exception e)
        {
            Logger.Error(e);
            ErrorLog.Exception("Error handling request", e);
            request.Response.StatusCode = 500;
            request.Response.ContentType = "text/html";
            var content = @"<!DOCTYPE html><html><body><h1>Error 500</h1><h2>" + request.Response.StatusCode +
                          @"</h2><p>" + e.Message + @"</p><p><pre>" + e.StackTrace + @"</pre></p></body></html>";
            try
            {
                await request.Response.WriteAsync(content);
                await request.Response.CompleteAsync();
            }
            catch (Exception e2)
            {
                ErrorLog.Exception("Error responding to 500 error", e2);
            }
        }

        public virtual async Task HandleErrorAsync(WebScriptingRequest request, int statusCode, string message)
        {
            Logger.Warn($"Error {statusCode}: {message}");
            request.Response.StatusCode = statusCode;
            request.Response.ContentType = "text/html";
            var content = @"<!DOCTYPE html><html><body><h1>Error " + statusCode + @"</h1><h2>" + ReasonPhrases.GetReasonPhrase(statusCode) +
                          @"</h2><p>" + message + @"</p></body></html>";
            try
            {
                await request.Response.WriteAsync(content);
                await request.Response.CompleteAsync();
            }
            catch (Exception e)
            {
                ErrorLog.Exception($"Error responding to {statusCode} error", e);
            }
        }
    }
}