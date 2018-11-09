using System;

namespace KinectXEFTools
{
    public class Constants
    {
        public const uint STREAM_FRAME_LIMIT_MAXIMUM = 15;
        public const uint AUDIO_FRAME_VERSION_MINOR_MASK = 65535;
        public const uint AUDIO_FRAME_VERSION_MAJOR_MASK = 4294901760;
        public const uint AUDIO_FRAME_VERSION = 65536;
        public const uint AUDIO_RESERVED_BYTE_ARRAY_SIZE = 1024;
        public const uint AUDIO_NUM_SPK = 8;
        public const uint AUDIO_NUM_MIC = 4;
        public const uint AUDIO_SAMPLES_PER_SUBFRAME = 256;
        public const uint AUDIO_SAMPLERATE = 16000;
        public const uint AUDIO_MAX_SUBFRAMES = 8;
        public const uint MAX_AUDIO_FRAME_SIZE = 115344;
        public const uint STREAM_BODY_INDEX_HEIGHT = 424;
        public const uint STREAM_COLOR_HEIGHT = 1080;
        public const uint STREAM_COLOR_WIDTH = 1920;
        public const uint STREAM_IR_HEIGHT = 424;
        public const uint STREAM_IR_WIDTH = 512;
        public const uint STREAM_DEPTH_HEIGHT = 424;
        public const uint STREAM_DEPTH_WIDTH = 512;
        public const byte BODY_INDEX_BACKGROUND = 255;
        public const uint BODY_INVALID_TRACKING_ID = 0;
        public const uint BODY_COUNT = 6;
        public const uint STREAM_BODY_INDEX_WIDTH = 512;
    }

    public static class StreamDataTypeIds
	{
		public static readonly Guid UncompressedColor = new Guid("{2ba0d67d-be11-4534-9444-3fb21ae0f08b}");
        public static readonly Guid LongExposureIr    = new Guid("{7e06d98e-d271-4a1f-9bfd-6648a700db75}");
        public static readonly Guid CompressedColor   = new Guid("{0a3914dc-3b16-11e1-aac3-001e4fd58c0f}");
        public static readonly Guid RawIr             = new Guid("{0a3914e2-3b16-11e1-aac3-001e4fd58c0f}");
        public static readonly Guid BodyIndex         = new Guid("{df82ffac-b533-4438-954a-686a1e20f4aa}");
        public static readonly Guid Body              = new Guid("{a0c45179-5168-4875-a75c-f8f1760f637c}");
        public static readonly Guid Depth             = new Guid("{0a3914d6-3b16-11e1-aac3-001e4fd58c0f}");
        public static readonly Guid Ir                = new Guid("{0a3914d7-3b16-11e1-aac3-001e4fd58c0f}");
        public static readonly Guid Audio             = new Guid("{787c7abd-9f6e-4a85-8d67-6365ff80cc69}");
        public static readonly Guid Null              = new Guid("{00000000-0000-0000-0000-000000000000}");
    }
}
