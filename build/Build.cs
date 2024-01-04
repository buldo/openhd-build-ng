using System;
using System.Linq;
using Microsoft.Build.Tasks;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.Execution;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.IO.PathConstruction;
using static Nuke.Common.IO.HttpTasks;

class Build : NukeBuild
{
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode

    public static int Main () => Execute<Build>(x => x.BuildGcc);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    readonly AbsolutePath WorkDir = RootDirectory / "workdir";
    readonly AbsolutePath ToolchainsDir = RootDirectory / "toolchains";
    readonly AbsolutePath SysrootsDir = RootDirectory / "sysroots";

    Target CleanWorkdir => _ => _
        .Executes(() =>
        {
            WorkDir.CreateOrCleanDirectory();
        });

    Target CleanBuildSystem => _ => _
        .Before(BuildGcc)
        .Executes(() =>
        {
            ToolchainsDir.CreateOrCleanDirectory();
            ToolchainsDir.CreateOrCleanDirectory();
        });

    Target BuildGcc => _ => _
        .Executes(() =>
        {
            ToolchainsDir.CreateDirectory();
            var destinationName = ToolchainsDir / GccDefs.Gcc13ArchiveName;
            if(!destinationName.FileExists())
            {
                Serilog.Log.Information($"{GccDefs.Gcc13ArchiveName} downloading");
                HttpDownloadFile(
                    GccDefs.Gcc13SourcesLink, 
                    destinationName, 
                    System.IO.FileMode.Create,
                    c => {
                        c.Timeout = TimeSpan.FromMinutes(20);
                        return c;
                    });
                Serilog.Log.Information($"{GccDefs.Gcc13ArchiveName} downloaded");
            }
            else
            {
                Serilog.Log.Information($"{GccDefs.Gcc13ArchiveName} already downloaded");
            }

            var gccSourcesDir = ToolchainsDir / GccDefs.Gcc13Folder;
            if(!gccSourcesDir.DirectoryExists())
            {
                Serilog.Log.Information($"{GccDefs.Gcc13ArchiveName} extracting");
                destinationName.UncompressTo(ToolchainsDir / GccDefs.Gcc13Folder);
                Serilog.Log.Information($"{GccDefs.Gcc13ArchiveName} extracted");
            }
            else
            {
                Serilog.Log.Information($"{GccDefs.Gcc13ArchiveName} already extracted");
            }
        }
        );

    Target DownloadToolchain => _ => _
        .Executes(() =>
        {

        }
        );

    Target Restore => _ => _
        .Executes(() =>
        {
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
        });

}