# fxb2gra
Converts VST preset bank files to .gra usable in Image-Line Minihost Modular (and vice versa)

# Command line usage
fxb2gra [-rfga] infile1 infile2 infile3...

-r: recursive (directories only)

-f: extract fxp/fxb (directories only)

-g: wrap in .gra (directories only)

-a: don't wait for input on completion

# fxb2gra.json
fxb2gra's directory modes can be defined per directory via a "fxb2gra.json" file in the root of the directory passed.

{
   "mode": "f" or "g" (does the same thing as the respective switch)
   "recursive": true/false
}
