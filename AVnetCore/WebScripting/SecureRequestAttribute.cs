using System;

namespace UXAV.AVnetCore.WebScripting
{
    public class SecureRequestAttribute : Attribute
    {
        public SecureRequestAttribute()
        {
        }

        public SecureRequestAttribute(bool redirectToLogin)
        {
            RedirectToLogin = redirectToLogin;
        }

        public bool RedirectToLogin { get; }
    }
}