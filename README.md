KinectXEFTools
=========================
Finally, a cross-platform method for extracting data from KinectStudio XEF files!
This project is based on reverse-engineering the XEF file format, documenting it for future reference, and providing tools for reading event data (loosely follows the KStudioEventReader API style) as well as an example of how to extract different stream data (e.g., RGB, depth) to disk.

The project is built on .NET Core 2.1, allowing for cross-platform deployment to Windows and Linux systems. Not only is this the only way to easily work with XEF files on Linux, but it's also currently around 3-5x faster than using Microsoft's Kinect Studio libraries based on internal benchmarks!

Right now, the solution consists of two projects:
* KinectXEFTools: a .NET Standard library for reading from XEF files
* XEFExtract: a utility to extract XEF data to usable file formats (also an example of how to use the library)

Current Progress
----------------
- [X] Reverse-engineering event general stream format
- [X] Reading raw event data from streams
- [X] Support for archived/compressed XEF files (with KSConvert)

- [X] Extracting audio from XEF
- [X] Extracting depth frames from XEF
- [ ] Extracting color frames from XEF
- [ ] Extracting body skeletons from XEF

- [X] Saving extracted audio data as WAVE file
- [ ] Saving depth frames as video
- [ ] Saving color frames as video
- [ ] Saving skeleton data to file

- [ ] Make NuGet package for KinectXEFTools library
- [ ] Add additional examples of usage
- [ ] Nicer documentation of reverse engineered XEF format
