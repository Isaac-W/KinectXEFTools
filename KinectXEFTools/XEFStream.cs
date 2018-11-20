using System;
using System.Collections.Generic;

namespace KinectXEFTools
{
    public class XEFStream
    {
        private int _eventsCount;

        public XEFStream(short index, short flags, int tagSize, string dataTypeName, Guid dataTypeId, Guid semanticId)
        {
            _eventsCount = 0;
            StreamIndex = index;
            StreamFlags = flags;
            DataTypeName = dataTypeName;
            TagSize = tagSize;
            DataTypeId = dataTypeId;
            SemanticId = semanticId;
        }

        public int EventCount { get { return _eventsCount; } }

        public short StreamIndex { get; private set; }

        public short StreamFlags { get; private set; }

        public string Name { get { return DataTypeName; } }

        public int TagSize { get; private set; }

        public string DataTypeName { get; private set; }

        public Guid DataTypeId { get; private set; }

        public Guid SemanticId { get; private set; }

        public bool IsCompressed { get { return (StreamFlags & DataConstants.FLAG_COMPRESSED) == DataConstants.FLAG_COMPRESSED; } }

        public void AddEvent(XEFEvent xefEvent)
        {
            // TODO Not storing events because it loads too much data into memory
            _eventsCount++;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
