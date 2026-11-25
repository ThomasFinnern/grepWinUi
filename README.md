# grepWinUi
In ancient times existed **"Windows Grep"** by Huw Millington which i used a lot. 
The program was a tool for searching files for text strings with a marvelous way 
to display the search results in a colored HTML view for Windows XP. 

Then it was not supported any more and i started with my own version. This is a 
redesign from start with the help of AI

## Features

- **Main window:** starts an empty window titled "grepWinUi" plus version. (Menus and search follow.)

## Build and run

Needs the free .NET SDK 10 or newer (no Visual Studio needed). From the repository folder:

```
dotnet build GrepWinUi.slnx
dotnet test GrepWinUi.slnx
dotnet run --project src/GrepWinUi.App
``` 







