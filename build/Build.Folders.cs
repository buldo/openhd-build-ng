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
    /// WorkDir - directory for all temp files
    /// `- ToolchainsDir - uncompressed toolchains
    /// `- SysrootsDirs - uncompressed sysroots
    /// `- BuildDir - Sources files / clonned repos that will be built
    ///     `- OpenIpcDir - OpenIPC repo clone folder
    /// `- CacheDir
    ///     `- OpenIpcDlDir - BuildRoot download folder for openIPC 
    
    readonly AbsolutePath WorkDir;
    readonly AbsolutePath ToolchainsDir;
    readonly AbsolutePath SysrootsDirs;
    readonly AbsolutePath OpenIpcDir;
    readonly AbsolutePath CacheDir;
    readonly AbsolutePath OpenIpcDlDir;
    readonly AbsolutePath BuildDir;

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

    Target EnsureCacheDirExists => _ => _
        .Executes(() =>
        {
            if (!CacheDir.Exists())
            {
                CacheDir.CreateDirectory();
            }
        });

    Target EnsureBuildDirExists => _ => _
        .Executes(() =>
        {
            if (!BuildDir.Exists())
            {
                BuildDir.CreateDirectory();
            }
        });

    Target EnsureOpenIpcDlDirExists => _ => _
            .DependsOn(EnsureCacheDirExists)
            .Executes(() =>
            {
                if (!OpenIpcDlDir.Exists())
                {
                    OpenIpcDlDir.CreateDirectory();
                }
            });
}