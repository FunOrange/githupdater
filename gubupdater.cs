
// read file current_version.txt
// contents should look something like:
//
// github repository name: osu-trainer
// application name: osu-trainer.exe
// current version: 1.4
// <EOF>

// Make API request to github
// Copy code from osu-trainer\Updater.cs

// Download zip file
// Delete updater.exe from zip file
// extract zip file into same directory as this .exe

Process.Start(applicationName);
