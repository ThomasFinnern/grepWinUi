# grepWinUi
In ancient times existed **"Windows Grep"** by Huw Millington which i used a lot. 
The program was a tool for searching files for text strings with a marvelous way 
to display the search results in a colored HTML view for Windows XP. 

Then it was not supported any more and i started with my own version. This is a 
redesign from start with the help of AI

## Features

- **Main window:** an MDI window titled "grepWinUi" plus version, which will hold the search forms.
- **Menu bar:** File, Search, View, Options, Tools, Windows, Help (Help on the right).
  Items marked with `*` have no function yet.
- **Status bar:** Title, Info, Working (progress bar), Folder, Files, Time. Shows dummy texts for now.
- **Settings:** stored per user in `%AppData%\GrepWinUi\Config.json` (no admin rights needed).
  Until you change a setting, the standard values from `Defaults\Config.json` next to the exe are used.
- **File → Exit:** closes the program.
- **Options → Settings:** edit the settings on three pages (Search result view, While searching,
  On start of program). **Save** writes `Config.json`, **Cancel** discards all changes.
  New result forms start with a copy of these settings.

## Build and run

Needs the free .NET SDK 10 or newer (no Visual Studio needed). From the repository folder:

```
dotnet build GrepWinUi.slnx
dotnet test GrepWinUi.slnx
dotnet run --project src/GrepWinUi.App
```

Or use the batch files in `build\`: `build_debug.bat`, `build_release.bat`, plus `_rebuild`, `_clean`
and `_detailed` variants. All of them call `build.bat [Debug|Release] [extra args]`. 







