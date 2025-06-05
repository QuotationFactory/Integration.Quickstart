using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.Zip;

namespace Integration.Host.Features.Project;

public static class ProjectZipFileHelper
{
    public static async Task<Dictionary<string, byte[]>> ReadProjectZipFileAsync(string zipFilePath)
    {
        await using var fileStream = File.OpenRead(zipFilePath);
        using var zipFile = new ZipFile(fileStream);

        var zipFiles = new Dictionary<string, byte[]>();

        foreach (ZipEntry zipEntry in zipFile)
        {
            // ignore directories
            if (!zipEntry.IsFile) continue;

            var fileName = zipEntry.Name.Contains("/") ? zipEntry.Name.Substring(zipEntry.Name.LastIndexOf('/') + 1) : zipEntry.Name;

            if (zipFiles.ContainsKey(fileName))
                continue;

            var zipStream = zipFile.GetInputStream(zipEntry);
            await using var ms = new MemoryStream();
            await zipStream.CopyToAsync(ms);
            zipFiles.Add(fileName, ms.ToArray());
        }

        return zipFiles;
    }
}
