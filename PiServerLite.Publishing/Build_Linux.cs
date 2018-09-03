using Photon.Framework.Agent;
using Photon.Framework.Tasks;
using Photon.MSBuild;
using System.Threading;
using System.Threading.Tasks;

namespace PiServerLite.Publishing
{
    public class Build_Linux : IBuildTask
    {
        public IAgentBuildContext Context {get; set;}

        
        public async Task RunAsync(CancellationToken token)
        {
            await BuildSolution(token);
        }

        private async Task BuildSolution(CancellationToken token)
        {
            var msbuild = new MSBuildCommand(Context) {
                Exe = "msbuild",
                WorkingDirectory = Context.ContentDirectory,
            };

            var buildArgs = new MSBuildArguments {
                ProjectFile = "PiServerLite.sln",
                Properties = {
                    ["Configuration"] = "Release",
                    ["Platform"] = "Any CPU",
                },
                Verbosity = MSBuildVerbosityLevel.Minimal,
            };

            await msbuild.RunAsync(buildArgs, token);
        }
    }
}
