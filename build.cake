var target = Argument("target", "Compress");
var configuration = Argument("configuration", "Release");
var framework = Argument("framework", "netcoreapp3.1");

var solutionFolder = "./";
var outputFolder = "./Builds";

string[] runtimes = new string[]
{
    "win-x64",
    "win-x86",
    "osx-x64"
};

Task("Clean")
    .Does(()=>{
        CleanDirectory(outputFolder);
    });

Task("Publish")
    .IsDependentOn("Clean")
    .Does(()=>{
        foreach (var runtime in runtimes)
        {
            string outFolder = $"{outputFolder}/{runtime}";
            DotNetCorePublish(solutionFolder, new DotNetCorePublishSettings(){
                Configuration = configuration,
                OutputDirectory = outFolder,
                // NoRestore = true,
                // NoBuild = true,
                SelfContained = true,
                Runtime= runtime,
                Framework = framework
            });
        }
    });

Task("Compress")
    .IsDependentOn("Publish")
    .Does(()=>{
        System.Threading.Tasks.Task[] tasks = new System.Threading.Tasks.Task[runtimes.Length];
        for (int i = 0; i < runtimes.Length; i++)
        {
            string inputFolder = $"{outputFolder}/{runtimes[i]}";
            int iCopy = i;
            tasks[i] = System.Threading.Tasks.Task.Run(()=>{
                string name = $"Organizer {runtimes[iCopy]}";
                Information($"Compressing: {name}");
                Zip(inputFolder, $"{outputFolder}/{name}");
                Information($"\tFinished Compressing: {name}");
            });
        }

        System.Threading.Tasks.Task.WaitAll(tasks);
    });

RunTarget(target);