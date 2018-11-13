using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace XEFExtract
{
    public class VideoWriter : IDisposable
    {
        private const int TICKS_PER_SECOND = 10000000;

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

        private TimeSpan _frameDuration;

        private byte[] _lastFrameData;

        private Process _ffmpegProcess;

        private BinaryWriter _writer;

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

        public int FrameCount { get; private set; }

        public TimeSpan Duration { get { return FrameToTime(FrameCount); } }

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
            FrameCount = 0;

            _frameDuration = FrameToTime(1);
            _lastFrameData = null;

            try
            {
                // Start process and open streams
                _ffmpegProcess = new Process();
                _ffmpegProcess.StartInfo = new ProcessStartInfo()
                {
                    FileName = "ffmpeg",
                    Arguments =
                        $"-v quiet " + // Hide all messages
                        $"-stats " + // Only see video output stats
                        $"-f rawvideo " +
                        $"-pixel_format {FormatNames[inputFormat]} " +
                        $"-video_size {width}x{height} " +
                        $"-framerate {framerate} " +
                        $"-i - " +
                        $"-c:v {CodecNames[outputFormat]} " +
                        (bitrate > 0 ? $"-b:v {bitrate} " : "") +
                        $"-y " +
                        $"\"{path}\"",
                    UseShellExecute = false,
                    CreateNoWindow = false, // Set to false to see ffmpeg output (good progress indicator)
                    RedirectStandardInput = true,
                };
                _ffmpegProcess.Start();
            }
            catch (Exception)
            {
                _ffmpegProcess.Dispose();
                throw new Exception("Could not initalize process! Please ensure FFMPEG is installed and part of the system path.");
            }

            _writer = new BinaryWriter(_ffmpegProcess.StandardInput.BaseStream);
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
                    _writer.Dispose(); // Need to close stream otherwise FFMPEG won't exit!
                    _ffmpegProcess.WaitForExit();
                    _ffmpegProcess.Dispose();
                }

                disposed = true;
            }
        }

        //
        //  Methods
        //

        private TimeSpan FrameToTime(int frame)
        {
            return TimeSpan.FromTicks((long)frame * TICKS_PER_SECOND / Framerate);
        }

        public void Close()
        {
            Dispose(true);
        }

        public void WriteFrame(byte[] frameData)
        {
            _writer.Write(frameData);
            _lastFrameData = frameData;
            FrameCount++;
        }

        public void WriteFrame(byte[] frameData, TimeSpan timestamp)
        {
            if (_lastFrameData != null)
            {
                // Skip frame if more than a frame behind
                if (timestamp + _frameDuration < Duration)
                {
                    return;
                }

                // Pad frames if timestamp is over a frame away
                while (Duration + _frameDuration < timestamp)
                {
                    WriteFrame(_lastFrameData);
                }
            }

            WriteFrame(frameData);
        }
    }
}
