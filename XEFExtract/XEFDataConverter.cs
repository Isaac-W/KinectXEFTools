using KinectXEFTools;
using System;
using System.IO;
using System.Collections.Generic;

namespace XEFExtract
{
    public class XEFDataConverter : IDisposable
    {
        //
        //  Properties
        //

        public bool UseVideo { get; set; }
        public bool UseSkeleton { get; set; }
        public bool UseDepth { get; set; }
        public bool ResumeConversion { get; set; }

        //
        //  Constructor
        //

        public XEFDataConverter()
        {
        }

        ~XEFDataConverter()
        {
            Dispose(false);
        }

        //
        //	IDisposable
        //

        private bool disposed = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
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

            // Start parsing events
            try
            {
                using (IEventReader reader = XEFEventReader.GetEventReader(path))
                {
                    //
                    //  Set up XEF data converters/writers
                    //

                    List<IXEFDataWriter> dataWriters = new List<IXEFDataWriter>();
                    
                    if (videoFlag)
                    {
                        dataWriters.Add(new XEFColorWriter(rgbVideoPath));
                    }
                    
                    if (videoFlag)
                    {
                        dataWriters.Add(new XEFAudioWriter(wavAudioPath));
                    }

                    if (skeletonFlag)
                    {
                        dataWriters.Add(new XEFBodyWriter(skeletonPath));
                    }

                    if (depthFlag)
                    {
                        dataWriters.Add(new XEFDepthWriter(depthDatPath));
                    }

                    //
                    //  Process events
                    //

                    XEFEvent ev;
                    while ((ev = reader.GetNextEvent()) != null)
                    {
                        foreach (IXEFDataWriter dataWriter in dataWriters)
                        {
                            dataWriter.ProcessEvent(ev);
                        }
                    }

                    //
                    //  Finalization
                    //

                    if (videoFlag)
                    {
                        // Mux color and audio files into one video file
                        // TODO
                    }

                    foreach (IXEFDataWriter dataWriter in dataWriters)
                    {
                        dataWriter.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error processing file: " + path);
                Console.WriteLine("Exception thrown: " + ex.Message);
                return;
            }
        }
    }
}
