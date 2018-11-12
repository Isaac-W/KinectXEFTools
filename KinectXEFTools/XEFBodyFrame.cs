using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace KinectXEFTools
{
    public class XEFVector
    {
        public float x;
        public float y;
        public float z;
        public float w;

        public static XEFVector FromReader(BinaryReader reader)
        {
            XEFVector vector = new XEFVector();
            vector.x = reader.ReadSingle();
            vector.y = reader.ReadSingle();
            vector.z = reader.ReadSingle();
            vector.w = reader.ReadSingle();
            return vector;
        }
    }

    public enum XEFTrackingState
    {
        NOT_TRACKED = 0,
        INFERRED,
        TRACKED
    }
    
    public enum XEFJointType
    {
        SpineBase = 0,
        SpineMid,
        Neck,
        Head,
        ShoulderLeft,
        ElbowLeft,
        WristLeft,
        HandLeft,
        ShoulderRight,
        ElbowRight,
        WristRight,
        HandRight,
        HipLeft,
        KneeLeft,
        AnkleLeft,
        FootLeft,
        HipRight,
        KneeRight,
        AnkleRight,
        FootRight,
        SpineShoulder,
        HandTipLeft,
        ThumbLeft,
        HandTipRight,
        ThumbRight
    }

    public enum XEFExpressionConfidenceLevel
    {
        NONE = 0,
        LOW,
        HIGH,
        UNKNOWN
    }

    public class XEFExpressionData
    {
        public XEFExpressionConfidenceLevel Face_Neutral;
        public XEFExpressionConfidenceLevel Face_Happy;

        public XEFExpressionConfidenceLevel Activity_Talking;
        public XEFExpressionConfidenceLevel Activity_EyeLeftClosed;
        public XEFExpressionConfidenceLevel Activity_EyeRightClosed;
        public XEFExpressionConfidenceLevel Activity_MouthOpen;
        public XEFExpressionConfidenceLevel Activity_MouthMoved;
        public XEFExpressionConfidenceLevel Activity_LookingAway;

        public XEFExpressionConfidenceLevel Engagement_Engaged;

        public XEFExpressionConfidenceLevel Appearance_WearingGlasses;

        public XEFTrackingState TrackingState;

        public static XEFExpressionData FromReader(BinaryReader reader)
        {
            XEFExpressionData expressionData = new XEFExpressionData();

            expressionData.Face_Neutral = (XEFExpressionConfidenceLevel)reader.ReadInt32();
            expressionData.Face_Happy = (XEFExpressionConfidenceLevel)reader.ReadInt32();

            expressionData.Activity_Talking = (XEFExpressionConfidenceLevel)reader.ReadInt32();
            expressionData.Activity_EyeLeftClosed = (XEFExpressionConfidenceLevel)reader.ReadInt32();
            expressionData.Activity_EyeRightClosed = (XEFExpressionConfidenceLevel)reader.ReadInt32();
            expressionData.Activity_MouthOpen = (XEFExpressionConfidenceLevel)reader.ReadInt32();
            expressionData.Activity_MouthMoved = (XEFExpressionConfidenceLevel)reader.ReadInt32();
            expressionData.Activity_LookingAway = (XEFExpressionConfidenceLevel)reader.ReadInt32();
            reader.ReadInt32(); // Extra unknown expressions
            reader.ReadInt32();
            reader.ReadInt32();
            reader.ReadInt32();

            expressionData.Engagement_Engaged = (XEFExpressionConfidenceLevel)reader.ReadInt32();

            expressionData.Appearance_WearingGlasses = (XEFExpressionConfidenceLevel)reader.ReadInt32();
            reader.ReadInt32(); // Extra unknown appearances
            reader.ReadInt32();
            reader.ReadInt32();
            reader.ReadInt32();
            reader.ReadInt32();

            expressionData.TrackingState = (XEFTrackingState)reader.ReadInt32();

            return expressionData;
        }
    }

    public enum XEFHandState
    {
        OPEN = 0,
        CLOSED,
        LASSO,
        UNKNOWN
    }

    public class XEFHandData
    {
        public XEFHandState HandState;
        public XEFTrackingState HandTrackingState;
        public XEFTrackingState HandConfidence;

        public static XEFHandData FromReader(BinaryReader reader)
        {
            XEFHandData handData = new XEFHandData();
            handData.HandState = (XEFHandState)reader.ReadInt32();
            handData.HandTrackingState = (XEFTrackingState)reader.ReadInt32();
            handData.HandConfidence = (XEFTrackingState)reader.ReadInt32();
            return handData;
        }
    }

    public class XEFLeanData
    {
        public float X;
        public float Y;
        public uint Reserved;
        public XEFTrackingState LeanTrackingState;

        public static XEFLeanData FromReader(BinaryReader reader)
        {
            XEFLeanData leanData = new XEFLeanData();
            leanData.X = reader.ReadSingle();
            leanData.Y = reader.ReadSingle();
            leanData.Reserved = reader.ReadUInt32();
            leanData.LeanTrackingState = (XEFTrackingState)reader.ReadInt32();
            return leanData;
        }
    }

    public enum XEFBodyTrackingState
    {
        NOT_TRACKED = 0,
        TRACKED
    }

    public class XEFBodyData
    {
        public Dictionary<XEFJointType, XEFVector> SkeletonJointPositions;
        public Dictionary<XEFJointType, XEFVector> SkeletonJointOrientations;
        public Dictionary<XEFJointType, XEFTrackingState> SkeletonJointPositionTrackingStates;

        public XEFExpressionData ExpressionData;

        public XEFHandData HandDataLeft;
        public XEFHandData HandDataRight;

        public ulong TrackingID;
        public XEFBodyTrackingState TrackingState;

        public XEFLeanData LeanData;

        public uint QualityFlags;

        public XEFBodyData()
        {
            SkeletonJointPositions = new Dictionary<XEFJointType, XEFVector>();
            SkeletonJointOrientations = new Dictionary<XEFJointType, XEFVector>();
            SkeletonJointPositionTrackingStates = new Dictionary<XEFJointType, XEFTrackingState>();
        }

        public static XEFBodyData FromReader(BinaryReader reader)
        {
            XEFBodyData bodyData = new XEFBodyData();

            foreach (XEFJointType jointType in Enum.GetValues(typeof(XEFJointType)))
            {
                bodyData.SkeletonJointPositions[jointType] = XEFVector.FromReader(reader);
            }

            foreach (XEFJointType jointType in Enum.GetValues(typeof(XEFJointType)))
            {
                bodyData.SkeletonJointOrientations[jointType] = XEFVector.FromReader(reader);
            }

            foreach (XEFJointType jointType in Enum.GetValues(typeof(XEFJointType)))
            {
                bodyData.SkeletonJointPositionTrackingStates[jointType] = (XEFTrackingState)reader.ReadInt32();
            }

            bodyData.ExpressionData = XEFExpressionData.FromReader(reader);

            bodyData.HandDataLeft = XEFHandData.FromReader(reader);
            bodyData.HandDataRight = XEFHandData.FromReader(reader);

            reader.ReadInt32(); // padding

            bodyData.TrackingID = reader.ReadUInt64();
            bodyData.TrackingState = (XEFBodyTrackingState)reader.ReadInt32();

            bodyData.LeanData = XEFLeanData.FromReader(reader);

            bodyData.QualityFlags = reader.ReadUInt32();

            return bodyData;
        }
    }

    public class XEFBodyFrame
    {
        private const int BODY_DATA_SIZE = 6;

        public XEFVector FloorClipPlane;
        public XEFVector Up;
        public XEFBodyData[] BodyData;
        public uint QualityFlags;

        public XEFBodyFrame()
        {
            BodyData = new XEFBodyData[BODY_DATA_SIZE];
        }

        public static XEFBodyFrame FromByteArray(byte[] data)
        {
            XEFBodyFrame bodyData = new XEFBodyFrame();
            using (MemoryStream stream = new MemoryStream(data))
            {
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    bodyData.FloorClipPlane = XEFVector.FromReader(reader);
                    bodyData.Up = XEFVector.FromReader(reader);

                    for (int i = 0; i < BODY_DATA_SIZE; i++)
                    {
                        bodyData.BodyData[i] = XEFBodyData.FromReader(reader);
                    }

                    bodyData.QualityFlags = reader.ReadUInt32();
                    reader.ReadInt32(); // Padding
                    reader.ReadInt32();
                    reader.ReadInt32();
                }
            }
            return bodyData;
        }
    }
}
