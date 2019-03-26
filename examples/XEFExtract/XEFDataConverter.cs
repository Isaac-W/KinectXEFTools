using KinectXEFTools;
using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;

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
                }

                disposed = true;
            }
        }

        //
        //  Methods
        //

        public void ConvertFile(string path, Stream input = null)
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

            // Start parsing events
            try
            {
                //
                //  Set up XEF data converters/writers
                //

                List<IXEFDataWriter> dataWriters = new List<IXEFDataWriter>();
                    
                if (videoFlag)
                {
                    dataWriters.Add(new XEFColorWriter(rgbVideoPath));
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

                if (dataWriters.Count == 0)
                {
                    Console.WriteLine("Skipped: " + basePath);
                    return;
                }

                //
                //  Process events
                //

                using (IEventReader reader = (input == null) ? new XEFEventReader(path) : new XEFEventStreamReader(input) as IEventReader)
                {
                    XEFEvent ev;
                    while ((ev = reader.GetNextEvent()) != null)
                    {
                        foreach (IXEFDataWriter dataWriter in dataWriters)
                        {
                            dataWriter.ProcessEvent(ev);
                        }
                    }
                }

                //
                //  Finalization
                //

                foreach (IXEFDataWriter dataWriter in dataWriters)
                {
                    dataWriter.Close();
                }

                if (videoFlag)
                {
                    // First determine if there were any audio events processed
                    bool containsAudio = false;
                    foreach (IXEFDataWriter dataWriter in dataWriters)
                    {
                        if (dataWriter.GetType() == typeof(XEFAudioWriter))
                        {
                            containsAudio = dataWriter.EventCount > 0;
                        }
                    }

                    // Mux color and audio files into one video file
                    using (Process ffmpegProc = new Process())
                    {
                        ffmpegProc.StartInfo = new ProcessStartInfo()
                        {
                            FileName = "ffmpeg",
                            Arguments =
                                $"-v quiet " +
                                $"-i \"{rgbVideoPath}\" " +
                                (containsAudio ? $"-i \"{wavAudioPath}\" " : "") +
                                $"-codec copy " +
                                $"-shortest " +
                                $"-y " +
                                $"\"{fulVideoPath}\"",
                            UseShellExecute = false,
                            CreateNoWindow = true,
                        };

                        ffmpegProc.Start();
                        ffmpegProc.WaitForExit();
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
