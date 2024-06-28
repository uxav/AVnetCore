using System;

namespace UXAV.AVnet.Core.WebScripting
{
    public class RequestHandlerMethodAttribute : Attribute
    {
        public RequestHandlerMethod MethodType { get; }

        public RequestHandlerMethodAttribute(RequestHandlerMethod methodType)
        {
            MethodType = methodType;
        }
    }

    public enum RequestHandlerMethod
    {
        Get,
        Post,
        Put,
        Delete,
        Patch,
        Options,
        Head
    }
}