using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using Crestron.SimplSharp.CrestronIO;
using Crestron.SimplSharp.WebScripting;
using Stream = Crestron.SimplSharp.CrestronIO.Stream;

namespace UXAV.AVnetCore.WebScripting
{
    public class WebScriptingRequest
    {
        private readonly HttpCwsRequest _request;

        internal WebScriptingRequest(HttpCwsContext context)
        {
            _request = context.Request;
            Response = context.Response;
        }

        public HttpCwsResponse Response { get; }

        public string Method => _request.HttpMethod;

        public string ContentType => _request.ContentType;

        public int ContentLength => _request.ContentLength;

        public Stream InputStream => _request.InputStream;

        public HttpCwsCookieCollection Cookies => _request.Cookies;

        public string RawUrl => _request.RawUrl;

        public Uri Url => _request.Url;

        public string Path => _request.Path;

        public string UserHostAddress => _request.UserHostAddress;

        public string UserHostName => _request.UserHostName;

        public string PathAndQueryString => Url.PathAndQuery;

        public NameValueCollection Headers => _request.Headers;

        public Dictionary<string, string> RoutePatternArgs { get; internal set; }

        public string RoutePattern { get; internal set; }

        public string QueryString => Url.Query;

        public NameValueCollection Query => _request.QueryString;

        public string GetStringContents()
        {
            string result;

            using (var reader = new StreamReader(InputStream))
            {
                result = reader.ReadToEnd();
            }

            return result;
        }
    }
}