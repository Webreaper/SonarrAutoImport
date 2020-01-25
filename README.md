# SonarrAutoImport
Scan video files and submit them to import into Sonarr, Drone-factory style.

## Why?
Sonarr's 'Quick Import' is supposed to auto-import files downloaded via other channels that the Download Clients configured in settings. However, it only works if you pass it the full path/folder to a single episode. 
I run get_iplayer as a BBC PVR, and want episodes to be auto-imported by Sonarr where possible, so they get moved into Plex without me having to run a Manual Import.
So this utility scans the folder for video files, and for each one found, calls the API to trigger an automatic import. It won't work for all files, since some may be misnamed, but it will automate some of them.

## Usage:
Usage has changed as of v1.1. The tool will look for a file Settings.json in the working folder or same folder as the executable. The Json file includes the sonarr and radarr config (either is optional) and the transform rules for Sonarr.

An example Settings file can be found [here](https://github.com/Webreaper/SonarrAutoImport/blob/master/Settings.json).

There are also some optional params:
* A 4th param specifying the mapped folder will replace the root folder with this remote folder. This replicates the remote folders setting in the Sonnar Download Client settings screen.
* -v Enables verbose logging
* -dry-run will scan the folder for video files, but not call the Sonarr API

## Example:

```SonarrAutoImport "/volume1/video/Downloads" 12345678901234567890123456 http://192.168.1.30:8989 /downloads/```

This will scan for all video files in /volume1/video/Downloads and for any it finds, trigger an import into Sonarr.

## Sonarr Episode Filename transforms

For each file that is scanned by the tool for import into Sonarr, prior to running the Sonarr import, all transforms will be run on the filename. Regex is supported. So for example, I have the following:

```
"search" : "Series (\\d+) - ",
"replace" : "S$1E"
```

Which will convert:

```Poldark Series 5 - 04.Episode 04.mp4```

to 

```Poldark S5E04.Episode 04.mp4```

which will then be correctly imported and scanned by Sonarr. Another example, showing how multiple transforms are applied in-order:

```
"search" : "Gardeners World 2019",
"replace" : "Gardeners World Series 52"
```
which, combined with the first tranfrom above, will convert:

```Gardeners World 2019 - 24.Episode 24.mp4```

to 

```Gardeners World Series 52 - 24.Episode 24.mp4```

to 

```Gardeners World S52E24.Episode 24.mp4```

This makes up for the irritating fact that Sonarr doesn't support user-defined series renames and relies on them being reported centrally and then a) accepted and b) updated in a timely fashion - neither of which are guaranteed.

