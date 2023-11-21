namespace EucRepo.Models;

public class ApiCallSettings
{
    public const string Key = "ApiCallSettings";
    public string FileUploadPath { get; set; } = null!;
    public string PoshApiUrl { get; set; } = null!;
    public Dictionary<string, BatchSettings> BatchSettings { get; set; } = null!;
}

public class BatchSettings
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string? Notes { get; set; }
    public string[] FileMasks { get; set; } = Array.Empty<string>();
    public int FileCount { get; set; } = 0;
}

public class EucRepoRoles
{
    public Dictionary<string, string[]> Roles { get; set; } = null!;
}