using System;
using System.Text;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;

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
	
	public class XEFStream
	{

		public XEFStream(int index, int tagSize, string dataTypeName, Guid dataTypeId, Guid semanticId)
		{
            EventCount = 0;
            StreamIndex = index;
            DataTypeName = dataTypeName;
			TagSize = tagSize;
			DataTypeId = dataTypeId;
			SemanticId = semanticId;
		}

        public int EventCount { get; private set; }
		
		public int StreamIndex { get; private set; }

        public string Name { get { return DataTypeName; } }

		public int TagSize { get; private set; }

        public string DataTypeName { get; private set; }
		
		public Guid DataTypeId { get; private set; }
		
		public Guid SemanticId { get; private set; }

        public bool IsCompressed { get; private set; }

        public void IncrementEventCount()
        {
            EventCount++;
        }

        public override string ToString()
        {
            return Name;
        }
    }
	
	public class XEFEvent
	{
		public XEFEvent(XEFStream eventStream, int frameIndex, TimeSpan relativeTime, byte[] tagData, byte[] eventData, uint unknown)
		{
            EventStream = eventStream;
            FrameIndex = frameIndex;
            RelativeTime = relativeTime;
            TagData = tagData;
            EventData = eventData;
            Unknown = unknown;

            EventIndex = EventStream.EventCount;
            EventStream.IncrementEventCount(); // Need to tell stream that an event has been added
        }
		
		public XEFStream EventStream { get; private set; }
		
        public int EventIndex { get; private set; }

		public int FrameIndex { get; private set; }

		public TimeSpan RelativeTime { get; private set; }

        public int TagDataSize { get { return EventStream.TagSize; } }

        public int EventDataSize { get { return EventData.Length; } }

        public Guid EventStreamSemanticId { get { return EventStream.SemanticId; } }

		public Guid EventStreamDataTypeId { get { return EventStream.DataTypeId; } }

        public byte[] TagData { get; private set; }

        public byte[] EventData { get; private set; }

        public byte[] RawEventData { get; private set; }

        public bool IsCompressed { get; private set; }

        public uint Unknown { get; private set; } // TODO Unknown data found in event (could be some index or id)
	}
	
	public class XEFEventReader : IDisposable
	{
		//
		//	Data offsets
		//
		
		private const int STREAM_COUNT_ADDRESS = 0xC;
		private const int STREAM_COUNT_SIZE = 4; // int
                      
		private const int STREAM_DESC_START_ADDRESS = 0x4B4;
		private const int STREAM_DESC_SIZE = 486;
        private const int STREAM_ARC_DESC_SIZE = 494; // archived stream description
                      
		private const int STREAM_INDEX_SIZE = 4; // int
        private const int STREAM_UNK1_SIZE = 4;
        private const int STREAM_NULL_SIZE = 8;
		private const int STREAM_UNK2_SIZE = 4;
        private const int STREAM_KEY_SIZE = 4;
		private const int STREAM_TYPID_SIZE = 16; // guid
		private const int STREAM_UNK3_SIZE = 4;
		private const int STREAM_NAME_SIZE = 256; // wstr
		private const int STREAM_UNK4_SIZE = 2;
		private const int STREAM_TAG_SIZE = 2; // ushort
		private const int STREAM_UNK5_SIZE = 162;
        private const int STREAM_ARC_UNK5_SIZE = 170; // larger for archived stream
		private const int STREAM_SEMID_SIZE = 16; // guid
                      
		private const int EVENT_HEADER_SIZE = 24;
        private const int EVENT_DATALEN_SIZE = 4; // int
        private const int EVENT_TIME_SIZE = 8; // long (TimeSpan.Ticks)
        private const int EVENT_UNK_SIZE = 4;
		
		//
		//	Members
		//
		
		private Dictionary<int, XEFStream> _streams;
		
		private BinaryReader _reader;

        //
        //	Properties
        //

        public string FilePath { get; private set; }

        public bool EndOfStream { get; private set; }

        public int StreamCount { get { return _streams.Count; } }

        public bool IsCompressed { get; private set; }

        //
        //	Constructor
        //

        public XEFEventReader(string path)
		{
            FilePath = path;
			_reader = new BinaryReader(File.Open(path, FileMode.Open));
			
			InitializeStreams();
			SeekToEvents();
		}

        ~XEFEventReader()
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
					_reader.Dispose();
				}
				
				disposed = true;
			}
		}
		
		//
		//	Methods
		//
		
		private void InitializeStreams()
		{
            _streams = new Dictionary<int, XEFStream>();

			// Get total number of streams from header
			_reader.BaseStream.Position = STREAM_COUNT_ADDRESS;
            int streamCount = _reader.ReadInt32();
            streamCount -= 1; // For some reason, XEF inflates the count by 1

            // Read in stream descriptions and populate stream list
            _reader.BaseStream.Position = STREAM_DESC_START_ADDRESS;
            for (int i = 0; i < streamCount; i++)
            {
                int streamIndex = _reader.ReadInt32();

                if (streamIndex == 0xFFFF)
                {
                    // This XEF is archived!
                    IsCompressed = true;
                }

                // Read stream description (TODO make class/struct once we have more info)
                _reader.ReadBytes(STREAM_UNK2_SIZE);
                Guid dataTypeId = new Guid(_reader.ReadBytes(STREAM_TYPID_SIZE));
                _reader.ReadBytes(STREAM_UNK2_SIZE);
                string dataTypeName = Encoding.Unicode.GetString(_reader.ReadBytes(STREAM_NAME_SIZE)).TrimEnd('\0');
                _reader.ReadBytes(STREAM_UNK4_SIZE);
                int tagSize = _reader.ReadInt16();
                _reader.ReadBytes(STREAM_UNK5_SIZE);
                Guid semanticId = new Guid(_reader.ReadBytes(STREAM_SEMID_SIZE));

                _streams[streamIndex] = new XEFStream(streamIndex, tagSize, dataTypeName, dataTypeId, semanticId);
            }
		}
		
		private void SeekToEvents()
		{
            // Seek the reader to the start of the actual XEF events
            _reader.BaseStream.Position = STREAM_DESC_START_ADDRESS + STREAM_DESC_SIZE * StreamCount;
		}
		
		public void Close()
		{
			Dispose(true);
		}
		
        /// <summary>
        /// Gets the next event from the file.
        /// </summary>
        /// <returns>The next XEFEvent if available; otherwise, null.</returns>
		public XEFEvent GetNextEvent()
		{
			if (!EndOfStream)
			{
                int streamIndex = _reader.ReadInt32();

                // Check if reached end of events (denoted by special cookie index)
                if (streamIndex == 0xFFFF)
                {
                    EndOfStream = true;
                    return null;
                }

                XEFStream eventStream = _streams[streamIndex];

                // Read event metadata
                int dataSize = _reader.ReadInt32();
                TimeSpan relativeTime = TimeSpan.FromTicks(_reader.ReadInt64());
                uint unknown = _reader.ReadUInt32();
                _reader.ReadInt32(); // Second data size field

                // Read tag if needed
                byte[] tagData = null;
                int frameIndex = 0;

                if (eventStream.TagSize > 0)
                {
                    tagData = _reader.ReadBytes(eventStream.TagSize);

                    // Treat tag as frame number
                    if (eventStream.TagSize == 4)
                    {
                        frameIndex = BitConverter.ToInt32(tagData, 0);
                    }
                }

                // Read event data
                byte[] eventData = _reader.ReadBytes(dataSize);

                // Check end of stream (shouldn't ever reach due to XEF footer)
                if (_reader.BaseStream.Position == _reader.BaseStream.Length)
                {
                    EndOfStream = true;
                }

                return new XEFEvent(eventStream, frameIndex, relativeTime, tagData, eventData, unknown);
            }
			
			return null;
		}

        /// <summary>
        /// Gets the next event of specified type from the file.
        /// </summary>
        /// <param name="streamDataType">The event type to retrieve.</param>
        /// <returns>The next XEFEvent if available; otherwise, null.</returns>
        public XEFEvent GetNextEvent(Guid streamDataType)
        {
            return GetNextEvent(new Guid[] { streamDataType });
        }

        /// <summary>
        /// Gets the next event of specified types from the file.
        /// </summary>
        /// <param name="streamDataTypes">The event types to retrieve.</param>
        /// <returns>The next XEFEvent if available; otherwise, null.</returns>
        public XEFEvent GetNextEvent(ICollection<Guid> streamDataTypes)
        {
            XEFEvent xefEvent = null;
            
            do
            {
                xefEvent = GetNextEvent();
            } while (xefEvent != null && !streamDataTypes.Contains(xefEvent.EventStreamDataTypeId));

            return xefEvent;
        }

        /// <summary>
        /// Gets all events from the file.
        /// </summary>
        /// <returns>An IEnumerable of all the XEFEvents.</returns>
        public IEnumerable<XEFEvent> GetAllEvents()
		{
			// Reset reader
			SeekToEvents();
			
			// Read all events into list
			List<XEFEvent> allEvents = new List<XEFEvent>();
			
			while (!EndOfStream)
			{
				XEFEvent xefEvent = GetNextEvent();
				if (xefEvent != null)
				{
					allEvents.Add(xefEvent);
				}
			}
			
			return allEvents;
		}

        /// <summary>
        /// Gets all events of specified type from the file.
        /// </summary>
        /// <param name="streamDataType">The event type to retrieve.</param>
        /// <returns>An IEnumerable of all the XEFEvents.</returns>
        public IEnumerable<XEFEvent> GetAllEvents(Guid streamDataType)
        {
            return GetAllEvents(new Guid[] { streamDataType });
        }

        /// <summary>
        /// Gets all events of specified types from the file.
        /// </summary>
        /// <param name="streamDataTypes">The event type to retrieve.</param>
        /// <returns>An IEnumerable of all the XEFEvents.</returns>
        public IEnumerable<XEFEvent> GetAllEvents(ICollection<Guid> streamDataTypes)
        {
            // Reset reader
            SeekToEvents();

            // Read all events into list
            List<XEFEvent> allEvents = new List<XEFEvent>();

            while (!EndOfStream)
            {
                XEFEvent xefEvent = GetNextEvent();
                if (xefEvent != null && streamDataTypes.Contains(xefEvent.EventStreamDataTypeId))
                {
                    allEvents.Add(xefEvent);
                }
            }

            return allEvents;
        }
    }
}
