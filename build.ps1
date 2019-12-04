param(
[string]$version="master-dev"
)

$root = (Get-Item -Path ".\").FullName
$publishDir = $root + "\src\Bucket.CLI\bin\Release\netcoreapp3.0\publish"
$version = $version.Trim()
$versionParttern = "^v?(?<master>(?<major>\d{1,5})(?<minor>\.\d+)?(?<patch>\.\d+)?(?<revision>\.\d+)?|master)(?<stability>\-(?:stable|beta|b|RC|alpha|a|patch|pl|p|dev)(?:(?:[.-]?\d+)+)?)?(?<build>\+[0-9A-Za-z\-\.]+)?$"
if(!($version -match $versionParttern))
{
    throw ("Invalid version, must conform to the semver version: " + $version)
}

if($matches["stability"] -eq $null)
{
	$matches["stability"] = "-stable"
}

if($matches["master"] -eq "master")
{
	$matches["major"] = "0"
}

if($matches["minor"] -eq $null)
{
	$matches["minor"] = ".0"
}

if($matches["patch"] -eq $null)
{
	$matches["patch"] = ".0"
}

if($matches["build"] -match "^[0-9a-f]{40}$")
{
	$env:BUCKET_COMMIT_SHA = $matches["build"].substring(1).trim()
    $matches["build"] = $matches["build"].substring(0, 8)
}
elseif($env:CI_COMMIT_SHA -ne $null)
{
	$env:BUCKET_COMMIT_SHA = ($env:CI_COMMIT_SHA).trim()
	$matches["build"] = "+" + $env:BUCKET_COMMIT_SHA.substring(0, 7)
}
else
{
	$env:BUCKET_COMMIT_SHA = (git rev-parse HEAD).trim()
	$matches["build"] = "+" + $env:BUCKET_COMMIT_SHA.substring(0, 7)
}

$major = $matches["major"]
$minor = $matches["minor"]
$assemblyVersion = $major + ".0.0.0"
$versionNormalized = $major + $minor + $matches["patch"] + $matches["revision"] + $matches["stability"] + $matches["build"]

$beginDateTime = ([DateTime] "01/01/2000");
$midnightDateTime = (Get-Date -Hour 0 -Minute 0 -Second 0);

$elapseDay = -1 * (New-TimeSpan -end $beginDateTime).Days
$elapseMidnight = [math]::floor((New-TimeSpan $midnightDateTime -End (Get-Date)).TotalSeconds * 0.5)
$fileVersion = $major + $minor + "." + $elapseDay + "." + $elapseMidnight

if(Test-Path -Path $publishDir\.temp)
{
	Remove-Item $publishDir\.temp -Recurse
}
dotnet publish src\Bucket.CLI\Bucket.CLI.csproj -c Release /p:Version=$versionNormalized /p:AssemblyVersion=$assemblyVersion /p:FileVersion=$fileVersion --self-contained false

if($LastExitCode){
	echo "Abnormal. build failded."
	exit(1)
}

$env:BUCKET_PROJECT_VERSION=$versionNormalized.trim()
$env:BUCKET_FILE_VERSION=$fileVersion.trim()
$env:BUCKET_STABILITY=$matches["stability"].substring(1).trim()
$env:BUCKET_MIN_DOTNET_CORE="3.0.0".trim()
$env:BUCKET_PUBLISH_DIR=$publishDir.trim()

echo "BUCKET_COMMIT_SHA $env:BUCKET_COMMIT_SHA"
echo "BUCKET_FILE_VERSION $env:BUCKET_FILE_VERSION"
echo "BUCKET_PROJECT_VERSION $env:BUCKET_PROJECT_VERSION"
echo "BUCKET_STABILITY $env:BUCKET_STABILITY"
echo "BUCKET_MIN_DOTNET_CORE $env:BUCKET_MIN_DOTNET_CORE"
echo "BUCKET_PUBLISH_DIR $env:BUCKET_PUBLISH_DIR"