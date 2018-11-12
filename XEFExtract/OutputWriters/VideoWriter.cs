using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace XEFExtract
{
    public class VideoWriter : IDisposable
    {
        public enum FrameFormat
        {
            RGB24,
            BGR24,
            YUV444,
            YUV422,
            YUV420,
            YUYV422,
            GRAY8,
            GRAY16,
        }

        private static Dictionary<FrameFormat, string> FormatNames = new Dictionary<FrameFormat, string>()
        {
            { FrameFormat.RGB24,   "rgb24" },
            { FrameFormat.BGR24,   "bgr24" },
            { FrameFormat.YUV444,  "yuv444p" },
            { FrameFormat.YUV422,  "yuv422p" },
            { FrameFormat.YUV420,  "yuv420p" },
            { FrameFormat.YUYV422, "yuyv422" },
            { FrameFormat.GRAY8,   "gray" },
            { FrameFormat.GRAY16,  "gray16le" },
        };

        public enum VideoCodec
        {
            H264,
            XVID,
            FLV,
            MPEG4,
            WMV2,
            YUV,
            RGB,
        }

        private static Dictionary<VideoCodec, string> CodecNames = new Dictionary<VideoCodec, string>()
        {
            { VideoCodec.H264,  "libx264" },
            { VideoCodec.XVID,  "libxvid" },
            { VideoCodec.FLV,   "flv" },
            { VideoCodec.MPEG4, "mpeg4" },
            { VideoCodec.WMV2,  "wmv2" },
            { VideoCodec.YUV,   "yuv4" },
            { VideoCodec.RGB,   "r210" },
        };

        //
        //  Members
        //

        private Process _ffmpegProcess;
        
        //
        //  Properties
        //

        public string FilePath { get; private set; }

        public FrameFormat InputFormat { get; private set; }

        public VideoCodec OutputFormat { get; private set; }

        public int Width { get; private set; }

        public int Height { get; private set; }

        public int Framerate { get; private set; }

        public int Bitrate { get; private set; }

        //
        //  Constructor
        //

        public VideoWriter(string path, FrameFormat inputFormat, VideoCodec outputFormat, int width, int height, int framerate) :
            this(path, inputFormat, outputFormat, width, height, framerate, -1)
        {
        }

        public VideoWriter(string path, FrameFormat inputFormat, VideoCodec outputFormat, int width, int height, int framerate, int bitrate)
        {
            FilePath = path;
            InputFormat = inputFormat;
            OutputFormat = outputFormat;
            Width = width;
            Height = height;
            Framerate = framerate;
            Bitrate = bitrate;

            if (!VerifyFFMPEG())
            {
                throw new Exception("Could not initalize process! Please ensure FFMPEG is installed and part of the system path.");
            }

            // Start process and open streams
            _ffmpegProcess = new Process();
            _ffmpegProcess.StartInfo = new ProcessStartInfo()
            {
                FileName = "ffmpeg",
                Arguments = $"-i - -input_",
                UseShellExecute = false,
                CreateNoWindow = true
            };
        }

        ~VideoWriter()
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
                    // TODO Finish writing video and close streams/process
                    _ffmpegProcess.Dispose();
                }

                disposed = true;
            }
        }

        //
        //  Methods
        //

        private bool VerifyFFMPEG()
        {
            Process process = new Process();
            process.StartInfo = new ProcessStartInfo()
            {
                FileName = "ffmpeg",
                Arguments = "",
                UseShellExecute = false,
                CreateNoWindow = true
            };

            try
            {
                process.Start();
            }
            catch (Exception)
            {
                return false;
            }
            finally
            {
                process.Close();
            }

            return true;
        }

        public void Close()
        {
            Dispose(true);
        }

        /*
        // Combine audio and RGB video streams
        Process ffmpegProc = new Process();
        ffmpegProc.StartInfo.FileName = PATH_TO_FFMPEG;
        ffmpegProc.StartInfo.Arguments = "-i " + rgbVideoPath + " -i " + wavAudioPath + " -codec copy -shortest -y " + recordingBaseName + "_Video.mp4";

        ffmpegProc.Start();
        ffmpegProc.WaitForExit();
        ffmpegProc.Close();
        */
    }
}
