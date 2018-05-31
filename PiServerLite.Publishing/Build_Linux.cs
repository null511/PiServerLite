using Photon.Framework.Agent;
using Photon.Framework.Tasks;
using PiServerLite.Publishing.Internal;
using System.Threading;
using System.Threading.Tasks;

namespace PiServerLite.Publishing
{
    public class Build_Linux : IBuildTask
    {
        public IAgentBuildContext Context {get; set;}

        
        public async Task RunAsync(CancellationToken token)
        {
            await BuildSolution();
        }

        private async Task BuildSolution()
        {
            var msBuild = new MsBuild(Context) {
                Exe = "msbuild",
                Filename = "PiServerLite.sln",
                Configuration = "Release",
                Platform = "Any CPU",
            };

            await msBuild.BuildAsync();
        }
    }
}
