using Photon.DotNetPlugin;
using Photon.Framework.Agent;
using Photon.Framework.Extensions;
using Photon.Framework.Packages;
using Photon.Framework.Tasks;
using Photon.Framework.Tools;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace PiServerLite.Publishing.Scripts
{
    public class Package : IBuildTask
    {
        private DotNetCommand dotnet;

        public IAgentBuildContext Context {get; set;}

        
        public async Task RunAsync(CancellationToken token)
        {
            await BuildSolution(token);
        }

        private async Task BuildSolution(CancellationToken token)
        {
            dotnet = new DotNetCommand(Context) {
                WorkingDirectory = Context.ContentDirectory,
            };

            await BuildSolutionAsync(token);
            await UnitTestAsync(token);

            var assemblyFile = Path.Combine(Context.ContentDirectory, "PiServerLite", "bin", "Release", "net45", "PiServerLite.dll");
            var assemblyVersion = AssemblyTools.GetVersion(assemblyFile);
            var projectPackageVersion = $"{Context.BuildNumber}.{assemblyVersion}";

            await CreateNugetPackageAsync(token);
            await CreateProjectPackageAsync(projectPackageVersion, token);
        }

        private Task BuildSolutionAsync(CancellationToken token)
        {
            return dotnet.BuildAsync(new DotNetBuildArguments {
                ProjectFile = "PiServerLite.sln",
                Configuration = "Release",
                NoIncremental = true,
            }, token);
        }

        private Task UnitTestAsync(CancellationToken token)
        {
            return dotnet.TestAsync(new DotNetTestArguments {
                ProjectFile = "PiServerLite.Tests\\PiServerLite.Tests.csproj",
                Configuration = "Release",
                NoBuild = true,
                //Filter = "Category=unit",
            }, token);
        }

        private Task CreateNugetPackageAsync(CancellationToken token)
        {
            var packageDir = Path.Combine(Context.ContentDirectory, "PiServerLite.Publishing", "bin", "Package");

            return dotnet.PackAsync(new DotNetPackArguments {
                ProjectFile = "PiServerLite\\PiServerLite.csproj",
                Configuration = "Release",
                OutputDirectory = packageDir,
                NoBuild = true,
            }, token);
        }

        private async Task CreateProjectPackageAsync(string version, CancellationToken token)
        {
            var projectPath = Path.Combine(Context.ContentDirectory, "PiServerLite.Publishing");
            var packageDefFile = Path.Combine(projectPath, "PiServerLite.Publishing.json");
            var output = Path.Combine(Context.ContentDirectory, "PublishPackage", "PiServerLite.zip");

            try {
                Context.WriteTagLine("Creating project package...", ConsoleColor.White);

                var packageDef = ProjectPackageTools.LoadDefinition(packageDefFile);

                await ProjectPackageTools.CreatePackage(
                    definition: packageDef,
                    rootPath: projectPath,
                    version: version,
                    outputFilename: output);

                Context.WriteTagLine("Created project package successfully.", ConsoleColor.White);
            }
            catch (Exception error) {
                Context.WriteErrorBlock("Failed to create project package!", error.UnfoldMessages());
                throw;
            }

            try {
                Context.WriteTagLine("Publishing project package...", ConsoleColor.White);

                await Context.Packages.PushProjectPackageAsync(output, token);

                Context.WriteTagLine("Published project package successfully.", ConsoleColor.White);
            }
            catch (Exception error) {
                Context.WriteErrorBlock("Failed to publish project package!", error.UnfoldMessages());
                throw;
            }
        }
    }
}
