KinectXEFTools
==============
Finally, a cross-platform library for extracting data from KinectStudio XEF files!
This project is based on reverse-engineering the XEF file format, documenting it for future reference, and providing tools for reading event data (loosely follows the KStudioEventReader API style) as well as an example of how to extract different stream data (e.g., RGB, depth) to disk.

The KinectXEFTools library is built on .NET Standard 2.0, meaning it will work with programs written in the .NET Framework (Windows) or .NET Core (Windows/Linux/macOS). Cross-platform for the win!

Right now, the repository contains two Visual Studio solutions:
* KinectXEFTools: a .NET Standard library for reading from XEF files
* XEFExtract: a utility to extract XEF data to usable file formats (also an example of how to use the library)

Installation
------------
Download the source and include the project in your Visual Studio solution (or compile a .dll and import it separately).
NuGet package *hopefully* coming soon!

Dependencies
------------
Everything should be self-contained, you only need Visual Studio 2017, .NET Standard 2.0, and optionally the .NET Core 2.1 SDK for the example program to be installed.

Getting Started
---------------
Once you have the library in your solution, make sure to include the namespace in your code.
```C#
using KinectXEFTools;
```

### Open the XEF file

You can read in an XEF file by creating a new XEFEventReader and passing in the path.
```C#
XEFEventReader reader = new XEFEventReader("PATH_TO_FILE.xef");
...
reader.Close();
```

The *using* statement is also supported and is recommended.

```C#
using (XEFEventReader reader = new XEFEventReader("PATH_TO_FILE.xef"))
{
    ...
}
```

### Reading Events

An XEF file is basically a series of "events" that occur during a recording. Events are logically grouped into "streams," which are selected for recording in Kinect Studio. For example, you may have the "Nui Uncompressed Color" and "Nui Depth" streams selected in Kinect Studio, which will result in an XEF file that contains color/video and depth events.

Events are read sequentially from the file, so you may get a depth event followed by a color event, which is then followed by a skeleton event, etc.

```C#
XEFEvent ev;
while ((ev = reader.GetNextEvent()) != null)
{
    // Do something with the data from the event
    ...
}
```

You can also specify what type(s) of event (i.e., which stream) you want, and the reader will only return events matching that type.

```C#
XEFEvent ev = reader.GetNextEvent(StreamDataTypeIds.UncompressedColor);
```

```C#
XEFEvent ev = reader.GetNextEvent(new Guid[] {
    StreamDataTypeIds.UncompressedColor,
    StreamDataTypeIds.Body
});
```

Or you can just get a list of all those events at once.
```C#
IReadOnlyList<XEFEvent> allEvents = reader.GetAllEvents();
```

```C#
IReadOnlyList<XEFEvent> allSkeletonEvents = reader.GetAllEvents(StreamDataTypeIds.Body);
```

### Parsing the Data

Events contain the raw data buffer associated with the event in the file. These can be accessed directly if you already know the way the data is structured.

```C#
foreach (XEFEvent ev in reader.GetAllEvents(StreamDataTypeIds.UncompressedColor))
{
    byte[] rgbValues = ev.EventData;
    // Do something with the raw color bytes
    // Format: 1920x1080 @ 16bpp (YUYV image format)
    ...
}
```

The event data can be converted to objects that are more convenient to use. For example, the XEFBodyFrame class lets you easily get access to the joint positions of the skeleton motion capture data.

```C#
XEFBodyFrame bodyFrame = XEFBodyFrame.FromByteArray(ev.EventData);
XEFBodyData body = bodyFrame.BodyData[0];

if (body.TrackingState == XEFBodyTrackingState.TRACKED)
{
    XEFVector kneePosition = body.SkeletonJointPositions[XEFJointType.KneeRight];
    // Do something with the joint positions
    ...
}
```

Other examples of using the library can be found by browsing the source for the XEFExtract project under `examples/`.

Current Progress
----------------
- [X] Reverse-engineering event general stream format
- [X] Reading raw event data from streams
  - [X] Extracting audio from XEF
  - [X] Extracting depth frames from XEF
  - [X] Extracting color frames from XEF
  - [X] Extracting body skeletons from XEF
- [X] Support for archived/compressed XEF files (archived by the KSConvert tool)

- [ ] API for nicely accessing data (like XEFBodyFrame)
  - [ ] Access RGB frames as a 2D array of pixels
  - [ ] Access depth frames in terms of distance

- [ ] Make NuGet package for KinectXEFTools library
- [X] Add additional examples of usage
- [ ] Nicer documentation of reverse engineered XEF format
- [ ] Add full class documentation
