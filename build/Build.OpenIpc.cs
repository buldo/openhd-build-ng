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
            CopyFile(
                RootDirectory/ "openhd-openipc" / "br-ext-chip-sigmastar" / "configs" / "ssc338q_openhd_defconfig",
                OpenIpcDir / "br-ext-chip-sigmastar" / "configs" / "ssc338q_openhd_defconfig",
                FileExistsPolicy.Overwrite);

            var packagesDir = OpenIpcDir / "general" / "package";
            var commonConfigIn = packagesDir / "Config.in";
            if(!commonConfigIn.ReadAllText().Contains("openhd"))
            {
                commonConfigIn.AppendAllLines(["source \"$BR2_EXTERNAL_GENERAL_PATH/package/openhd/Config.in\""]);
            }

            var packageDir = packagesDir / "openhd";
            packageDir.DeleteDirectory();
            CopyDirectoryRecursively(
                RootDirectory / "openhd-openipc" / "openhd",
                packageDir,
                DirectoryExistsPolicy.Fail
            );
            CopyFile(
                RootDirectory / "openhd-openipc" / "general" / "scripts" / "ubifs"/ "ubinize_sigmastar.cfg",
                OpenIpcDir / "general" / "scripts" / "ubifs"/ "ubinize_sigmastar.cfg",
                FileExistsPolicy.Overwrite);
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
                { "BOARD", "ssc338q_openhd" }
            };
            envVariables["PWD"] = OpenIpcDir;

            Make("all", OpenIpcDir, envVariables);
        });
}