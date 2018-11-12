using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KinectXEFTools
{
    public class XEFAudioSubframe
    {
        private const int SPEAKER_TRACKING_IDS_SIZE = 6;
        private const int OUT_BUFFER_SIZE = 256;
        private const int MIC_BUFFER_SIZE = 1024;
        private const int SPK_BUFFER_SIZE = 2048;
        private const int RESERVED_SIZE = 1024;

        public uint SubFrameNumber;
        public uint EventBitField;
        public ulong TimeCounter;
        public int BeamMode;
        public float BeamAngle;
        public float BeamAngleConfidence;
        public uint SpeakerTrackingIdCount;
        public ulong[] SpeakerTrackingIds;
        public float[] OutBuffer;
        public float[] MicBuffer;
        public float[] SpkBuffer;
        public byte[] ReservedByteArray;

        public XEFAudioSubframe()
        {
            SpeakerTrackingIds = new ulong[SPEAKER_TRACKING_IDS_SIZE];
            OutBuffer = new float[OUT_BUFFER_SIZE];
            MicBuffer = new float[MIC_BUFFER_SIZE];
            SpkBuffer = new float[SPK_BUFFER_SIZE];
            ReservedByteArray = new byte[RESERVED_SIZE];
        }

        public static XEFAudioSubframe FromReader(BinaryReader reader)
        {
            XEFAudioSubframe audioSubframe = new XEFAudioSubframe();
            audioSubframe.SubFrameNumber = reader.ReadUInt32();
            audioSubframe.EventBitField = reader.ReadUInt32();
            audioSubframe.TimeCounter = reader.ReadUInt64();
            audioSubframe.BeamMode = reader.ReadInt32();
            audioSubframe.BeamAngle = reader.ReadSingle();
            audioSubframe.BeamAngleConfidence = reader.ReadSingle();
            audioSubframe.SpeakerTrackingIdCount = reader.ReadUInt32();

            for (int j = 0; j < audioSubframe.SpeakerTrackingIds.Length; j++)
            {
                audioSubframe.SpeakerTrackingIds[j] = reader.ReadUInt64();
            }

            for (int j = 0; j < audioSubframe.OutBuffer.Length; j++)
            {
                audioSubframe.OutBuffer[j] = reader.ReadSingle();
            }

            for (int j = 0; j < audioSubframe.MicBuffer.Length; j++)
            {
                audioSubframe.MicBuffer[j] = reader.ReadSingle();
            }

            for (int j = 0; j < audioSubframe.SpkBuffer.Length; j++)
            {
                audioSubframe.SpkBuffer[j] = reader.ReadSingle();
            }

            for (int j = 0; j < audioSubframe.ReservedByteArray.Length; j++)
            {
                audioSubframe.ReservedByteArray[j] = reader.ReadByte();
            }

            return audioSubframe;
        }
    }

    public class XEFAudioFrame
    {
        public uint Version;
        public uint SubFrameCount;
        public uint SubFramesAllocated;
        public uint Reserved;
        public XEFAudioSubframe[] SubFrames;
        
        public static XEFAudioFrame FromByteArray(byte[] data)
        {
            XEFAudioFrame audioData = new XEFAudioFrame();
            using (MemoryStream stream = new MemoryStream(data))
            {
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    audioData.Version = reader.ReadUInt32();
                    audioData.SubFrameCount = reader.ReadUInt32();
                    audioData.SubFramesAllocated = reader.ReadUInt32();
                    audioData.Reserved = reader.ReadUInt32();

                    Debug.Assert(audioData.SubFrameCount == audioData.SubFramesAllocated);
                    audioData.SubFrames = new XEFAudioSubframe[audioData.SubFrameCount];

                    for (int i = 0; i < audioData.SubFrameCount; i++)
                    {
                        audioData.SubFrames[i] = XEFAudioSubframe.FromReader(reader);
                    }
                }
            }
            return audioData;
        }
    }
}
