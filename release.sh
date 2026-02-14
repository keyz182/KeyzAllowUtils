#!/usr/bin/env bash

dotnet restore 1.6/Source/KeyzAllowUtilities.sln
dotnet build 1.6/Source/KeyzAllowUtilities.sln /p:Configuration=Release

'
FILES_TO_EXCLUDE contains a list of files and folders to exclude from the release zip file.
e.g.:
    .editorconfig
    .git
    1.6/Source
    Assets
    modlist.xml
    release.bat
'
FILES_TO_EXCLUDE=$(grep "<exclude>" _PublisherPlus.xml | sed 's/[[:space:]]*<exclude>//g' | sed 's:</exclude>::g')

# Create the exclude list for zip.
# We need to exclude both the directory itself and its contents.
# Using pattern* helps to match both 'dir' and 'dir/file'.
EXCLUDES=()
for item in $FILES_TO_EXCLUDE; do
    EXCLUDES+=("$item" "$item/*")
done

# Create the zip file
# Use -r for recursive
# Use -x to exclude files/folders
zip -r KeyzAllowUtils.zip . -x "${EXCLUDES[@]}"
