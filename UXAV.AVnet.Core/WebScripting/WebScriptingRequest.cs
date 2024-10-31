using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Routing;
using UXAV.Logging;

namespace UXAV.AVnet.Core.WebScripting
{
    public class WebScriptingRequest
    {
        private readonly HttpRequest _request;
        private readonly string _path;

        internal WebScriptingRequest(HttpContext context, string path)
        {
            _request = context.Request;
            _path = path;
            Response = context.Response;
        }

        public HttpResponse Response { get; }

        public string Method => _request.Method;

        public string ContentType => _request.ContentType;

        public long? ContentLength => _request.ContentLength;

        public Stream InputStream => _request.Body;

        public IRequestCookieCollection Cookies => _request.Cookies;

        public RouteValueDictionary RouteValues => _request.RouteValues;

        public string Path => _path;

        public string UserHostAddress => _request.HttpContext.Connection.RemoteIpAddress.ToString();

        public string PathAndQueryString => _path + _request.QueryString;

        public IHeaderDictionary Headers => _request.Headers;

        public Dictionary<string, string> RoutePatternArgs { get; internal set; }

        public string RoutePattern { get; internal set; }

        public string QueryString => _request.QueryString.Value;

        public IQueryCollection Query => _request.Query;

        public async Task<string> GetStringContentsAsync()
        {
            using var reader = new StreamReader(InputStream);
            return await reader.ReadToEndAsync();
        }
    }
}