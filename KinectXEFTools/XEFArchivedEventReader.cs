using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace KinectXEFTools
{
    public class XEFArchivedEventReader : IEventReader
    {
        //
        //	Members
        //

        private Dictionary<short, XEFStream> _streams;

        private BinaryReader _reader;

        private int _totalReportedStreams;

        //
        //	Properties
        //

        public string FilePath { get; private set; }

        public bool EndOfStream { get; private set; }

        public int StreamCount { get { return Math.Max(_streams.Count, _totalReportedStreams); } }

        //
        //	Constructor
        //

        public XEFArchivedEventReader(string path)
        {
            FilePath = path;
            _reader = new BinaryReader(File.Open(path, FileMode.Open));

            InitializeStreams();
            SeekToEvents();
        }

        ~XEFArchivedEventReader()
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
        //  Methods
        //

        private void InitializeStreams()
        {
            _streams = new Dictionary<short, XEFStream>();

            // Get total number of streams from header
            _reader.BaseStream.Position = DataConstants.STREAM_COUNT_ADDRESS;
            _totalReportedStreams = _reader.ReadInt32();
            if (_totalReportedStreams == 0)
            {
                //throw new Exception("Error reading XEF! The file may be corrupt.");
                // This is fine for archived streams; should be able to recover
            }
            _totalReportedStreams--; // For some reason, XEF inflates the count by 1

            // Check compression status
            _reader.BaseStream.Position = DataConstants.ARC_STREAM_DESC_START_ADDRESS;
            short streamIndex = _reader.ReadInt16();
            short streamFlags = _reader.ReadInt16();

            if (streamIndex != DataConstants.ARC_STREAM_DESC_INDEX_KEY)
            {
                EndOfStream = true;
                _reader.Close();
                throw new Exception("Cannot open non-archived XEF! Use XEFEventReader instead.");
            }
        }

        private void SeekToEvents()
        {
            // Seek reader to start of events
            _reader.BaseStream.Position = DataConstants.ARC_STREAM_FIRST_EVENT_ADDRESS;
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
                short streamIndex = _reader.ReadInt16();
                short streamFlags = _reader.ReadInt16();

                // Check if reached unknown event or footer
                if (streamIndex == DataConstants.EVENT_UNKRECORD_INDEX)
                {
                    // TODO
                    EndOfStream = true;
                    return null;
                }

                // Check if is stream description event (missing flag)
                if (streamFlags == 0)
                {
                    // Check if stream exists already (shouldn't happen unless we reset)
                    if (_streams.ContainsKey(streamIndex))
                    {
                        // Seek to next event
                        // _reader.BaseStream.Position += STREAM_DESC_SIZE - sizeof(int);
                    }
                    
                    // Read stream description
                    _reader.ReadInt32(); // Size of record?
                    _reader.ReadInt64(); // Null timestamp?
                    _reader.ReadInt32(); // Unknown
                    _reader.ReadInt32(); // Size of record?

                    streamIndex = _reader.ReadInt16();
                    streamFlags = _reader.ReadInt16();

                    Guid dataTypeId = new Guid(_reader.ReadBytes(DataConstants.STREAM_TYPID_SIZE));

                    _reader.ReadInt32(); // Null

                    string dataTypeName = Encoding.Unicode.GetString(_reader.ReadBytes(DataConstants.STREAM_NAME_SIZE)).TrimEnd('\0');

                    _reader.ReadInt16(); // Unknown cookie 0x3333

                    int tagSize = _reader.ReadInt16();

                    _reader.ReadInt32(); // Unknown cookie 0x00000004
                    _reader.ReadInt32(); // Null
                    _reader.ReadInt32(); // Stream key

                    _reader.ReadBytes(DataConstants.ARC_STREAM_UNK_SIZE);
                    _reader.ReadBytes(DataConstants.STREAM_UNK_GUID_SIZE); // Unknown GUID string

                    Guid semanticId = new Guid(_reader.ReadBytes(DataConstants.STREAM_SEMID_SIZE));

                    // Create new stream and add to stream list
                    _streams[streamIndex] = new XEFStream(streamIndex, streamFlags, tagSize, dataTypeName, dataTypeId, semanticId);

                    // Get next event stream index ready
                    streamIndex = _reader.ReadInt16();
                    streamFlags = _reader.ReadInt16();
                }

                // Get event stream
                Debug.Assert(_streams.ContainsKey(streamIndex));
                XEFStream eventStream = _streams[streamIndex];

                // Read event metadata
                int dataSize = _reader.ReadInt32();
                TimeSpan relativeTime = TimeSpan.FromTicks(_reader.ReadInt64());
                uint unknown = _reader.ReadUInt32();
                int fullDataSize = _reader.ReadInt32(); // Uncompressed data size

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

                return new XEFEvent(eventStream, frameIndex, relativeTime, fullDataSize, tagData, eventData, unknown);
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
