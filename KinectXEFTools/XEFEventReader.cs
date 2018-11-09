using System;
using System.Text;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using System.Diagnostics;

namespace KinectXEFTools
{
	public class XEFEventReader : IDisposable, IEventReader
	{
        /// <summary>
        /// Returns an event reader for the XEF file (may be compressed XEF).
        /// </summary>
        /// <param name="path">Path to the XEF file to open.</param>
        /// <returns>An event reader (either XEFEventReader or XEFArchivedEventReader).</returns>
        public static IEventReader GetEventReader(string path)
        {
            try
            {
                return new XEFEventReader(path);
            }
            catch (Exception ex)
            {
                return new XEFArchivedEventReader(path);
            }
        }

		//
		//	Data offsets
		//
		
        private const int STREAM_KEY_COMPRESSION_FLAG = 0x00000100;

		private const int STREAM_COUNT_ADDRESS = 0xC;
		private const int STREAM_COUNT_SIZE = 4; // int
                      
		private const int STREAM_DESC_START_ADDRESS = 0x4B4;
		private const int STREAM_DESC_SIZE = 486;
                      
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
                    EndOfStream = true;
                    throw new Exception("Cannot open archived XEF! Use XEFArchivedEventReader instead.");
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
