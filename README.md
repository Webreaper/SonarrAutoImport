# SonarrAutoImport
Scan video files and submit them to import into Sonarr, Drone-factory style.

## Why?
Sonarr's 'Quick Import' is supposed to auto-import files downloaded via other channels that the Download Clients configured in settings. However, it only works if you pass it the full path/folder to a single episode. 
I run get_iplayer as a BBC PVR, and want episodes to be auto-imported by Sonarr where possible, so they get moved into Plex without me having to run a Manual Import.
So this utility scans the folder for video files, and for each one found, calls the API to trigger an automatic import. It won't work for all files, since some may be misnamed, but it will automate some of them.

## Usage:
The tool takes 3 required parameters:
* Source directory to scan for video files
* Sonarr API key
* Sonarr base URL (/api will be added)

There are also some optional params:
* A 4th param specifying the mapped folder will replace the root folder with this remote folder. This replicates the remote folders setting in the Sonnar Download Client settings screen.
* -v Enables verbose logging
* -dry-run will scan the folder for video files, but not call the Sonarr API

## Example:

```SonarrAutoImport "/volume1/video/Downloads" 12345678901234567890123456 http://192.168.1.30:8989 /downloads/```

This will scan for all video files in /volume1/video/Downloads and for any it finds, trigger an import into Sonarr.
