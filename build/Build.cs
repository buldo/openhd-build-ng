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
using Nuke.Common.Tools.Git;
using static Nuke.Common.Tools.Git.GitTasks;
using System.Collections.Generic;
using Stubble.Core.Builders;

partial class Build : NukeBuild
{
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode

    public static int Main () => Execute<Build>(x => x.BuildOpenIpc);

    public Build()
    {
        TargetPlatform = SupportedPlatforms.RaspberryPi;
        WorkDir = RootDirectory / "workdir";
        ToolchainsDir = RootDirectory / "toolchains";
        SysrootsDirs = RootDirectory / "sysroots";
        OpenIpcDir = WorkDir / "openipc";    
    }
    
    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    readonly BuildPlatform TargetPlatform;
    readonly AbsolutePath WorkDir;
    readonly AbsolutePath ToolchainsDir;
    readonly AbsolutePath SysrootsDirs;
    readonly AbsolutePath OpenIpcDir;

    Target CleanWorkdir => _ => _
        .Executes(() =>
        {
            WorkDir.CreateOrCleanDirectory();
        });

    Target EnsureWorkDirExists => _ => _
        .Executes(() =>
        {
            if (!WorkDir.Exists())
            {
                WorkDir.CreateDirectory();
            }
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
        .Before(BuildOpenHd)
        .Executes(() =>
        {
            var sysrootDir = GetSysrootDir();
            Log.Information($"Creating sysroot for {sysrootDir.Name}");
            sysrootDir.CreateOrCleanDirectory();
            
            Log.Information($"Running mmdebstrap for {sysrootDir.Name}");

            var sourcesFile = RootDirectory / $"{TargetPlatform.NameStub}-{TargetPlatform.DebianReleaseName}-{TargetPlatform.Arch}.sources.list";
            if(!sourcesFile.FileExists())
            {
                throw new FileNotFoundException($"Source file {sourcesFile} not found");
            }

            string[] debstrapArgsArray = [
                "--mode=unshare",
                $"--architectures={TargetPlatform.Arch}",
                "--variant=extract",
                $"--keyring={RootDirectory/"keyring"}",
                $"--include={string.Join(',',TargetPlatform.BuildDeps)}",
                // "--dpkgopt=path-exclude=\"*\"",
                // "--dpkgopt=path-include=\"/lib/*\"",
                // "--dpkgopt=path-include=\"/lib32/*\"",
                // "--dpkgopt=path-include=\"/usr/include/*\"",
                // "--dpkgopt=path-include=\"/usr/lib/*\"",
                // "--dpkgopt=path-include=\"/usr/lib32/*\"",
                // "--dpkgopt=path-exclude=\"/usr/lib/debug/*\"",
                // "--dpkgopt=path-exclude=\"/usr/lib/python*\"",
                // "--dpkgopt=path-exclude=\"/usr/lib/valgrind/*\"",
                // "--dpkgopt=path-include=\"/usr/share/pkgconfig/*\"",
                // "--dpkgopt=path-include=\"/usr/pkgconfig/*\"",
                $"{TargetPlatform.DebianReleaseName}",
                $"{sysrootDir}",
                $"{sourcesFile}",
                "-v"
                ];
            var debstrapArgs = string.Join(' ', debstrapArgsArray);
            Log.Information($"Calling {debstrapArgs}");
            Mmdebstrap(debstrapArgs);
        });

    Target BuildOpenHd => _ => _
        .Executes(() => 
        {
            WorkDir.CreateDirectory();
            Log.Information("Cloning OpenHD");
            (WorkDir/"OpenHD").DeleteDirectory();
            Git("clone --recurse-submodules https://github.com/OpenHD/OpenHD.git", WorkDir);
            Log.Information("Clonned");
            var buildDir = WorkDir / "OpenHD" / "OpenHD" / "build";
            
            Log.Information("Generating toochain file");
            var toolchainFilePath = WorkDir / $"{TargetPlatform.NameStub}-{TargetPlatform.DebianReleaseName}-{TargetPlatform.Arch}.cmake";
            Log.Information($"Toolchain file name \"{toolchainFilePath.Name}\"");
            
            var stubble = new StubbleBuilder().Build();
            var data = new Dictionary<string, string>()
            {
                {"CMAKE_SYSTEM_NAME", "Linux"},
                {"CMAKE_SYSTEM_PROCESSOR", TargetPlatform.Arch},
                {"CMAKE_SYSROOT", GetSysrootDir()},
                {"CMAKE_STAGING_PREFIX", GetSysrootDir()},
                {"CMAKE_C_COMPILER", "arm-linux-gnueabihf-gcc-10"},
                {"CMAKE_CXX_COMPILER", "arm-linux-gnueabihf-g++-10"}
            };
            var rendered = stubble.Render(File.ReadAllText(RootDirectory/"toolchain.cmake.template"), data);
            File.WriteAllText(toolchainFilePath, rendered);

            buildDir.CreateOrCleanDirectory();

            var sysroot = GetSysrootDir();
            
            var cmakeEnvVariables = new Dictionary<string, string>(EnvironmentInfo.Variables)
            {
                {"PKG_CONFIG_PATH", ""},
                // TODO: {sysroot}/usr/lib/arm-linux-gnueabihf/pkgconfig have to be fixed to generated
                {"PKG_CONFIG_LIBDIR", $"{sysroot}/usr/lib/pkgconfig:{sysroot}/usr/share/pkgconfig:{sysroot}/usr/lib/arm-linux-gnueabihf/pkgconfig"},
                {"PKG_CONFIG_SYSROOT_DIR", sysroot},
            };
            Cmake($".. -DCMAKE_TOOLCHAIN_FILE={toolchainFilePath}", buildDir, cmakeEnvVariables);
            Make($"-j{Environment.ProcessorCount}", buildDir);
        });

    AbsolutePath GetSysrootDir()
    {
        return SysrootsDirs / $"{TargetPlatform.NameStub}-{TargetPlatform.DebianReleaseName}-{TargetPlatform.Arch}";
    }
}