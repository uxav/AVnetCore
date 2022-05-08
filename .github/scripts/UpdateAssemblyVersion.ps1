param([string] $Path, [string] $Version)

$file = Get-Item $Path;
Write-Output "File to open is $file"
Write-Output "Specific version is $Version"
if(!$Version)
{
    Write-Output("Version is not set, exiting!")
    exit(1);
}

$r = [Regex]::Match($Version, '(\d+)\.(\d+)\.(\d+)');

$major = [int]$r.Groups[1].Value;
$minor = [int]$r.Groups[2].Value;

$newAssemblyVersion = 'AssemblyVersion("' + $major + '.' + $minor + '.*")'
Write-Output "AssemblyVersion = $NewAssemblyVersion"
$newAssemblyFileVersion = 'AssemblyFileVersion("' + $Version + '")'
Write-Output "AssemblyFileVersion = $newAssemblyFileVersion"
$newAssemblyInformationalVersion = 'AssemblyInformationalVersion("' + $Version + '")'
Write-Output "AssemblyInformationalVersion = $newAssemblyInformationalVersion"

$TmpFile = $file.FullName + ".tmp"
Get-Content $file.FullName |
        ForEach-Object {
            $_ -replace 'AssemblyVersion\(".*"\)', $newAssemblyVersion } |
        ForEach-Object {
            $_ -replace 'AssemblyFileVersion\(".*"\)', $newAssemblyFileVersion } |
        ForEach-Object {
            $_ -replace 'AssemblyInformationalVersion\(".*"\)', $newAssemblyInformationalVersion
        }  > $TmpFile
move-item $TmpFile $file.FullName -force
