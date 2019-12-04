if($env:BUCKET_PROJECT_VERSION -eq $null)
{
	throw "Must run build.ps1 scripts first."
}

& "AdvancedInstaller.com" /edit "bucket-installer.aip" /SetVersion $env:BUCKET_PROJECT_VERSION
& "AdvancedInstaller.com" /build "bucket-installer.aip"