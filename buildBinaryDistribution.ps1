$version = $args[0]
echo "Version: $version"

# Recreate "ZipPackage"
Remove-Item -Recurse -Force "ZipPackage" -ErrorAction Ignore
Remove-Item -Recurse -Force "mmb.$version" -ErrorAction Ignore

New-Item -ItemType directory -Path "ZipPackage"

# Copy-Item -Path files
Copy-Item -Path "MUDMapBuilder.Console\bin\Release\net6.0\*" -Destination "ZipPackage" -Recurse -Force
Copy-Item -Path "MUDMapBuilder.BatchConverter\bin\Release\net6.0\*" -Destination "ZipPackage" -Recurse -Force

# Compress
Rename-Item "ZipPackage" "mmb.$version"
Compress-Archive -Path "mmb.$version" -DestinationPath "mmb.$version.zip" -Force

# Delete the folder
Remove-Item -Recurse -Force "mmb.$version"