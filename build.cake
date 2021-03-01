#addin "nuget:?package=System.Runtime.InteropServices.RuntimeInformation&version=4.3.0"

using System.Runtime.InteropServices;

var target = Argument("target", "Compress");
var configuration = Argument("configuration", "Release");
var framework = Argument("framework", "netcoreapp3.1");
var compileAll = Argument("compileAll", false);
var compressAll = Argument("compressAll", false);

var solutionFolder = "./";
var outputFolder = "./Builds";
var currentRID = RuntimeInformation.RuntimeIdentifier;

string[] runtimes = new string[]
{
    "win-x64",
    "win-x86",
    "osx-x64",
    "linux-x64"
};

Task("Clean")
    .Does(()=>{
        CleanDirectory(outputFolder);
    });

Task("Publish")
    .IsDependentOn("Clean")
    .Does(()=>{
        string outFolder = $"{outputFolder}/{currentRID}";
        var publishConfiguration = new DotNetCorePublishSettings(){
            Configuration = configuration,
            OutputDirectory = outFolder,
            // NoRestore = true,
            // NoBuild = true,
            SelfContained = true,
            PublishTrimmed = true,
            Runtime= currentRID,
            Framework = framework,
        };

        if (!compileAll)
        {
            DotNetCorePublish(solutionFolder, publishConfiguration);
            return;
        }

        foreach (var runtime in runtimes)
        {
            outFolder = $"{outputFolder}/{runtime}";
            publishConfiguration.OutputDirectory = outFolder;
            publishConfiguration.Runtime = runtime;

            DotNetCorePublish(solutionFolder, publishConfiguration);
        }
    });

Task("Compress")
    .IsDependentOn("Publish")
    .Does(()=>{
        if (!compressAll)
            return;

        if (!compileAll)
        {
            string inputFolder = $"{outputFolder}/{currentRID}";
            string name = $"Organizer {currentRID.ToUpper()}";
            
            CompressBin(inputFolder, name);
            return;
        }
        System.Threading.Tasks.Task[] tasks = new System.Threading.Tasks.Task[runtimes.Length];
        for (int i = 0; i < runtimes.Length; i++)
        {
            string inputFolder = $"{outputFolder}/{runtimes[i].ToUpper()}";
            string name = $"Organizer {runtimes[i].ToUpper()}";

            tasks[i] = System.Threading.Tasks.Task.Run(()=>CompressBin(inputFolder, name));
        }

        System.Threading.Tasks.Task.WaitAll(tasks);
    });

public void CompressBin(string inputFolder, string finalName){
    Information($"Compressing: {finalName}");
    Zip(inputFolder, $"{outputFolder}/{finalName}");
    Information($"\tFinished Compressing: {finalName}");
}

// ALL (NT): 274MB
// WIN10-x64 (NT): 67.5MB

//ALL: 151MB
//WIN10-X64: 35.6MB

RunTarget(target);