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
using Serilog;
using System.Net.Http.Headers;
using System.IO;

class Build : NukeBuild
{
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode

    public static int Main () => Execute<Build>(x => x.CreateSysroot);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    readonly AbsolutePath WorkDir = RootDirectory / "workdir";
    readonly AbsolutePath ToolchainsDir = RootDirectory / "toolchains";
    readonly AbsolutePath SysrootsDirs = RootDirectory / "sysroots";

    [PathVariable("mmdebstrap")]
    Tool Mmdebstrap;

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

    Target CreateSysroot => _ => _
        .Executes(() =>
        {
            var platform = SupportedPlatforms.RaspberryPi;
            var sysrootDir = SysrootsDirs / $"{platform.NameStub}-{platform.DebianReleaseName}-{platform.Arch}";
            Log.Information($"Creating sysroot for {sysrootDir.Name}");
            sysrootDir.CreateOrCleanDirectory();
            
            Log.Information($"Running mmdebstrap for {sysrootDir.Name}");

            var sourcesFile = RootDirectory / $"{platform.NameStub}-{platform.DebianReleaseName}-{platform.Arch}.sources.list";
            if(!sourcesFile.FileExists())
            {
                throw new FileNotFoundException($"Source file {sourcesFile} not found");
            }

            string[] debstrapArgsArray = [
                "--mode=unshare",
                $"--architectures={platform.Arch}",
                "--variant=extract",
                $"--keyring={RootDirectory/"keyring"}",
                // "--aptopt=Acquire::AllowInsecureRepositories true",
                // "--aptopt=Acquire::AllowDowngradeToInsecureRepositories true",
                // "--aptopt=APT::Get::AllowUnauthenticated true",
                $"--include={string.Join(',',platform.BuildDeps)}",
                "--dpkgopt=path-exclude=\"*\"",
                "--dpkgopt=path-include=\"/lib/*\"",
                "--dpkgopt=path-include=\"/lib32/*\"",
                "--dpkgopt=path-include=\"/usr/include/*\"",
                "--dpkgopt=path-include=\"/usr/lib/*\"",
                "--dpkgopt=path-include=\"/usr/lib32/*\"",
                "--dpkgopt=path-exclude=\"/usr/lib/debug/*\"",
                "--dpkgopt=path-exclude=\"/usr/lib/python*\"",
                "--dpkgopt=path-exclude=\"/usr/lib/valgrind/*\"",
                "--dpkgopt=path-include=\"/usr/share/pkgconfig/*\"",
                $"{platform.DebianReleaseName}",
                $"{sysrootDir}",
                $"{sourcesFile}",
                "-v"
                ];
            var debstrapArgs = string.Join(' ', debstrapArgsArray);
            Log.Information($"Calling {debstrapArgs}");
            Mmdebstrap(debstrapArgs);
        });

}