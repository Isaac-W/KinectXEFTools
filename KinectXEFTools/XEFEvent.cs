using System;
using System.IO;
using System.IO.Compression;

namespace KinectXEFTools
{
    public class XEFEvent
    {
        public XEFEvent(XEFStream eventStream, int frameIndex, TimeSpan relativeTime, int eventDataSize, byte[] tagData, byte[] eventData, uint unknown)
        {
            EventStream = eventStream;
            FrameIndex = frameIndex;
            RelativeTime = relativeTime;
            TagData = tagData;
            EventDataSize = eventDataSize;
            RawEventData = eventData;
            Unknown = unknown;

            if (EventStream != null)
            {
                EventIndex = EventStream.EventCount;
                EventStream.AddEvent(this); // Need to tell stream that an event has been added
            }
        }

        public XEFStream EventStream { get; private set; }

        public int EventIndex { get; private set; }

        public int FrameIndex { get; private set; }

        public TimeSpan RelativeTime { get; private set; }

        public int TagDataSize { get { return EventStream == null ? DataConstants.EVENT_DEFAULT_TAG_SIZE : EventStream.TagSize; } }

        public int EventDataSize { get; private set; }

        public Guid EventStreamSemanticId { get { return EventStream == null ? Guid.Empty : EventStream.SemanticId; } }

        public Guid EventStreamDataTypeId { get { return EventStream == null ? Guid.Empty : EventStream.DataTypeId; } }

        public byte[] TagData { get; private set; }

        private byte[] _cachedData = null;
        public byte[] EventData
        {
            get
            {
                if (IsCompressed)
                {
                    if (_cachedData == null)
                    {
                        _cachedData = new byte[EventDataSize];

                        // Decompress event data (remove zlib 0x7801 header -- unknown if other compression used)
                        using (MemoryStream stream = new MemoryStream(RawEventData, 2, RawEventData.Length - 2))
                        {
                            using (DeflateStream deflate = new DeflateStream(stream, CompressionMode.Decompress))
                            {
                                deflate.Read(_cachedData, 0, EventDataSize);
                            }
                        }
                    }

                    return _cachedData;
                }
                else
                {
                    return RawEventData;
                }
            }
        }

        public byte[] RawEventData { get; private set; }

        public bool IsCompressed { get { return EventStream == null ? false : EventStream.IsCompressed; } }

        public uint Unknown { get; private set; } // TODO Unknown data found in event (could be some index or id)

        public override string ToString()
        {
            return EventDataSize.ToString();
        }
    }
}
