using Photon.Framework.Agent;
using Photon.Framework.Tasks;
using PiServerLite.Publishing.Internal;
using System.Threading;
using System.Threading.Tasks;

namespace PiServerLite.Publishing
{
    public class Build_Win : IBuildTask
    {
        public IAgentBuildContext Context {get; set;}

        
        public async Task RunAsync(CancellationToken token)
        {
            await BuildSolution();
        }

        private async Task BuildSolution()
        {
            var msbuild_exe = Context.AgentVariables["global"]["msbuild_exe"];

            var msBuild = new MsBuild(Context) {
                Exe = $"\"{msbuild_exe}\"",
                Filename = "PiServerLite.sln",
                Configuration = "Release",
                Platform = "Any CPU",
                Parallel = true,
            };

            await msBuild.BuildAsync();
        }
    }
}
