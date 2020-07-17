using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Crestron.SimplSharp;
using Crestron.SimplSharp.WebScripting;
using UXAV.AVnetCore.Logging;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace UXAV.AVnetCore.WebScripting
{
    public class WebScriptingServer
    {
        private readonly Models.SystemBase _system;
        private readonly string _directory;
        private readonly Dictionary<string, Type> _handlers = new Dictionary<string, Type>();
        private readonly Dictionary<string, List<string>> _keyNames = new Dictionary<string, List<string>>();
        private readonly Dictionary<string, string> _originalPatterns = new Dictionary<string, string>();
        private readonly Dictionary<string, string> _redirects = new Dictionary<string, string>();
        private readonly HttpCwsServer _cws;

        public WebScriptingServer(Models.SystemBase system, string directory)
        {
            _system = system;
            _directory = directory;
            _cws = new HttpCwsServer(directory);
            _cws.Register();
            _cws.ReceivedRequestEvent += CwsOnReceivedRequestEvent;
            CrestronEnvironment.ProgramStatusEventHandler += OnProgramStatusEventHandler;
        }

        private void OnProgramStatusEventHandler(eProgramStatusEventType programeventtype)
        {
            if (programeventtype != eProgramStatusEventType.Stopping) return;
            Task.Run(Unregister);
        }

        private void Unregister()
        {
            Logger.Highlight("Shutting down and unregistering {0}, at path \"/cws/{1}\"", GetType().Name, _directory);
            _cws.Unregister();
        }

        public Models.SystemBase System => _system;

        public void AddRedirect(string routePattern, string redirectUrl)
        {
            var finalPattern = Regex.Replace(routePattern, @"\/([^\s<\/]+)|\/<(\w*)(?::([^\s>]+))?>|\/",
                delegate(Match match)
                {
                    if (!string.IsNullOrEmpty(match.Groups[1].Value))
                    {
                        return @"\/" + match.Groups[1].Value;
                    }

                    if (!string.IsNullOrEmpty(match.Groups[3].Value))
                    {
                        return @"\/(" + match.Groups[3].Value + ")";
                    }

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
                delegate(Match match)
                {
                    if (!string.IsNullOrEmpty(match.Groups[1].Value))
                    {
                        return @"\/" + match.Groups[1].Value;
                    }

                    if (!string.IsNullOrEmpty(match.Groups[3].Value))
                    {
                        return @"\/(" + match.Groups[3].Value + ")";
                    }

                    return !string.IsNullOrEmpty(match.Groups[2].Value) ? @"\/(\w+)" : @"\/";
                });

            finalPattern = "^" + finalPattern + "$";

            _originalPatterns[finalPattern] = routePattern;

            _handlers[finalPattern] = handlerType;
            _keyNames[finalPattern] = keyNames;

            Logger.Log("Added handler type {0} for {1} at \"{2}\"", handlerType.Name, GetType().Name, routePattern);
        }

        private void CwsOnReceivedRequestEvent(object sender, HttpCwsRequestEventArgs args)
        {
            var sw = new Stopwatch();
            sw.Start();

            var request = new WebScriptingRequest(args.Context);

            try
            {
                var decodedPath = WebUtility.UrlDecode(args.Context.Request.Path);
                //var remoteAddress = args.Context.Request.UserHostAddress;
                //var hostName = args.Context.Request.UserHostName;

                //Logger.Highlight(Logger.LoggerLevel.Debug, "New WebScripting Request from {0} ({1}) {2} {3}", remoteAddress, hostName,
                //    request.Method, request.PathAndQueryString);
                /*var headerContents = args.Context.Request.Headers.Cast<string>().Aggregate(string.Empty,
                    (current, header) =>
                        current + $"{Environment.NewLine}{header}: {args.Context.Request.Headers[header]}");*/

                //Logger.Debug("Headers:" + headerContents);

                foreach (var redirect in from redirect in _redirects
                    let pattern = redirect.Key
                    let match = Regex.Match(decodedPath, pattern)
                    where match.Success
                    select redirect)
                {
                    try
                    {
                        Logger.Debug("Redirect found!, Redirected to: \"{0}\"", redirect.Value);
                        args.Context.Response.Redirect(redirect.Value);
                    }
                    catch (Exception e)
                    {
                        Logger.Error("Error with redirect. {0}", e.Message);
                        HandleError(request, e);
                    }

                    return;
                }

                var processed = false;

                foreach (var keyValuePair in _handlers)
                {
                    var pattern = keyValuePair.Key;

                    var match = Regex.Match(decodedPath, pattern);

                    if (!match.Success) continue;

                    request.RoutePattern = _originalPatterns[keyValuePair.Key];

                    try
                    {
                        var keyNames = _keyNames[pattern];
                        request.RoutePatternArgs = new Dictionary<string, string>();
                        var index = 0;
                        foreach (var keyName in keyNames)
                        {
                            if (keyName.Length > 0)
                            {
                                request.RoutePatternArgs[keyName] = match.Groups[index + 1].Value;
                            }

                            index++;
                        }

                        var requestType = keyValuePair.Value;

                        var ctor = requestType.GetConstructor(BindingFlags.Public | BindingFlags.Instance, null,
                            new[] {typeof(WebScriptingServer), typeof(WebScriptingRequest)}, null);

                        if (ctor == null)
                        {
                            HandleError(request, 500, "Server Error",
                                "Could not load ctor for handler type: " + requestType.FullName);
                            return;
                        }

                        var instance = ctor.Invoke(BindingFlags.Public | BindingFlags.Instance, null,
                            new object[] {this, request}, CultureInfo.InvariantCulture) as RequestHandler;

                        if (instance == null)
                        {
                            HandleError(request, 500, "Server Error",
                                "Could not invoke ctor for handler type: " + requestType.FullName);
                            return;
                        }

                        instance.Process();
                        processed = true;
                    }
                    catch (Exception e)
                    {
                        HandleError(request, e);
                        processed = true;
                    }
                }

                if (processed) return;
                Logger.Warn(Logger.LoggerLevel.Debug, "No handler found for request");
                HandleError(request, 404, "Not Found", "No handler found on this path to deal with the request");
            }
            catch (Exception e)
            {
                HandleError(request, e);
            }
        }

        public virtual void HandleError(WebScriptingRequest request, Exception e)
        {
            Logger.Error(e);
            ErrorLog.Exception("Error handling request", e);
            request.Response.StatusCode = 500;
            request.Response.StatusDescription = "Server Error";
            request.Response.ContentType = "text/html";
            var content = @"<!DOCTYPE html><html><body><h1>Error 500</h1><h2>" + request.Response.StatusDescription +
                          @"</h2><p>" + e.Message + @"</p><p><pre>" + e.StackTrace + @"</pre></p></body></html>";
            try
            {
                request.Response.Write(content, true);
            }
            catch (Exception e2)
            {
                ErrorLog.Exception($"Error responding to 500 error", e2);
            }
        }

        public virtual void HandleError(WebScriptingRequest request, int statusCode, string statusDescription,
            string message)
        {
            Logger.Warn("Error {0} {1}: {2}", statusCode, statusDescription, message);
            request.Response.StatusCode = statusCode;
            request.Response.StatusDescription = statusDescription;
            request.Response.ContentType = "text/html";
            var content = @"<!DOCTYPE html><html><body><h1>Error " + statusCode + @"</h1><h2>" + statusDescription +
                          @"</h2><p>" + message + @"</p></body></html>";
            try
            {
                request.Response.Write(content, true);
            }
            catch (Exception e)
            {
                ErrorLog.Exception($"Error responding to {statusCode} error", e);
            }
        }
    }
}