using Photon.Framework.Agent;
using Photon.Framework.Tasks;
using Photon.Framework.Tools;
using Photon.NuGetPlugin;
using PiServerLite.Publishing.Internal;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace PiServerLite.Publishing
{
    public class Publish_Windows : IBuildTask
    {
        public IAgentBuildContext Context {get; set;}

        
        public async Task RunAsync(CancellationToken token)
        {
            await BuildSolution();
            await PublishProjectPackage(token);
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

        private async Task PublishProjectPackage(CancellationToken token)
        {
            var projectPath = Path.Combine(Context.ContentDirectory, "PiServerLite");
            var packageDefinition = Path.Combine(projectPath, "PiServerLite.csproj");
            var assemblyFilename = Path.Combine(projectPath, "bin", "Release", "PiServerLite.dll");
            var assemblyVersion = AssemblyTools.GetVersion(assemblyFilename);

            await PublishPackage("PiServerLite", packageDefinition, assemblyVersion, token);
        }

        private async Task PublishPackage(string packageId, string packageDefinitionFilename, string assemblyVersion, CancellationToken token)
        {
            var nugetPackageDir = Path.Combine(Context.WorkDirectory, "Packages");
            var nugetApiKey = Context.ServerVariables["global"]["nuget.apiKey"];

            var publisher = new NuGetPackagePublisher(Context) {
                Mode = NugetModes.Hybrid,
                PackageDirectory = nugetPackageDir,
                PackageDefinition = packageDefinitionFilename,
                PackageId = packageId,
                Version = assemblyVersion,
                CL = new NuGetCommandLine {
                    ExeFilename = "bin\\NuGet.exe",
                    Output = Context.Output,
                },
                Client = new NuGetCore {
                    ApiKey = nugetApiKey,
                    Output = Context.Output,
                },
                PackProperties = {
                    ["Configuration"] = "Release",
                    ["Platform"] = "AnyCPU",
                },
            };

            publisher.Client.Initialize();

            await publisher.PublishAsync(token);
        }
    }
}
