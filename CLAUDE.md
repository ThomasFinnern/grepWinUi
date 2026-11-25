# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

# Code restart for a grep UI project grepWinUi with search forms 

This project will reimplement the functions in former project netGrepWin from scratch. 

## Repository state
- Solution `GrepWinUi.slnx` with `src/GrepWinUi.Core`, `src/GrepWinUi.App`, `tests/GrepWinUi.Core.Tests`
  (see Architecture). Commands, run from the repo root:
  - Build: `dotnet build GrepWinUi.slnx`
  - Test: `dotnet test GrepWinUi.slnx`
  - Run: `dotnet run --project src/GrepWinUi.App`
- `doc/netGrepWin/` holds the description of the old program (`netGrepWin.code.md`)
  and UI screenshots (`*Menu.png`, `searchForm*.png`, `MdiFormWithResultsForm.png`).
  This is the main spec for the reimplementation.
- The old C# source (Visual Studio 2015) is in the sibling folder `..\netGrepWin`.
  Its central "god" object is `NetGrepWin\Program\MainGlobal\clsMainGlobal.cs`;
  config and command line definitions are in `NetGrepWin\Program`.
- Target design (from the old program): an MDI main window holding several search forms.
  Each search form shows HTML/CSS-styled results in two panes (file list and matching lines).
  Search queries and saved result files are kept in most-recent-first lists that are saved to disk.
  Config should be per user and must not need admin rights.

## Technology (decided 2026-11-25)
- **C# with WinForms on .NET 10 (LTS)**: free SDK, no Visual Studio license needed.
- Edit and debug in **VS Code** with the "C#" extension (`dotnet build`, `dotnet run`, F5).
- There is no WinForms designer in VS Code, so build forms in code
  (`Dock`, `TableLayoutPanel`, …) and don't use `*.Designer.cs` files.
- Use **WebView2** for the HTML/CSS result panes. The old project's `WebBrowser` control is based on IE and outdated.
- Run searches with `async/await` so the UI never freezes (a known problem of the old program).
- Store config in the user profile (`%AppData%`), never in the program folder, so no admin rights are needed.
- The new project does not reference the old project's code (.NET Framework 2.0 / 3.5 / 4.5);
  copy its logic over instead (search, regex, query lists).
  Development of the old project is finished: read its code, never change or run it.
- Target framework `net10.0-windows`. Any SDK ≥ 10 can build it; the dev PC has SDK 11.0.100
  plus the 10.0 and 11.0 WindowsDesktop runtimes. Also needed: the VS Code "C#" extension.

## Architecture (planned 2026-11-25)
```
GrepWinUi.slnx
src/GrepWinUi.Core/          class library (net10.0, no UI): search, query model,
                             recent-first lists, config, result files, HTML building
src/GrepWinUi.App/           WinForms (net10.0-windows): MDI window, forms, WebView2
tests/GrepWinUi.Core.Tests/  xUnit tests for Core
```
- All logic lives in Core so it can be tested; App only wires up forms.
- Tests: **xUnit**.
- Config: JSON (`System.Text.Json`) in `%AppData%\GrepWinUi\`. A shared default file next to
  the exe is copied for a new user. No group configs for now.
- Result files: own new JSON format; no reading of old `*.Res` files (import maybe later).
- UI: classic MDI child windows, as in the old program.
- Before each feature step: read the matching code in `..\netGrepWin` (read only)
  and add short notes to `doc/netGrepWin/`.

## Conventions
- Explicit types on public members, short XML doc comments (`///`).
- Keep functions small and pure where possible; no global state.
- Update README.md with each new feature, in the same step: a short user-facing description
  (what it does, where to find it: menu entry, key or command line option) under a "Features" section.
- Menu items without a working function get a `*` in front of their text (e.g. `*Open...`).
  Remove the `*` in the step that makes the function work.

## How to work with me
- I build this in small steps. Do ONE step at a time, then stop.
- Before changing multiple files or the structure, explain the plan briefly and wait.
- Don't add features I didn't ask for; list ideas at the end instead.
- Add or update a test for each new behavior.

## planning:
My ideas but roadmap may be different and better 
1. First you need to scan for a function overview of the old project save it  
1. Then general planning with update of this document, this section and roadmap
1. Second we want to check which code language it suits best and how i can compile 
   it without Visual Studio license. if needed i may find an old C# 2010 version
1. Then we want to start with the outer form just all the menu items without functionality
1. Then we start with configuration for all user/?group active
1. Naked search form with search input and start form with elements. 
1. Save / load search input lists in several positions

Then further planning 

## old netGrepWin exe doc files

See file doc\netGrepWin\netGrepWin.code.md and the other files in folder doc\netGrepWin

### Issues

* State: Most functions finished, not all features are programmed
* Config code tries to write into windows admin restricted area and 
  therefore needs admin rights to start


## Tasks

* Grasp functionality of old project and save to file there for further read. Update this file.
* 
* 

## Running the old program for tests

- When coded: A rerun of the exe starts with the last search window(s) open again (`ReopenFiles.txt`,
  `SearchResult.Res` in `%AppData%\NetGrepWin\Config`; the result file can be large, so the main window
  may take 10+ s to appear).
- Before testing menus: use **Exit** on that reopened search form. Then one form, the last search result
  form, is active in the MDI window, and all menus and functions can be used.


## Roadmap
One step = one session. Steps 1–5 are done.
1. [x] Choose language/toolchain: C# WinForms on .NET 10, VS Code (see Technology)
1. [x] Install .NET SDK (11.0.100 installed)
1. [x] Install VS Code "C#" extension
1. [x] Planning the reimplementation (see Architecture)
1. [x] Skeleton: solution, Core/App/Tests projects, empty window that starts, `.gitignore`;
       add build/run/test commands to this file
1. [ ] Main MDI window: all menus from the screenshots, status bar, No "Ctrl Shift F6" toolbar;
       no functions
1. [ ] Config: settings model, JSON in `%AppData%\GrepWinUi\`, shared defaults copied for a new user
1. [ ] Recent-first list: newest on top, duplicates move to top, size limit, saved to disk (tested)
1. [ ] Search query model: search string, file types, folders, all options; list of past queries
1. [ ] Search properties form: all inputs from `searchForm.png`, dropdowns filled from the lists; no search
1. [ ] Search engine (Core): file enumeration (wildcard/regex, subfolders), line matching
       (regex, whole word, match case, lines before/after); async, cancellable, progress
1. [ ] Result form: header line with view options, two WebView2 panes; HTML/CSS built in Core
1. [ ] Connect search to results: start/cancel, progress and statistics in the status bar
1. [ ] Result clicks: click on a file jumps to its lines; double click opens the "show file" form
1. [ ] Save/open search results and the recent result files list (File menu)
1. [ ] Reopen the last forms on start
1. [ ] Replace
1. [ ] Search in found files, "search again" key
1. [ ] Options dialog: config editor, CSS colors
1. [ ] Command line

Ideas for later (not planned): ZIP search, printing, Explorer right-click entry,
fixed-width/delimited field search, import of old `*.Res` files, group configs.


## Current step
Step 6: Main MDI window (menus, status bar, toolbar; no functions).
