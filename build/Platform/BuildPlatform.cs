internal class BuildPlatform
{
    public required string Name { get; init; }

    public required string NameStub {get;init;}

    public required string DebianReleaseName {get;init;}

    public required string Arch {get;init;}

    public required string[] BuildDeps {get; init;}

    public required string[] Mirrors {get; init;}
}