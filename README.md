# SonarrAutoImport
Scan video files and submit them to import into Sonarr & Radarr, Drone-factory style.

## Why?
Sonarr's 'Quick Import' is supposed to auto-import files downloaded via other channels that the Download Clients configured in settings. However, it only works if you pass it the full path/folder to a single episode. 
I run get_iplayer as a BBC PVR, and want episodes to be auto-imported by Sonarr where possible, so they get moved into Plex without me having to run a Manual Import.
So this utility scans the folder for video files, and for each one found, calls the API to trigger an automatic import. It won't work for all files, since some may be misnamed, but it will automate some of them.

## Usage:
Usage has changed as of v1.1. The tool will look for a file Settings.json in the working folder or same folder as the executable. The Json file includes the sonarr and radarr config (either is optional) and the transform rules for Sonarr.

An example Settings file can be found [here](https://github.com/Webreaper/SonarrAutoImport/blob/master/Settings.json).

There are also some optional params:
* -v Enables verbose logging (--v for windows users)
* -dry-run will scan the folder for video files, but not call the Sonarr API (--dry-run for windows users)

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

### **downloadsFolder vs. mappingPath**
- **downloadsFolder**: This is the path to the media files that need to be processed from the perspective of the SonarrAutoImport script.
- **mappingPath**: This is the path to the media files from the perspective of Sonarr.

These two paths should point to the same location. However, in some environments (such as Docker), the same destination might require different paths. Make sure both paths are correctly configured based on the context of your setup.
