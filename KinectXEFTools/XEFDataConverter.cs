using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KinectXEFTools
{
    class XEFDataConverter : IDisposable
    {
        //
        //  Members
        //

        private string _path;
        private string _basepath;

        private XEFEventReader _reader;

        //
        //  Constructor
        //

        public XEFDataConverter(string path)
        {
            _path = path;
            _basepath = Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path));
            _reader = new XEFEventReader(path);
        }

        //
        //  Properties
        //

        public bool ConvertVideo { get; set; }
        public bool ConvertSkeleton { get; set; }
        public bool ConvertDepth { get; set; }
        public bool ResumeConversion { get; set; }

        //
        //	IDisposable
        //

        private bool disposed = false;

        public void Dispose()
        {
            Dispose(true);
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
    }
}
