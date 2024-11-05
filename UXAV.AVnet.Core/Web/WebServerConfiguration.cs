using System;
using System.Security.Cryptography.X509Certificates;
using Crestron.SimplSharp;

namespace UXAV.AVnet.Core.Web;

public class WebServerConfiguration()
{
    public int Port { get; set; } = 8080 + (int)InitialParametersClass.ApplicationNumber;
    public int SecurePort { get; set; } = 9090 + (int)InitialParametersClass.ApplicationNumber;
    public X509Certificate2 Certificate { get; set; } = null;
    public Action<WebServer> ConfigureServer { get; set; } = null;
    public bool UseHttpsRedirection { get; set; } = true;
}