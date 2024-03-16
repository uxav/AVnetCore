#!/bin/bash                                                                                                         

# This script is used to upload the app to a processor
# echo "Upload script: $@"

archive="$2$3.dll"

# comment this out to continue below
exit 0;

if [ $1 == "net472" ]; then

sshHost='mc4'
sshUser='mike'
# Set the program slot here
programSlot=1

dir=$(echo 0${programSlot} | tail -c 3)
dir=$(echo program${dir})

#echo "Uploading $target to $sshHost"
echo "Uploading $archive to $sshHost"
sftp ${sshUser}@${sshHost}:/${dir} <<EOF
put $archive
EOF

# Open SSH connection to processor and restart the program
ssh -t -t ${sshUser}@${sshHost} << EOF
progres -P:${programSlot}
bye
EOF

fi