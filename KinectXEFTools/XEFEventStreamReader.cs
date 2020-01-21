﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace KinectXEFTools
{
    /// <summary>
    /// Modified version of XEFEventReader that reads in an XEF file as a stream. Used for reading XEF files piped through stdin.
    /// </summary>
    public class XEFEventStreamReader : IEventReader
    {
        private class SkippableStream : Stream
        {
            private long _position;
            private byte[] _buffer;

            public SkippableStream(Stream baseStream) :
                base()
            {
                BaseStream = baseStream;
            }

            public Stream BaseStream { get; private set; }

            public override bool CanRead => BaseStream.CanRead;

            public override bool CanSeek => BaseStream.CanSeek;

            public override bool CanWrite => BaseStream.CanWrite;

            public override long Length => BaseStream.Length;

            public override long Position
            {
                get
                {
                    if (CanSeek)
                    {
                        return BaseStream.Position;
                    }
                    else
                    {
                        return _position;
                    }
                }
                set
                {
                    if (CanSeek)
                    {
                        BaseStream.Position = value;
                    }
                    else
                    {
                        if (_position > value)
                        {
                            throw new Exception("Non-seekable stream is already past seek location!");
                        }
                        else
                        {
                            int count = (int)(value - _position);
                            if (_buffer == null || _buffer.Length < count)
                            {
                                _buffer = new byte[count * 2];
                            }

                            // Consume bytes
                            Read(_buffer, 0, count);
                        }
                    }
                }
            }

            public override void Flush()
            {
                BaseStream.Flush();
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                int bytesRead = BaseStream.Read(buffer, offset, count);
                _position += bytesRead;
                return bytesRead;
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                if (CanSeek)
                {
                    return BaseStream.Seek(offset, origin);
                }
                else
                {
                    return _position;
                }
            }

            public override void SetLength(long value)
            {
                BaseStream.SetLength(value);
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                BaseStream.Write(buffer, offset, count);
            }
        }

        //
        //	Members
        //

        private Dictionary<short, XEFStream> _streams;

        private BinaryReader _reader;

        private int _totalReportedStreams;

        private long _totalDataSize;

        private long _eventStartAddress;

        private long _cachedPosition;
        private short _cachedIndex, _cachedFlags;

        //
        //	Properties
        //

        public string FilePath { get; private set; }

        public bool EndOfStream { get; private set; }

        public bool StreamError { get; private set; }

        public int StreamCount { get { return Math.Max(_streams.Count, _totalReportedStreams); } }

        public ICollection<XEFStream> StreamList { get { return _streams.Values; } }

        public XEFEvent CurrentEvent { get; private set; }

        //
        //	Constructor
        //

        public XEFEventStreamReader(Stream stream)
        {
            FilePath = "";

            Stream baseStream = stream;
            if (!baseStream.CanSeek)
            {
                baseStream = new SkippableStream(baseStream);
            }
            _reader = new BinaryReader(baseStream);
            _streams = new Dictionary<short, XEFStream>();

            ReadHeader();
            _eventStartAddress = _reader.BaseStream.Position;
        }

        ~XEFEventStreamReader()
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
        
        private void ReadHeader()
        {
            // Get total number of streams from header
            _reader.BaseStream.Position = DataConstants.STREAM_COUNT_ADDRESS;
            _totalReportedStreams = _reader.ReadInt32();
            if (_totalReportedStreams == 0)
            {
                //throw new Exception("Error reading XEF! The file may be corrupt.");
                // This is fine for archived streams; should be able to recover
                _totalReportedStreams = int.MaxValue; // Set to large value to avoid issues with IsValidStreamIndex()
            }
            _totalReportedStreams--; // For some reason, XEF inflates the count by 1
            _totalDataSize = _reader.ReadInt64();
        }
        
        private bool IsValidStreamIndex(short index)
        {
            // Is the stream a valid (non-property) stream, or is unknown or is signaling end of file
            return (index > 0 && index <= _totalReportedStreams) || index == DataConstants.EVENT_UNKRECORD_INDEX || index == _totalReportedStreams + 1;
        }

        public void Close()
        {
            Dispose(true);
        }

        private void GetEventKey(out short streamIndex, out short streamFlags)
        {
            PeekEventKey(out streamIndex, out streamFlags);
            _cachedPosition = 0;
        }

        private void PeekEventKey(out short streamIndex, out short streamFlags)
        {
            if (_reader.BaseStream.Position == _cachedPosition)
            {
                streamIndex = _cachedIndex;
                streamFlags = _cachedFlags;
            }
            else
            {
                streamIndex = _reader.ReadInt16();
                streamFlags = _reader.ReadInt16();

                _cachedPosition = _reader.BaseStream.Position;
                _cachedIndex = streamIndex;
                _cachedFlags = streamFlags;
            }
        }

        private XEFStream ReadStreamDescription()
        {
            //
            //  Stream name
            //

            XEFEvent nameEvent = ReadDataEvent();

            // Extract useful data
            short streamIndex = BitConverter.ToInt16(nameEvent.TagData, DataConstants.STREAM_INDEX_OFFSET);
            short streamFlags = BitConverter.ToInt16(nameEvent.TagData, DataConstants.STREAM_FLAGS_OFFSET);
            bool compressed = (streamFlags & DataConstants.FLAG_COMPRESSED) == DataConstants.FLAG_COMPRESSED;

            byte[] guidBuf = new byte[DataConstants.STREAM_TYPID_SIZE];
            Array.Copy(nameEvent.TagData, DataConstants.STREAM_TYPID_OFFSET, guidBuf, 0, DataConstants.STREAM_TYPID_SIZE);

            Guid dataTypeId = new Guid(guidBuf);
            
            string dataTypeName = Encoding.Unicode.GetString(nameEvent.EventData, 0, DataConstants.STREAM_NAME_SIZE).TrimEnd('\0');
            short tagSize = BitConverter.ToInt16(nameEvent.EventData, DataConstants.STREAM_TAGSIZE_OFFSET);

            //
            //  Unknown compressed data
            //

            // If compressed, skip extra data (unknown purpose)
            if (compressed)
            {
                _reader.ReadBytes(DataConstants.ARC_STREAM_EXTRA_UNK_SIZE);
            }

            //
            //  Stream GUID
            //
            
            // Second event is considered a property event with stream index 0
            XEFEvent guidEvent = ReadDataEvent();

            Array.Copy(nameEvent.EventData, DataConstants.STREAM_SEMID_OFFSET, guidBuf, 0, DataConstants.STREAM_SEMID_SIZE);
            Guid semanticId = new Guid(guidBuf);

            // Create new stream
            return new XEFStream(streamIndex, streamFlags, tagSize, dataTypeName, dataTypeId, semanticId);
        }

        private void ReadUnknownEvent()
        {
            short streamIndex, streamFlags;
            GetEventKey(out streamIndex, out streamFlags);

            // Unknown event; skip it!
            Debug.Assert(streamIndex == DataConstants.EVENT_UNKRECORD_INDEX);
            //Debug.Assert(streamFlags == 0);

            int unkId = _reader.ReadInt32(); // Unknown id
            _reader.ReadInt64(); // Timestamp
            _reader.ReadInt32(); // Null
            _reader.ReadInt32(); // Null

            // Right now, we'll just heuristically skip through by 0x1000 at a time until we find a valid stream id
            // TODO Figure out better way to identify how long an unknown record is
            _reader.ReadBytes(0x6000);
            short peekIndex, peekFlags;
            PeekEventKey(out peekIndex, out peekFlags);

            if (!IsValidStreamIndex(peekIndex))
            {
                // Try next position (0x7000 total length)
                _reader.ReadBytes(0x1000 - sizeof(short) * 2);
                PeekEventKey(out peekIndex, out peekFlags);

                if (!IsValidStreamIndex(peekIndex))
                {
                    // Treat as long record (0xC000 total length)
                    _reader.ReadBytes(0x5000 - sizeof(short) * 2);

                    PeekEventKey(out peekIndex, out peekFlags);
                    Debug.Assert(IsValidStreamIndex(peekIndex)); // Next one should be good
                }
            }
        }

        private XEFEvent ReadDataEvent()
        {
            if (!EndOfStream)
            {
                try
                {
                    short streamIndex, streamFlags;
                    GetEventKey(out streamIndex, out streamFlags);

                    // Get event stream
                    XEFStream eventStream = null;
                    if (_streams.ContainsKey(streamIndex))
                    {
                        eventStream = _streams[streamIndex];
                    }

                    // Read event metadata
                    int dataSize = _reader.ReadInt32();
                    TimeSpan relativeTime = TimeSpan.FromTicks(_reader.ReadInt64());
                    uint unknown = _reader.ReadUInt32();
                    int fullDataSize = _reader.ReadInt32(); // Uncompressed data size

                    // Read tag if needed
                    byte[] tagData = null;
                    int frameIndex = 0;

                    if (eventStream != null)
                    {
                        if (eventStream.TagSize > 0)
                        {
                            tagData = _reader.ReadBytes(eventStream.TagSize);

                            // Treat tag as frame number
                            if (eventStream.TagSize == 4)
                            {
                                frameIndex = BitConverter.ToInt32(tagData, 0);
                            }
                        }
                        Debug.Assert(!eventStream.IsCompressed || fullDataSize != dataSize); // Verify event is valid
                    }
                    else
                    {
                        tagData = _reader.ReadBytes(DataConstants.EVENT_DEFAULT_TAG_SIZE);
                    }

                    // Read event data
                    byte[] eventData = _reader.ReadBytes(dataSize);
                    
                    CurrentEvent = new XEFEvent(eventStream, frameIndex, relativeTime, fullDataSize, tagData, eventData, unknown);
                    return CurrentEvent;
                }
                catch (IOException)
                {
                    EndOfStream = true;
                    StreamError = true;
                }
            }

            return null;
        }

        /// <summary>
        /// Gets the next event from the file.
        /// </summary>
        /// <returns>The next XEFEvent if available; otherwise, null.</returns>
		public XEFEvent GetNextEvent()
        {
            if (!EndOfStream)
            {
                short streamIndex, streamFlags;
                PeekEventKey(out streamIndex, out streamFlags);

                // Check if reached unknown event or footer
                if (streamIndex == DataConstants.EVENT_UNKRECORD_INDEX)
                {
                    ReadUnknownEvent();

                    // Return next event
                    return GetNextEvent();
                }
                else if (streamIndex == _totalReportedStreams + 1)
                {
                    // Reached footer
                    EndOfStream = true;
                    return null;
                }
                else if (!_streams.ContainsKey(streamIndex)) // Check if is stream description event (first event with given index)
                {
                    _streams[streamIndex] = ReadStreamDescription();

                    // Return next event
                    return GetNextEvent();
                }

                return ReadDataEvent();
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
        /// <returns>An IReadOnlyList of all the XEFEvents.</returns>
        public IReadOnlyList<XEFEvent> GetAllEvents()
        {
            // Reset reader
            _reader.BaseStream.Position = _eventStartAddress;

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
        /// <returns>An IReadOnlyList of all the XEFEvents.</returns>
        public IReadOnlyList<XEFEvent> GetAllEvents(Guid streamDataType)
        {
            return GetAllEvents(new Guid[] { streamDataType });
        }

        /// <summary>
        /// Gets all events of specified types from the file.
        /// </summary>
        /// <param name="streamDataTypes">The event type to retrieve.</param>
        /// <returns>An IReadOnlyList of all the XEFEvents.</returns>
        public IReadOnlyList<XEFEvent> GetAllEvents(ICollection<Guid> streamDataTypes)
        {
            // Reset reader
            _reader.BaseStream.Position = _eventStartAddress;

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
