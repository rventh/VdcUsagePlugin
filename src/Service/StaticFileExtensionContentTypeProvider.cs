// Copyright © Veeam Software Group GmbH.


// Copyright © Veeam Software Group GmbH.

using System.Collections.Generic;

namespace MyPlugin.Service;

public class StaticFileExtensionContentTypeProvider : Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider
{
    public StaticFileExtensionContentTypeProvider() : base(new Dictionary<string, string>
    {
        { ".htm", "text/html" },
        { ".html", "text/html" },
        { ".xml", "text/xml" },
        { ".yaml", "application/x-yaml" },
        { ".yml", "application/x-yaml" },
        { ".json", "application/json" },
        { ".js", "application/x-javascript" },
        { ".jsc", "application/x-javascript" },
        { ".jsonp", "application/x-javascript" },
        { ".proto", "application/protobuf" },
        { ".woff", "font/woff" },
        { ".woff2", "font/woff2" },
        { ".css", "text/css" },
        { ".cssc", "text/css" },
        { ".gif", "image/gif" },
        { ".bmp", "image/bmp" },
        { ".ico", "image/x-icon" },
        { ".ief", "image/ief" },
        { ".jpe", "image/jpeg" },
        { ".jpeg", "image/jpeg" },
        { ".jpg", "image/jpeg" },
        { ".png", "image/png" },
        { ".pnz", "image/png" },
        { ".tif", "image/tiff" },
        { ".tiff", "image/tiff" },
        { ".svg", "image/svg+xml" },
        { ".dll", "application/octet-stream" },
        { ".exe", "application/octet-stream" },
        { ".pdb", "application/octet-stream" },
    })
    {
    }
}
