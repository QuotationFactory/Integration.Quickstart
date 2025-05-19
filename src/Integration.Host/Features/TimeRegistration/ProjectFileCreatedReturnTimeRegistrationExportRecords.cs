using Integration.Host.Features.FileOrchestrator;

namespace Integration.Host.Features.TimeRegistration;

public class ProjectFileCreatedReturnTimeRegistrationExportRecords(string filePath) : OutputFileOrchestrator.OutputFileCreated(filePath);
