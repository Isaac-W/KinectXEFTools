using KinectXEFTools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace XEFExtract
{
    class XEFBodyWriter : IXEFDataWriter, IDisposable
    {
        //
        //  Members
        //

        private StreamWriter _writer;

        private bool _seenEvent = false;

        //
        //  Properties
        //

        public string FilePath { get; private set; }

        public long EventCount { get; private set; }

        public TimeSpan StartTime { get; private set; }

        public TimeSpan EndTime { get; private set; }

        public TimeSpan Duration { get { return EndTime - StartTime; } }

        //
        //  Constructor
        //

        public XEFBodyWriter(string path)
        {
            FilePath = path;
            EventCount = 0;
            StartTime = TimeSpan.Zero;
            EndTime = TimeSpan.Zero;

            _writer = new StreamWriter(path);

            WriteHeaders();
        }

        ~XEFBodyWriter()
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
                    _writer.Dispose();
                }

                disposed = true;
            }
        }

        //
        //  Methods
        //

        private void WriteHeaders()
        {
            _writer.WriteLine("EventIndex,Time,SkeletonId,HandLeftConfidence,HandLeftState,HandRightConfidence,HandRightState,Joint,Status,PositionX,PositionY,PositionZ,RotationW,RotationX,RotationY,RotationZ,Joint,Status,PositionX,PositionY,PositionZ,RotationW,RotationX,RotationY,RotationZ,Joint,Status,PositionX,PositionY,PositionZ,RotationW,RotationX,RotationY,RotationZ,Joint,Status,PositionX,PositionY,PositionZ,RotationW,RotationX,RotationY,RotationZ,Joint,Status,PositionX,PositionY,PositionZ,RotationW,RotationX,RotationY,RotationZ,Joint,Status,PositionX,PositionY,PositionZ,RotationW,RotationX,RotationY,RotationZ,Joint,Status,PositionX,PositionY,PositionZ,RotationW,RotationX,RotationY,RotationZ,Joint,Status,PositionX,PositionY,PositionZ,RotationW,RotationX,RotationY,RotationZ,Joint,Status,PositionX,PositionY,PositionZ,RotationW,RotationX,RotationY,RotationZ,Joint,Status,PositionX,PositionY,PositionZ,RotationW,RotationX,RotationY,RotationZ,Joint,Status,PositionX,PositionY,PositionZ,RotationW,RotationX,RotationY,RotationZ,Joint,Status,PositionX,PositionY,PositionZ,RotationW,RotationX,RotationY,RotationZ,Joint,Status,PositionX,PositionY,PositionZ,RotationW,RotationX,RotationY,RotationZ,Joint,Status,PositionX,PositionY,PositionZ,RotationW,RotationX,RotationY,RotationZ,Joint,Status,PositionX,PositionY,PositionZ,RotationW,RotationX,RotationY,RotationZ,Joint,Status,PositionX,PositionY,PositionZ,RotationW,RotationX,RotationY,RotationZ,Joint,Status,PositionX,PositionY,PositionZ,RotationW,RotationX,RotationY,RotationZ,Joint,Status,PositionX,PositionY,PositionZ,RotationW,RotationX,RotationY,RotationZ,Joint,Status,PositionX,PositionY,PositionZ,RotationW,RotationX,RotationY,RotationZ,Joint,Status,PositionX,PositionY,PositionZ,RotationW,RotationX,RotationY,RotationZ,Joint,Status,PositionX,PositionY,PositionZ,RotationW,RotationX,RotationY,RotationZ,Joint,Status,PositionX,PositionY,PositionZ,RotationW,RotationX,RotationY,RotationZ,Joint,Status,PositionX,PositionY,PositionZ,RotationW,RotationX,RotationY,RotationZ,Joint,Status,PositionX,PositionY,PositionZ,RotationW,RotationX,RotationY,RotationZ,Joint,Status,PositionX,PositionY,PositionZ,RotationW,RotationX,RotationY,RotationZ");
        }

        public void Close()
        {
            Dispose(true);
        }

        public void ProcessEvent(XEFEvent ev)
        {
            if (ev.EventStreamDataTypeId != StreamDataTypeIds.Body)
            {
                return;
            }

            // Update start/end time
            if (!_seenEvent)
            {
                StartTime = ev.RelativeTime;
                _seenEvent = true;
            }
            EndTime = ev.RelativeTime;

            // Get raw body data
            XEFBodyFrame bodyFrame = XEFBodyFrame.FromByteArray(ev.EventData);

            for (int i = 0; i < bodyFrame.BodyData.Length; i++)
            {
                XEFBodyData body = bodyFrame.BodyData[i];
                if (body.TrackingState == XEFBodyTrackingState.TRACKED)
                {
                    // Write skeleton body
                    _writer.Write("{0},{1},{2}",
                        ev.EventIndex,
                        ev.RelativeTime.Ticks,
                        body.TrackingID);

                    _writer.Write(",{0},{1}",
                        body.HandDataLeft.HandConfidence,
                        body.HandDataLeft.HandState);

                    _writer.Write(",{0},{1}",
                        body.HandDataRight.HandConfidence,
                        body.HandDataRight.HandState);

                    // Enumerate all joints
                    foreach (XEFJointType jointType in Enum.GetValues(typeof(XEFJointType)))
                    {
                        // Write skeleton joint
                        XEFTrackingState jointTrackingState = body.SkeletonJointPositionTrackingStates[jointType];
                        XEFVector jointPosition = body.SkeletonJointPositions[jointType];
                        XEFVector jointOrientation = body.SkeletonJointOrientations[jointType];

                        _writer.Write(",{0},{1},{2},{3},{4}",
                            jointType,
                            jointTrackingState,
                            jointPosition.x,
                            jointPosition.y,
                            jointPosition.z);

                        _writer.Write(",{0},{1},{2},{3}",
                            jointOrientation.w,
                            jointOrientation.x,
                            jointOrientation.y,
                            jointOrientation.z);
                    }

                    _writer.WriteLine();
                }
            }

            EventCount++;
        }
    }
}
