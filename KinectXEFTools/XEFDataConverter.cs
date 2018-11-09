using System;
using System.Collections.Generic;
using System.IO;

namespace KinectXEFTools
{
    class XEFDataConverter : IDisposable
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

                float minAudioVal = float.MaxValue;
                float maxAudioVal = float.MinValue;
                List<float[]> audioBuffers = new List<float[]>();
                WavFileWriter audioWriter = null;

                if (videoFlag)
                {
                    audioWriter = new WavFileWriter(wavAudioPath, 16000, WavFileWriter.SampleDepthOptions.BITS_PER_SAMPLE_24, 1);
                }

                //
                //  Set up skeleton
                //

                //
                //  Set up depth
                //

                long depthframecount = 0;
                uint depthframesize = Constants.STREAM_DEPTH_WIDTH * Constants.STREAM_DEPTH_HEIGHT * 2; // 16bpp
                long depthStartTime = -1;
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

                XEFEvent ev;
                while ((ev = reader.GetNextEvent()) != null)
                {
                    if (UseVideo && ev.EventStreamDataTypeId == StreamDataTypeIds.UncompressedColor)
                    {
                        // TODO
                    }
                    else if (UseVideo && ev.EventStreamDataTypeId == StreamDataTypeIds.Audio)
                    {
                        XEFAudioData audioData = XEFAudioData.FromByteArray(ev.EventData);
                        
                        for (int i = 0; i < audioData.SubFrames.Length; i++)
                        {
                            for (int j = 0; j < audioData.SubFrames[i].OutBuffer.Length; j++)
                            {
                                float sample = audioData.SubFrames[i].OutBuffer[j];

                                minAudioVal = Math.Min(minAudioVal, sample);
                                maxAudioVal = Math.Max(maxAudioVal, sample);
                            }

                            audioBuffers.Add(audioData.SubFrames[i].OutBuffer);
                        }
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

                if (videoFlag)
                {
                    // Finish writing audio file
                    float factor = Math.Max(Math.Abs(minAudioVal), Math.Abs(maxAudioVal)); // Normalization factor (the largest magnitude above or below zero) -- WaveFileWriter wants -1 to +1
                    foreach (float[] samples in audioBuffers)
                    {
                        // Normalize audio samples
                        for (int i = 0; i < samples.Length; i++)
                        {
                            samples[i] /= factor;
                        }

                        // Write samples to file
                        audioWriter.WriteSamples(samples);
                    }
                    audioWriter.Close();
                }

                if (depthFlag)
                {
                    //Console.WriteLine("Depth Frames: " + depthframecount);

                    // Finish writing depth file
                    depthWriter.Seek(0, SeekOrigin.Begin);
                    depthWriter.Write(depthframecount); // Write depth frame count -- seek back to reserved location
                    depthWriter.Close();
                }
            }
        }
    }
}
