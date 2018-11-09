using System;

namespace KinectXEFTools
{
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
}
