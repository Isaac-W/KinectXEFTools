using System;
using System.IO;

namespace XEFExtract
{
    class WavFileWriter : IDisposable
    {
        public enum SampleDepthOptions
        {
            BITS_PER_SAMPLE_8 = 8,
            BITS_PER_SAMPLE_16 = 16,
            BITS_PER_SAMPLE_24 = 24,
            BITS_PER_SAMPLE_32 = 32
        }

        //
        //  Format constants
        //

        private const ushort BITS_PER_BYTE = 8;
        private const ushort WAVE_HEADER_SIZE = 38;

        private const uint HDR_CHUNKID = 0x46464952;
        private const int HDR_CHUNKSIZE_OFFSET = 0x4;
        private const uint HDR_FORMAT = 0x45564157;

        private const uint FMT_SUBCHUNK1ID = 0x20746d66;
        private const uint FMT_SUBCHUNK1SIZE = 18;
        private const ushort FMT_AUDIOFORMAT = 1;
        private const ushort FMT_EXTRAPARAMSIZE = 0;

        private const uint DATA_SUBCHUNK2ID = 0x61746164;
        private const int DATA_SUBCHUNK2SIZE_OFFSET = 0x2A;
        private const int DATA_SAMPLES_OFFSET = 0x2C;

        //
        //  Members
        //

        private BinaryWriter _writer;

        //
        //  Properties
        //

        public string FilePath { get; private set; }

        public SampleDepthOptions SampleDepth { get; private set; }

        public uint SampleRate { get; private set; }

        public ushort BitsPerSample { get { return (ushort)SampleDepth; } }

        public ushort BytesPerSample { get { return (ushort)(BitsPerSample / BITS_PER_BYTE); } }

        public ushort NumChannels { get; private set; }

        public uint ChunkSize { get { return WAVE_HEADER_SIZE + DataSize; } }

        public uint ByteRate { get { return SampleRate * NumChannels * BytesPerSample; } }

        public ushort BlockAlign { get { return (ushort)(NumChannels * BytesPerSample); } }

        public uint NumSamples { get; private set; }

        public uint DataSize { get { return NumSamples * NumChannels * BytesPerSample; } }

        //
        //  Constructor
        //

        public WavFileWriter(string path, uint sampleRate, SampleDepthOptions bitsPerSample, ushort numChannels)
        {
            FilePath = path;

            SampleRate = sampleRate;
            SampleDepth = bitsPerSample;
            NumChannels = numChannels;
            NumSamples = 0;

            _writer = new BinaryWriter(File.Open(path, FileMode.Create));
            WriteHeader();
        }

        ~WavFileWriter()
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
                    UpdateHeader();

                    // Dispose managed resources
                    _writer.Dispose();
                }

                disposed = true;
            }
        }

        //
        //  Methods
        //

        public void Close()
        {
            Dispose(true);
        }

        private void WriteHeader()
        {
            // RIFF header
            _writer.Write(HDR_CHUNKID);
            _writer.Write(ChunkSize);
            _writer.Write(HDR_FORMAT);

            // fmt subchunk
            _writer.Write(FMT_SUBCHUNK1ID);
            _writer.Write(FMT_SUBCHUNK1SIZE);
            _writer.Write(FMT_AUDIOFORMAT);
            _writer.Write(NumChannels);
            _writer.Write(SampleRate);
            _writer.Write(ByteRate);
            _writer.Write(BlockAlign);
            _writer.Write(BitsPerSample);
            _writer.Write(FMT_EXTRAPARAMSIZE);

            // data subchunk
            _writer.Write(DATA_SUBCHUNK2ID);
            _writer.Write(DataSize);
        }

        private void UpdateHeader()
        {
            // Write total chunk size
            _writer.Seek(HDR_CHUNKSIZE_OFFSET, SeekOrigin.Begin);
            _writer.Write(ChunkSize);

            // Write total data size
            _writer.Seek(DATA_SUBCHUNK2SIZE_OFFSET, SeekOrigin.Begin);
            _writer.Write(DataSize);

            // Seek back to end
            _writer.Seek(0, SeekOrigin.End);
        }

        /// <summary>
        /// Writes a sample to the wav file.
        /// </summary>
        /// <param name="sample">A floating-point sample between -1.0 and +1.0</param>
        public void WriteSample(float sample)
        {
            switch (SampleDepth)
            {
                case SampleDepthOptions.BITS_PER_SAMPLE_8:
                    sbyte value8 = (sbyte)(sbyte.MaxValue * sample);
                    _writer.Write(value8);
                    break;
                case SampleDepthOptions.BITS_PER_SAMPLE_16:
                    short value16 = (short)(short.MaxValue * sample);
                    _writer.Write(value16);
                    break;
                case SampleDepthOptions.BITS_PER_SAMPLE_24:
                    byte[] value24 = BitConverter.GetBytes((int)(Math.Floor(0x7FFFFF * sample)));
                    _writer.Write(value24, 0, 3);
                    break;
                case SampleDepthOptions.BITS_PER_SAMPLE_32:
                    long value32 = (long)(long.MaxValue * sample);
                    _writer.Write(value32);
                    break;
            }

            NumSamples++;
        }

        /// <summary>
        /// Writes multiple samples to the wav file.
        /// </summary>
        /// <param name="samples">Floating-point samples between -1.0 and +1.0</param>
        public void WriteSamples(float[] samples)
        {
            foreach (float sample in samples)
            {
                WriteSample(sample);
            }
        }
    }
}
