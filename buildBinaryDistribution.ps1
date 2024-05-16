$version = $args[0]
echo "Version: $version"

# Recreate "ZipPackage"
Remove-Item -Recurse -Force "ZipPackage" -ErrorAction Ignore
Remove-Item -Recurse -Force "MUDMapBuilder.$version" -ErrorAction Ignore

New-Item -ItemType directory -Path "ZipPackage"
New-Item -ItemType directory -Path "ZipPackage\ROM_Areas"

# Copy-Item -Path files
Copy-Item -Path "MUDMapBuilder.Console\bin\Release\net6.0\*" -Destination "ZipPackage" -Recurse
Copy-Item -Path "ROM_Areas\*" -Destination "ZipPackage\ROM_Areas" -Recurse

# Compress
Rename-Item "ZipPackage" "MUDMapBuilder.$version"
Compress-Archive -Path "MUDMapBuilder.$version" -DestinationPath "MUDMapBuilder.$version.zip" -Force

# Delete the folder
Remove-Item -Recurse -Force "MUDMapBuilder.$version"