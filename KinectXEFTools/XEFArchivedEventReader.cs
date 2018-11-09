using System;
using System.IO;
using System.Collections.Generic;

namespace KinectXEFTools
{
    public class XEFArchivedEventReader : IDisposable, IEventReader
    {
        //
        //  Data offsets
        //

        private const int STREAM_KEY_COMPRESSION_FLAG = 0x00000100;

        private const int STREAM_ARC_DESC_SIZE = 494; // archived stream description
        private const int STREAM_ARC_UNK5_SIZE = 170; // larger for archived stream

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

        public XEFArchivedEventReader(string path)
        {

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

        public void Close()
        {
            Dispose(true);
        }

        public IEnumerable<XEFEvent> GetAllEvents()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<XEFEvent> GetAllEvents(Guid streamDataType)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<XEFEvent> GetAllEvents(ICollection<Guid> streamDataTypes)
        {
            throw new NotImplementedException();
        }

        public XEFEvent GetNextEvent()
        {
            throw new NotImplementedException();
        }

        public XEFEvent GetNextEvent(Guid streamDataType)
        {
            throw new NotImplementedException();
        }

        public XEFEvent GetNextEvent(ICollection<Guid> streamDataTypes)
        {
            throw new NotImplementedException();
        }
    }
}
