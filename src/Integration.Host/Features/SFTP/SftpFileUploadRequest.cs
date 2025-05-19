using Integration.Host.Features.FileOrchestrator;

namespace Integration.Host.Features.SFTP;

public class SftpFileUploadRequest(string filePath) : OutputFileOrchestrator.OutputFileCreated(filePath);
