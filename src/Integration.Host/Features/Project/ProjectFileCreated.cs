using Integration.Host.Features.FileOrchestrator;

namespace Integration.Host.Features.Project;

public class ProjectFileCreated(string filePath) : OutputFileOrchestrator.OutputFileCreated(filePath);
