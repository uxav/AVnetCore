param ($path, $targetName)

#####################################################
# Setup your connection details as below
$deviceAddress = "mc4"
$programSlot = 1
$user = "admin"
$password = "password"
#####################################################


#####################################################
# Setup the file name for the program / library
# Use a cpz if you need to fully reload the program or use a dll to just upload the built project target
#####################################################
#FILENAME
$fileName = "$targetName.dll" # Or use cpz!
#####################################################


###########################################
# DO NOT CHANGE ANYTHING BELOW THIS LINE! #
###########################################

# File paths
$localFilePath = "$path\$fileName"
$remoteFilePath = '\program{0:d2}' -f $programSlot
$remoteFilePath = "$remoteFilePath\$fileName"

# You need the Crestron Powershell EDK installed
Import-Module PSCrestron

# Upload the file
Write-Host "Uploading $localFilePath to $remoteFilePath"
Send-FTPFile -Device $deviceAddress -LocalFile $localFilePath -RemoteFile $remoteFilePath -Username $user -Password $password -Secure

# Get file extension
$extension = [System.IO.Path]::GetExtension($localFilePath)

if ($extension -eq ".cpz") {
    #Send progload for cpz
    Write-Host "Loaded cpz file to processor. Will now send progload to slot ${programSlot}..."
    $cmd = 'progload -P:{0:d2}' -f $programSlot
    Write-Host "Sending command: ${cmd}"
    Invoke-CrestronCommand -Device $deviceAddress -Command $cmd -Username $user -Password $password -Secure
}
elseif ($extension -eq ".dll") {
    # Send progres for dll
    Write-Host "Loaded dll file to processor. Will now send program restart to slot ${programSlot}..."
    $cmd = 'progres -P:{0:d2}' -f $programSlot
    Write-Host "Sending command: ${cmd}"
    Invoke-CrestronCommand -Device $deviceAddress -Command $cmd -Username $user -Password $password -Secure
}