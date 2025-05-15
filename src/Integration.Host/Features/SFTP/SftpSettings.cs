namespace Integration.Host.Features.SFTP;

public class SftpSettings
{
    public string Host { get; set; }
    public int Port { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
    public string RemoteDirectory { get; set; }
    public bool UploadZipFile { get; set; }
}
