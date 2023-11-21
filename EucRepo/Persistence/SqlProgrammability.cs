namespace EucRepo.Persistence;

public class SqlProgrammability
{
    protected string Name { get; set; } = null!;
    protected string ProgramType { get; init; } = null!;
    public ICollection<SqlVersion>? Versions { get; init; }

    public string Drop =>
        !string.IsNullOrWhiteSpace(Name) ? $"DROP {ProgramType} [dbo].[{Name}]" : "";

    public string? VersionContent(int version) => Versions?.First(v => v.Version == version).Content;
}

static class SqlProgramTypes
{
    public const string View = "VIEW";
    public const string Procedure = "PROCEDURE";
    public const string Function = "FUNCTION";
    public const string Trigger = "TRIGGER";
}

public class SqlVersion
{
    public SqlVersion()
    {
    }

    public SqlVersion(int version, string content)
    {
        Version = version;
        Content = content;
    }

    public int Version { get; init; } 
    public string Content { get; init; } = null!;
}