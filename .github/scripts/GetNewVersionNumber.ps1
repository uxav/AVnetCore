param([string] $Version, [string] $Type)

if (![Regex]::Match($Version, '^\d+\.\d+\.\d+').Success) {
    Write-Output("Version should be formatted x.x.x");
    exit(1);
}

if (![Regex]::Match($Type, '^major|minor|patch').Success) {
    Write-Output("Type should be major/minor/patch");
    exit(1);
}

$r = [Regex]::Match($Version, '(\d+)\.(\d+)\.(\d+)');

$major = [int]$r.Groups[1].Value;
$minor = [int]$r.Groups[2].Value;
$patch = [int]$r.Groups[3].Value;

switch($Type)
{
    major {
        $major = $major + 1;
        $minor = 0
        $patch = 0;
    }
    minor {
        $minor = $minor + 1;
        $patch = 0;
    }
    patch {
        $patch = $patch + 1;
    }
}

$result = "{0}.{1}.{2}" -f $major, $minor, $patch

Write-Output $result;