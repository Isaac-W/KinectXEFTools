using System;
using System.Collections.Generic;
using System.IO;

namespace KinectXEFTools
{
    class XEFDataConverter : IDisposable
    {
        //
        //  Constructor
        //

        public XEFDataConverter()
        {
        }

        //
        //  Properties
        //

        public bool UseVideo { get; set; }
        public bool UseSkeleton { get; set; }
        public bool UseDepth { get; set; }
        public bool ResumeConversion { get; set; }

        //
        //	IDisposable
        //

        private bool disposed = false;

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources
                    // TODO
                }

                disposed = true;
            }
        }

        //
        //  Methods
        //

        public void ConvertFile(string path)
        {
            using (XEFEventReader reader = new XEFEventReader(path))
            {
                //
                //  Set up filenames
                //

                string basePath = Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path));

                string rgbVideoPath = basePath + "_RGB.avi";
                string wavAudioPath = basePath + "_Audio.wav";
                string fulVideoPath = basePath + "_Video.avi";
                string skeletonPath = basePath + "_Skeleton.txt";
                string depthDatPath = basePath + "_Depth.dat";

                bool videoFlag = UseVideo;
                bool skeletonFlag = UseSkeleton;
                bool depthFlag = UseDepth;

                // Check if files exist (disable flags if found)
                if (ResumeConversion)
                {
                    if (File.Exists(fulVideoPath)) videoFlag = false;
                    if (File.Exists(skeletonPath)) skeletonFlag = false;
                    if (File.Exists(depthDatPath)) depthFlag = false;
                }

                if (!(videoFlag || skeletonFlag || depthFlag))
                {
                    Console.WriteLine("Skipped: " + basePath);
                    return;
                }

                // TODO RGB, Audio, and Skeleton currently not supported!

                //
                //  Set up video
                //

                //
                //  Set up audio
                //

                //
                //  Set up skeleton
                //

                //
                //  Set up depth
                //

                long depthframecount = 0;
                uint depthframesize = Constants.STREAM_DEPTH_WIDTH * Constants.STREAM_DEPTH_HEIGHT * 2; // 16bpp
                BinaryWriter depthWriter = null;

                if (depthFlag)
                {
                    depthWriter = new BinaryWriter(new FileStream(depthDatPath, FileMode.Create));

                    // Write initial headers
                    depthWriter.Write(depthframecount); // 8 bytes -- RESERVED (this is updated after enumerating through all the frames)
                    depthWriter.Write(Constants.STREAM_DEPTH_WIDTH); // 4 bytes
                    depthWriter.Write(Constants.STREAM_DEPTH_HEIGHT); // 4 bytes
                    depthWriter.Write(depthframesize); // 4 bytes
                }

                //
                //  Process events
                //

                // Starting time
                long depthStartTime = -1;

                XEFEvent ev;
                while ((ev = reader.GetNextEvent(StreamDataTypeIds.Depth)) != null)
                {
                    if (UseVideo && ev.EventStreamDataTypeId == StreamDataTypeIds.UncompressedColor)
                    {
                        // TODO
                    }
                    else if (UseVideo && ev.EventStreamDataTypeId == StreamDataTypeIds.Audio)
                    {
                        // TODO
                    }
                    else if (UseSkeleton && ev.EventStreamDataTypeId == StreamDataTypeIds.Body)
                    {
                        // TODO
                    }
                    else if (UseDepth && ev.EventStreamDataTypeId == StreamDataTypeIds.Depth)
                    {
                        // Get frame time
                        long frameTime = ev.RelativeTime.Ticks;
                        if (depthStartTime < 0) depthStartTime = frameTime;
                        frameTime -= depthStartTime;

                        // Write to binary file
                        //depthWriter.Write(frameTime); // 8 bytes
                        depthWriter.Write(depthframecount);
                        depthWriter.Write(ev.EventData);

                        depthframecount++;
                    }
                }

                //
                //  Finalization
                //

                if (depthFlag)
                {
                    //Console.WriteLine("Depth Frames: " + depthframecount);

                    // Write depth frame count -- seek back to reserved location
                    depthWriter.Seek(0, SeekOrigin.Begin);
                    depthWriter.Write(depthframecount);
                    depthWriter.Close();
                }
            }
        }
    }
}
