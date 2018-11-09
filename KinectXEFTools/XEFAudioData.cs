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
    }

    public class XEFAudioData
    {
        public uint Version;
        public uint SubFrameCount;
        public uint SubFramesAllocated;
        public uint Reserved;
        public XEFAudioSubframe[] SubFrames;

        public XEFAudioData()
        {
        }

        public static XEFAudioData FromByteArray(byte[] data)
        {
            XEFAudioData audioData = new XEFAudioData();
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
                        audioData.SubFrames[i] = new XEFAudioSubframe();
                        audioData.SubFrames[i].SubFrameNumber = reader.ReadUInt32();
                        audioData.SubFrames[i].EventBitField = reader.ReadUInt32();
                        audioData.SubFrames[i].TimeCounter = reader.ReadUInt64();
                        audioData.SubFrames[i].BeamMode = reader.ReadInt32();
                        audioData.SubFrames[i].BeamAngle = reader.ReadSingle();
                        audioData.SubFrames[i].BeamAngleConfidence = reader.ReadSingle();
                        audioData.SubFrames[i].SpeakerTrackingIdCount = reader.ReadUInt32();

                        for (int j = 0; j < audioData.SubFrames[i].SpeakerTrackingIds.Length; j++)
                        {
                            audioData.SubFrames[i].SpeakerTrackingIds[j] = reader.ReadUInt64();
                        }

                        for (int j = 0; j < audioData.SubFrames[i].OutBuffer.Length; j++)
                        {
                            audioData.SubFrames[i].OutBuffer[j] = reader.ReadSingle();
                        }

                        for (int j = 0; j < audioData.SubFrames[i].MicBuffer.Length; j++)
                        {
                            audioData.SubFrames[i].MicBuffer[j] = reader.ReadSingle();
                        }

                        for (int j = 0; j < audioData.SubFrames[i].SpkBuffer.Length; j++)
                        {
                            audioData.SubFrames[i].SpkBuffer[j] = reader.ReadSingle();
                        }

                        for (int j = 0; j < audioData.SubFrames[i].ReservedByteArray.Length; j++)
                        {
                            audioData.SubFrames[i].ReservedByteArray[j] = reader.ReadByte();
                        }
                    }
                }
            }
            return audioData;
        }
    }
}
