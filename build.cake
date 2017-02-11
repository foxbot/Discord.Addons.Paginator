#addin nuget:?package=NuGet.Core&version=2.12.0
#addin "Cake.ExtendedNuGet"

var MyGetKey = EnvironmentVariable("MYGET_KEY");
string BuildNumber = EnvironmentVariable("TRAVIS_BUILD_NUMBER");
string Branch = EnvironmentVariable("TRAVIS_BRANCH");
ReleaseNotes Notes = null;

Task("Restore")
    .Does(() =>
{
    var settings = new DotNetCoreRestoreSettings
    {
        Sources = new[] { "https://www.myget.org/F/discord-net/api/v2", "https://www.nuget.org/api/v2" }
    };
    DotNetCoreRestore(settings);
});
Task("ReleaseNotes")
    .Does(() =>
{
    Notes = ParseReleaseNotes("./ReleaseNotes.md");
    Information("Release Version: {0}", Notes.Version);
});
Task("Build")
    .IsDependentOn("ReleaseNotes")
    .Does(() =>
{
    var suffix = BuildNumber != null ? BuildNumber.PadLeft(5,'0') : "";
    var settings = new DotNetCorePackSettings
    {
        Configuration = "Release",
        OutputDirectory = "./artifacts/",
        EnvironmentVariables = new Dictionary<string, string> {
            { "BuildVersion", Notes.Version.ToString() },
            { "BuildNumber", suffix },
            { "ReleaseNotes", string.Join("\n", Notes.Notes) },
        },
    };
    DotNetCorePack("./src/Discord.Addons.Paginator/", settings);
    DotNetCoreBuild("./src/Example/");
});
Task("Deploy")
    .WithCriteria(Branch == "master")
    .Does(() =>
{
    var settings = new NuGetPushSettings
    {
        Source = "https://www.myget.org/F/discord-net/api/v2/package",
        ApiKey = MyGetKey
    };
    var packages = GetFiles("./artifacts/*.nupkg");
    NuGetPush(packages, settings);
});

Task("Default")
    .IsDependentOn("Restore")
    .IsDependentOn("Build")
    .IsDependentOn("Deploy")
    .Does(() => 
{
    Information("Build Succeeded");
});

RunTarget("Default");