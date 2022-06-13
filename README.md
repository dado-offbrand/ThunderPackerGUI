# ThunderPackerGUI

This program packages mods for Thunderstore.io. The process to create a mod is rather simple:
 - Use the select file buttons to select and find your ModNameHere.dll file and your icon.png file.
 - Fill out some fields for the manifest (name, version, and desc are required)
 - For dependencies, no need to add quotes or commas. Just leave raw text.
 - Use the README Editor to either create a readme, or under 'Additions', check 'Exclude Readme' to skip this step
 - Click pack, and wait a few.

# Changelog
 - Added autofill, I would recommend keeping it on
 - Changed how directories are created
 - Added auto-zipping for the file (following the ThunderStore name format for downloaded mods)
 - Reorganized code to sequence actions better
 - Removed automatically opening the directory (temporarily)
 - Forced usage of .png instead of .png/.jpg

# Upcoming features
 - File size reduction, this program is reletively large
 - Proper support for auto-opening the directory to the mod folder
