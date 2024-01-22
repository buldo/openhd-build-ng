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
        [PathVariable("mmdebstrap")]
        Tool Mmdebstrap;
    
        [PathVariable("cmake")]
        Tool Cmake;
    
        [PathVariable("make")]
        Tool Make;
}