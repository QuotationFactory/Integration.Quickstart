namespace Integration.Host.Features.SFTP;

public class SftpSettings
{
    public required string Host { get; set; }
    public int Port { get; set; }
    public required string Username { get; set; }
    public required string Password { get; set; }
    public required string RemoteDirectory { get; set; }
    public bool UploadZipFile { get; set; }
}
