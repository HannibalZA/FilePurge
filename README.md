# File Purge Utility

Nice little command line utility for handling recursive/mass file deletes


## Usage
### Interactive mode

1. Set base search directory
2. Execute purge command/options
   
Purge command options:
filematch [foldermatch] [ignorematch] [olderThan] [newerThan]

Examples:

*Just delete all png files*

`*.png`

*Delete all png files in directories ending in 'to_delete'*

`*.png *to_delete`

*Delete png files in a directories ending in 'to_delete', skip files ending in 'keep.png' and delete files older than 2024-01-01 and newer than 2023-12-01*

`*.png *to_delete *keep.png 2024-01-01 2023-12-01`
    
### Very simple CLI mode

In command prompt/batch file execute:

`FilePurge C:\Base\Folder\Path *.png *to_delete *keep.png 2024-01-01 2023-12-01`


Honorable mentions to this dude for the Wildcard string matching function, saved some time.
Thanks, nerd ;)

https://www.hiimray.co.uk/2020/04/18/implementing-simple-wildcard-string-matching-using-regular-expressions/474

