using KinectXEFTools;
using System;
using System.Collections.Generic;
using System.Text;

namespace XEFExtract
{
    class XEFAudioWriter : IXEFDataWriter, IDisposable
    {
        //
        //  Members
        //

        private float _minAudioVal = float.MaxValue;
        private float _maxAudioVal = float.MinValue;
        private List<float[]> _audioBuffers = new List<float[]>();
        private WavFileWriter _writer;
        private bool _seenEvent = false;

        //
        //  Properties
        //

        public string FilePath { get; private set; }

        public long EventCount { get; private set; }

        public TimeSpan StartTime { get; private set; }

        public TimeSpan EndTime { get; private set; }

        public TimeSpan Duration { get { return EndTime - StartTime; } }

        //
        //  Constructor
        //

        public XEFAudioWriter(string path)
        {
            FilePath = path;
            EventCount = 0;
            StartTime = TimeSpan.Zero;
            EndTime = TimeSpan.Zero;

            _writer = new WavFileWriter(path, 16000, WavFileWriter.SampleDepthOptions.BITS_PER_SAMPLE_24, 1);
        }

        ~XEFAudioWriter()
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
                    WriteAudioBuffers();

                    // Dispose managed resources
                    _writer.Dispose();
                }

                disposed = true;
            }
        }

        //
        //  Methods
        //

        private void WriteAudioBuffers()
        {
            // Finish writing audio file
            float factor = Math.Max(Math.Abs(_minAudioVal), Math.Abs(_maxAudioVal)); // Normalization factor (the largest magnitude above or below zero) -- WaveFileWriter wants -1 to +1
            foreach (float[] samples in _audioBuffers)
            {
                // Normalize audio samples
                for (int i = 0; i < samples.Length; i++)
                {
                    samples[i] /= factor;
                }

                // Write samples to file
                _writer.WriteSamples(samples);
            }
        }

        public void Close()
        {
            Dispose(true);
        }

        public void ProcessEvent(XEFEvent ev)
        {
            if (ev.EventStreamDataTypeId != StreamDataTypeIds.Audio)
            {
                return;
            }

            // Update start/end time
            if (!_seenEvent)
            {
                StartTime = ev.RelativeTime;
                _seenEvent = true;
            }
            EndTime = ev.RelativeTime;

            // Get raw audio data
            XEFAudioFrame audioData = XEFAudioFrame.FromByteArray(ev.EventData);

            for (int i = 0; i < audioData.SubFrames.Length; i++)
            {
                for (int j = 0; j < audioData.SubFrames[i].OutBuffer.Length; j++)
                {
                    float sample = audioData.SubFrames[i].OutBuffer[j];

                    _minAudioVal = Math.Min(_minAudioVal, sample);
                    _maxAudioVal = Math.Max(_maxAudioVal, sample);
                }

                _audioBuffers.Add(audioData.SubFrames[i].OutBuffer);
            }

            EventCount++;
        }
    }
}
