using System.IO;
using MetalHeaven.Integration.Shared.Classes;

namespace Rhodium24.Host.Features.AgentOutputFile
{
    public static class SettingsExtension
    {
        public static string GetOrCreateDirectory(string rootDirectoryPath, string subDir = "")
        {
            var totalPath = Path.Combine(rootDirectoryPath, subDir);

            if (Directory.Exists(totalPath))
                return totalPath;

            if (!Directory.Exists(totalPath))
                Directory.CreateDirectory(totalPath);

            return totalPath;
        }
        public static string GetInputDirectory(this AgentSettings settings) => GetOrCreateDirectory(Path.Combine(settings.RootDirectory, "Input"));
        public static string GetOutputDirectory(this AgentSettings settings) => GetOrCreateDirectory(Path.Combine(settings.RootDirectory, "Output"));
        public static string GetProcessingDirectory(this AgentSettings settings) => GetOrCreateDirectory(settings.GetOutputDirectory(), "Processing");
        public static string GetProcessedDirectory(this AgentSettings settings) => GetOrCreateDirectory(settings.GetOutputDirectory(), "Processed");
        public static string GetErrorDirectory(this AgentSettings settings) => GetOrCreateDirectory(settings.GetOutputDirectory(), "Error");
        public static string MoveFileToProcessing(this AgentSettings settings, string filePath) => filePath.MoveFileToDirectory(settings.GetProcessingDirectory());
        public static string MoveFileToProcessed(this AgentSettings settings, string filePath) => filePath.MoveFileToDirectory(settings.GetProcessedDirectory());
        public static string MoveFileToError(this AgentSettings settings, string filePath) => filePath.MoveFileToDirectory(settings.GetErrorDirectory());
        public static string MoveFileToAgentInput(this AgentSettings settings, string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("File not found", filePath);

            var subDirectoryPath = settings.GetInputDirectory();

            var destFileName = Path.Combine(subDirectoryPath, Path.GetFileName(filePath));

            File.Move(filePath, destFileName);

            return destFileName;
        }
        public static string MoveFileToDirectory(this string filePath, string directoryPath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("File not found", filePath);

            var fileInfo = new FileInfo(filePath);

            if (!Directory.Exists(directoryPath))
                Directory.CreateDirectory(directoryPath);

            var destFileName = Path.Combine(directoryPath, fileInfo.Name);
            return filePath.MoveFile(destFileName);
        }
        public static string MoveFile(this string filePath, string destinationFilePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("File not found", filePath);

            var result = destinationFilePath;

            while (File.Exists(result))
            {
                result = Path.Combine(Path.GetDirectoryName(destinationFilePath), $"{Path.GetFileNameWithoutExtension(destinationFilePath)} (1){Path.GetExtension(destinationFilePath)}");
            }

            File.Move(filePath, result);

            return result;
        }
    }
}
