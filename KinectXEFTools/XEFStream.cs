using System;

namespace KinectXEFTools
{
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
}
