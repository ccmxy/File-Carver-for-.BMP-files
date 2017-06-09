# File-Carver-for-.BMP-files
A simple one-file program that carves out bmp files and finds non-bmp files in them as well, made according to this article: https://en.wikipedia.org/wiki/BMP_file_format

The whole thing is in program.cs and it's a Console App (.Net Framework version 5.4.2) in c#, made with MS Visual Studio 2017.

To use it just build and run the program and enter the name of the file (like simple.bmp, which is actually 3 files) into the console window when prompted, and it will output all 3 files into a new folder in the same directory that the input file is in
The "other" file is actually a png file, so you can just edit the .other extension to .png to see it. I haven't tested it on files that don't start with bmp yet. 