using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Crestron.SimplSharp;
using Microsoft.AspNetCore.StaticFiles;
using Newtonsoft.Json.Linq;
using UXAV.AVnet.Core.Models;

namespace UXAV.AVnet.Core.WebScripting.InternalApi;

internal class AppFilesApiHandler : ApiRequestHandler
{
    public AppFilesApiHandler(WebScriptingServer server, WebScriptingRequest request)
        : base(server, request, true)
    {
    }

    [SecureRequest]
    public async void Get()
    {
        try
        {
            var directories = new List<DirectoryInfo>();
            directories.AddRange([
                new(SystemBase.ProgramApplicationDirectory),
                new(SystemBase.ProgramNvramDirectory),
                new(SystemBase.ProgramUserDirectory),
                new(SystemBase.ProgramHtmlDirectory),
            ]);
            if (CrestronEnvironment.DevicePlatform == eDevicePlatform.Appliance)
            {
                directories.Add(new DirectoryInfo("/ftp/cert"));
                directories.Add(new DirectoryInfo("/ftp/firmware"));
                directories.Add(new DirectoryInfo("/ftp/plog"));
                directories.Add(new DirectoryInfo("/ftp/auditlog"));
                directories.Add(new DirectoryInfo("/ftp/autoupdatelogs"));
                directories.Add(new DirectoryInfo("/ftp/rm"));
                directories.Add(new DirectoryInfo("/ftp/sshbanner"));
                directories.Add(new DirectoryInfo("/ftp/temp"));
            }

            var result = directories.Where(d => d.Exists)
                .Select(x => GetDirectory(x))
                .ToArray();

            await WriteResponseAsync(result);
        }
        catch (Exception e)
        {
            await HandleErrorAsync(e);
        }
    }

    private JToken GetDirectory(DirectoryInfo directory)
    {
        var provider = new FileExtensionContentTypeProvider();
        return JToken.FromObject(new
        {
            name = directory.Name,
            path = directory.FullName,
            directories = directory.EnumerateDirectories()
                .Select(x => GetDirectory(x)),
            files = directory.EnumerateFiles().Select(x =>
            {
                return new
                {
                    name = x.Name,
                    path = x.FullName,
                    size = x.Length,
                    sizeString = BytesToString(x.Length),
                    date = x.CreationTimeUtc,
                    ext = x.Extension,
                    type = provider.TryGetContentType(x.Name, out var contentType) ? contentType : "application/octet-stream"
                };
            }).ToList()
        });
    }

    private static string BytesToString(long byteCount)
    {
        string[] suf = { "Bytes", "KB", "MB", "GB", "TB", "PB", "EB" };
        if (byteCount == 0)
            return "0 " + suf[0];
        long bytes = Math.Abs(byteCount);
        int place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
        double num = Math.Round(bytes / Math.Pow(1024, place), 1);
        return (Math.Sign(byteCount) * num).ToString() + " " + suf[place];
    }
}
