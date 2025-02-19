using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Crestron.SimplSharp;
using UXAV.AVnet.Core.Models;
using UXAV.Logging;

namespace UXAV.AVnet.Core.Web;

/// <summary>
/// Configuration for the ASP.NET core web server
/// </summary>
public class WebServerConfiguration
{
    internal WebServerConfiguration()
    {
    }

    /// <summary>
    /// Port to be used for http
    /// </summary>
    public int Port { get; set; } = 8080 + (int)InitialParametersClass.ApplicationNumber;

    /// <summary>
    /// Port to be used for https
    /// </summary>
    public int SecurePort { get; set; } = 9090 + (int)InitialParametersClass.ApplicationNumber;

    /// <summary>
    /// Certificate to be used for https
    /// </summary>
    public X509Certificate2 Certificate { get; set; } = null;

    /// <summary>
    /// Configure the server with additional settings
    /// </summary>
    /// <example>
    /// <![CDATA[
    ///    webServerConfiguration.ConfigureServer = (server) =>
    ///    {
    ///    var basePath = SystemBase.ProgramApplicationDirectory;
    ///    basePath = Path.Combine(basePath, "webroot/ch5");
    ///    Logger.Highlight($"Serving static files from {basePath}");
    ///    server.SetupUI("/ui", basePath);
    ///    server.AddWebService<Ch5ApiHandler>();
    ///    };]]>
    /// </example>
    public Action<WebServer> ConfigureServer { get; set; } = null;

    /// <summary>
    /// Use https redirection or not
    /// </summary>
    public bool UseHttpsRedirection { get; set; } = true;

    /// <summary>
    /// Create a new WebServerConfiguration object
    /// </summary>
    /// <param name="port">port to be used for http</param>
    /// <param name="securePort">port to be used for https</param>
    /// <param name="certificatePath">path of certificate to try and load</param>
    /// <param name="cetificatePassword">password of certificate to try and load</param>
    /// <returns>new WebServerConfiguration</returns>
    /// <remarks>
    /// If the certificatePath and certificatePassword are not provided or the certificate is expired, a new self signed certificate will be generated.
    /// </remarks>
    public static WebServerConfiguration Create(int port, int securePort, string certificatePath = null, string cetificatePassword = null)
    {
        X509Certificate2 certificate = null;
        var selfSignedCertificatePath = Path.Combine(SystemBase.ProgramNvramDirectory, "certs", "server.pfx");

        if (!string.IsNullOrEmpty(certificatePath) && !string.IsNullOrEmpty(cetificatePassword))
        {
            Logger.Log("Loading certificate from {0}", certificatePath);
            try
            {
                if (File.Exists(certificatePath))
                {
                    certificate = new X509Certificate2(certificatePath, cetificatePassword);
                    Logger.Highlight("Loaded PFX Certificate: {0}", certificatePath);
                }
            }
            catch (Exception e)
            {
                Logger.Error($"Failed to load Web Server Certificate: {certificatePath}, {e.Message}");
            }
        }

        if (certificate == null || certificate.NotAfter < DateTime.Now)
        {
            Logger.Warn("Certificate not found or expired. Will attempt to load self signed certificate.");
            certificate = null;
            if (Path.Exists(selfSignedCertificatePath))
            {
                Logger.Log("Self signed certificate found. Loading certificate.");
                certificate = new X509Certificate2(selfSignedCertificatePath, "password");
            }
        }

        if (certificate == null || certificate.NotAfter < DateTime.Now)
        {
            Logger.Warn("Certificate not found or expired. Generating new self signed certificate.");

            var hostname = SystemBase.HostName;
            var directoryName = Path.GetDirectoryName(selfSignedCertificatePath);
            if (!Directory.Exists(directoryName))
            {
                Logger.Warn($"Certificate directory not found. Creating new directory: {directoryName}");
                Directory.CreateDirectory(directoryName!);
            }

            using var rsa = RSA.Create();
            var orgranization = "UXAV";
            var country = "GB";
            var request = new CertificateRequest(
                new X500DistinguishedName($"CN={hostname}, O={orgranization}, OU={country}"),
                rsa,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);
            var cert = request.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(10));
            File.WriteAllBytes(selfSignedCertificatePath, cert.Export(X509ContentType.Pfx, "password"));
            certificate = new X509Certificate2(cert.Export(X509ContentType.Pfx, "password"), "password");
        }

        var config = new WebServerConfiguration
        {
            Port = port,
            SecurePort = securePort,
            Certificate = certificate
        };
        return config;
    }
}