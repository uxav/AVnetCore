using System;
using System.IO;
using System.Linq;
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
        var directories = new DirectoryInfo[]
        {
            new(SystemBase.ProgramApplicationDirectory),
            new(SystemBase.ProgramNvramDirectory),
            new(SystemBase.ProgramUserDirectory)
        };
        var result = directories.Select(x => GetDirectory(x)).ToArray();

        await WriteResponseAsync(result);
    }

    private JToken GetDirectory(DirectoryInfo directory)
    {
        var provider = new FileExtensionContentTypeProvider();
        return JToken.FromObject(new
        {
            name = directory.Name,
            directories = directory.EnumerateDirectories()
                .Select(x => GetDirectory(x)),
            files = directory.EnumerateFiles().Select(x =>
            {
                return new
                {
                    name = x.Name,
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
