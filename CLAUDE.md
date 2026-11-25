# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

# Code restart for a grep UI project with search forms 

This project will reimplement the functions in former project netGrepWin from scratch. 

## Repository state
- No source code yet: `src/` is empty, and there are no build, lint or test commands.
  Add them here once the language and toolchain are chosen (see planning).
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

## Conventions
- Type hints on all functions, short docstrings.
- Keep functions small and pure where possible; no global state.
- Update README.md with each new functionality

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


## Roadmap
1. [ ] Question: can i compile the old project ?  
1. [ ] If yes: can we change the configuration files origins so that the program starts 
       without administrator rights
1. [ ] Planning 
1. [ ] Plan extraction of netgrepwin exe program information
1. [ ] Extract netgrepwin exe program information into a file which will be read on Claude start
1. [ ] Defining tools
1. [ ] Planning 
1. [ ] 
1. [ ]  
1. [ ]  
1. [ ]  
1. [ ]  
1. [ ]  
1. [ ]  
1. [ ]  
1. [ ]  


## Current step
Step 1.
