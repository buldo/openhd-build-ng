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
    Target CheckoutOpenIpc => _ => _
        .After(CleanWorkdir)
        .DependsOn(EnsureWorkDirExists)
        .Executes(() =>
        {
            if (OpenIpcDir.Exists())
            {
                Serilog.Log.Warning("OpenIPC directory exists. Skipping clone");
            }
            else
            {
                Git($"clone https://github.com/OpenIPC/firmware.git {OpenIpcDir.Name}", WorkDir);
            }
        });

    Target BuildOpenIpc => _ => _
        .DependsOn(CheckoutOpenIpc)
        .Executes(() =>
        {
        });
}