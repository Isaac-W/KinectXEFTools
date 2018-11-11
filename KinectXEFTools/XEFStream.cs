using System;

namespace KinectXEFTools
{
    public class XEFStream
    {
        public XEFStream(short index, short flags, int tagSize, string dataTypeName, Guid dataTypeId, Guid semanticId)
        {
            EventCount = 0;
            StreamIndex = index;
            StreamFlags = flags;
            DataTypeName = dataTypeName;
            TagSize = tagSize;
            DataTypeId = dataTypeId;
            SemanticId = semanticId;
        }

        public int EventCount { get; private set; }

        public short StreamIndex { get; private set; }

        public short StreamFlags { get; private set; }

        public string Name { get { return DataTypeName; } }

        public int TagSize { get; private set; }

        public string DataTypeName { get; private set; }

        public Guid DataTypeId { get; private set; }

        public Guid SemanticId { get; private set; }

        public bool IsCompressed { get { return (StreamFlags & DataConstants.FLAG_COMPRESSED) == DataConstants.FLAG_COMPRESSED; } }

        public void IncrementEventCount()
        {
            EventCount++;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
