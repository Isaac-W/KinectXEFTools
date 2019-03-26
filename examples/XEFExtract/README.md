XEFExtract
==========
This is a command-line application that reads in a Kinect Studio .XEF file and extracts/converts the raw data into video (.AVI), audio (.wav), skeleton (.csv), and depth (.dat) files. This project also serves as an example of how to use the KinectXEFTools library for reading data in from .XEF files.

The main extraction code is in XEFDataConverter.cs, which opens an .XEF file and loops through the events. The code relies on a number of "XEFWriters" that independently handle converting the raw data from an event into a video frame, wave audio, skeleton frame, etc. You should be able to modify or create your own XEFWriter to convert data into a format that works best with your applications.

Installation
------------
Download the source, open the Visual Studio solution, and build!

Dependencies
------------
* .NET Core 2.1 SDK
* ffmpeg -- the binary needs to be downloaded and accessible through your environment PATH

Usage
-----
```
xefextract [-v] [-s] [-d] [-resume] path/file
-v : Output video file
-s : Output skeleton file
-d : Output depth data

-resume : Resume extraction (skips existing files)

-i : Read from stdin (still requires path for output)

path/file : Directory or File to convert
```

Getting Started
---------------
Extract all possible data (video, skeleton, depth) from a file:
> xefextract PATH_TO_FILE.xef

Extract only skeleton data from a file:
> xefextract -s PATH_TO_FILE.xef

Extract video and skeleton from all .XEF files recursively:
> xefextract -v -s ./PATH/TO/XEF/FILES/

Resume extraction on a directory (if it was cancelled halfway through):
> xefextract -resume ./PATH/TO/XEF/FILES/

Read an .XEF file from stdin and extract it (for example, if a file was gzipped to save space):
> gzip -dc PATH_TO_FILE.xef.gz | xefextract -i
