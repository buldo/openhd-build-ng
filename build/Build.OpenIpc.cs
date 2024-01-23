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
        .DependsOn(EnsureBuildDirExists)
        .Executes(() =>
        {
            if (OpenIpcDir.Exists())
            {
                Serilog.Log.Warning("OpenIPC directory exists. Skipping clone");
            }
            else
            {
                Git($"clone https://github.com/OpenIPC/firmware.git {OpenIpcDir}", WorkDir);
            }
        });

    Target ApplyOhdToOIpc => _ => _
        .DependsOn(EnsureOpenIpcDlDirExists)
        .Executes(() => 
        {
            var boardConfig = OpenIpcDir / "br-ext-chip-sigmastar" / "configs" / "ssc338q_ultimate_defconfig";
            if(!boardConfig.ReadAllText().Contains("BR2_PACKAGE_OPENHD=y"))
            {
                boardConfig.AppendAllLines(["BR2_PACKAGE_OPENHD=y"]);
            }

            var packagesDir = OpenIpcDir / "general" / "package";
            var commonConfigIn = packagesDir / "Config.in";
            if(!commonConfigIn.ReadAllText().Contains("openhd"))
            {
                commonConfigIn.AppendAllLines(["source \"$BR2_EXTERNAL_GENERAL_PATH/package/openhd/Config.in\""]);
            }

            var packageDir = packagesDir / "openhd";
            CopyFile(RootDirectory / "openhd-openipc" / "openhd" / "Config.in", packageDir / "Config.in", FileExistsPolicy.Overwrite, true);
        });

    Target BuildOpenIpc => _ => _
        .DependsOn(CheckoutOpenIpc)
        .DependsOn(EnsureOpenIpcDlDirExists)
        .DependsOn(ApplyOhdToOIpc)
        .Executes(() =>
        {
            var envVariables = new Dictionary<string, string>(EnvironmentInfo.Variables)
            {
                { "BR2_DL_DIR", OpenIpcDlDir },
                { "BOARD", "ssc338q_ultimate_defconfig" }
            };
            envVariables["PWD"] = OpenIpcDir;

            Make("build", OpenIpcDir, envVariables);
        });
}