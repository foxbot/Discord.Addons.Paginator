#addin nuget:?package=NuGet.Core&version=2.12.0
#addin "Cake.ExtendedNuGet"

var MyGetKey = EnvironmentVariable("MYGET_KEY");
//string BuildNumber = EnvironmentVariable("TRAVIS_BUILD_NUMBER");
string BuildNumber = "5";

Task("Restore")
    .Does(() =>
{
    var settings = new DotNetCoreRestoreSettings
    {
        Sources = new[] { "https://www.myget.org/F/discord-net/api/v2", "https://www.nuget.org/api/v2" }
    };
    DotNetCoreRestore(settings);
});
Task("Build")
    .Does(() =>
{
    var suffix = BuildNumber.PadLeft(5,'0');
    var settings = new DotNetCorePackSettings
    {
        Configuration = "Release",
        OutputDirectory = "./artifacts/",
        VersionSuffix = suffix
    };
    DotNetCorePack("./src/Discord.Addons.Paginator/", settings);
    DotNetCoreBuild("./src/Example/");
});
Task("Deploy")
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