Info about old c# project netGrepWin

Version c# Visual Studio 15 

## programm to start 

For test runs but needs admi n rights
"C:\Program Files\NetGrepWin\NetGrepWin.exe"

## code files
netGrepWin lives in its own folder ..\..\..\netGrepWin

* common libraries in root folders: lib*
* programm libraries in  NetGrepWin subfolder folders: lib*
* programm real config and commandline definitions in NetGrepWin\Program
* the *god* object containing all object is ni NetGrepWin\Program\MainGlobal\clsMainGlobal.cs

## General features List

* Colored search results
* Multiple forms in application so you may keep similar searches side by side
* Replacing as well as searching
* Use of regex (C# style)
* 'Search again' Key
* Saving of search results
* Saving and retrieval of search criteria
* Command line interface 
* Saving and retrieval of search criteria
* Click on above file list results will open complete file with highlighted search results (No edit changes possible though)
* 

### Drawbacks (06.01.2021)

Not fixed yet: 
* Results are not asynchron displayed and the screen freezes while searching
* Due to pre c# asyn functions the result is sometimes displayed but not acceptet -> After popup Question the results vanish but can be retrieved without search again
* Needs administrator rights due to read of config files from program folder

#### Missing features of "Windows grep"

ideas for further development:
* Printing of search results
* Fully multi-tasking
* ZIP file searching
* Delimited and fixed width data file searching at field-level.
* File Explorer Extension right click

## layout

### Mdi form with search forms

* The exe starts with a search query fom filled out with the last search 
  query dataand opens a search with this data
* On top File, Search, ...  menues
* On bottom info about search action and some statictics (partly not coded)
* Title: Info about the running search (events), 
* Progress area  

* Commands /keys commands from MDI do search for a matching active search 
  result form and executes them there 

* each search query is saved in a list where the newest is on top
  Double entries are moved to the top or may be already there
* The search query data list and the search result files list are saved on 
  each change into a file. hese are read on start into memeory for display 

#### Menu File
* Handle search query data/forms 
* New: open new empty form 
* Open: select from last search queries and create form
* Close open forms: this/all/all but not this
* Search results: open/save/save as: Use files to save 'search input', 
  'file list' and 'token lines list' for each search query result.
  A save will add the file to a last files list
* Last used search queries: Shows a list of last opened serch query where 
  a click (should) open the search query as search form with search open 
* Last used search files: List last saved search results . A click should 
  open a filled out search form with results without open te search itself

#### Menu Search

* Search form with several options and separate inputs for search token and 
  files selection token. 
* Cancel search: Interrupt and stop actual search. Display actual found  
* Search in files: Search in already found files

#### Menu Options

* References (should be config): Left side config sections, 
  right side config section parameter
* Search string list: Handle last users search strings items 
* Search string list: Handle last users search file strings items 
* Enable to change the html classes with color picker and ...

#### Menu View


#### Menu Tools



#### Menu Windows

* 

### Search form

* The search string is saved in a lafo list which the user can select from
* The search files string is saved in a lafo list which the user can select from
* On no active search forma new one is generated on search start

### Search results form

* Search Form has 'header' line above with view options 
* Two display areas: Result files section, result token lines section 
* Display areas are html displays each with it own *.css designs and classes 
  for the elements shown 
* Each starts with it own header following by a list and divided by a grey 
  line

#### Result files section

* Blue header tells about the search string and the files search setting
* Followed by file list. 
* A click on the file selects that file and shows the token line 
  results beginning with this file in below window
  It should indicate the active state  
* A double click on the file opens a 'Show and search inside file" form

#### Result token lines section 

* Blue header tells 'token fund inside files'
* The options from the 'header' line tell which elements will be shown in the 
  result lines. For example pre and post lines tell how many lines shall be 
  added to the result

## config

* Separate config files for common user and actual user 
  If user is new use the prepared common one
*   
