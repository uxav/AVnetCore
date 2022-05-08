param([string] $Path)

$file = Get-Item $Path;
$fileContent = Get-Content $file.FullName
$version = [Regex]::Match($fileContent, '(?<!// *)\[assembly: AssemblyFileVersion\("(\d+\.\d+\.\d+).*"\)\]').Captures.Groups[1].Value
Write-Output $version
