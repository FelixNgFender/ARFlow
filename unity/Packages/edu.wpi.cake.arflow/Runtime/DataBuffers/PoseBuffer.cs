using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using Google.Protobuf.WellKnownTypes;
using UnityEngine;

namespace CakeLab.ARFlow.DataBuffers
{
    using CakeLab.ARFlow.Utilities;
    using Clock;
    using Grpc.V1;
    using Vector3Unity = UnityEngine.Vector3;
    using QuaternionUnity = UnityEngine.Quaternion;
    using Vector3Grpc = Grpc.V1.Vector3;
    using QuaternionGrpc = Grpc.V1.Quaternion;

    public struct RawPoseFrame
    {
        public DateTime DeviceTimestamp;
        public Vector3Unity Forward;
        public Vector3Unity Position;
        public Vector3Unity Right;
        public QuaternionUnity Rotation;
        public Vector3Unity Up;


        public static explicit operator Grpc.V1.PoseFrame(RawPoseFrame rawFrame)
        {
            var PoseFrameGrpc = new Grpc.V1.PoseFrame
            {
                DeviceTimestamp = Timestamp.FromDateTime(rawFrame.DeviceTimestamp),
                Pose = new Pose
                {
                    Position = new Vector3Grpc
                    {
                        X = rawFrame.Position.x,
                        Y = rawFrame.Position.y,
                        Z = rawFrame.Position.z,
                    },
                    Rotation = new QuaternionGrpc
                    {
                        W = rawFrame.Rotation.w,
                        X = rawFrame.Rotation.x,
                        Y = rawFrame.Rotation.y,
                        Z = rawFrame.Rotation.z,
                    },
                    Forward = new Vector3Grpc
                    {
                        X = rawFrame.Forward.x,
                        Y = rawFrame.Forward.y,
                        Z = rawFrame.Forward.z,
                    },
                    Right = new Vector3Grpc
                    {
                        X = rawFrame.Right.x,
                        Y = rawFrame.Right.y,
                        Z = rawFrame.Right.z,
                    },
                    Up = new Vector3Grpc
                    {
                        X = rawFrame.Up.x,
                        Y = rawFrame.Up.y,
                        Z = rawFrame.Up.z,
                    }
                }
            };
            return PoseFrameGrpc;
        }

        public static explicit operator Grpc.V1.ARFrame(RawPoseFrame rawFrame)
        {
            var arFrame = new Grpc.V1.ARFrame { PoseFrame = (Grpc.V1.PoseFrame)rawFrame };
            return arFrame;
        }
    }

    public class PoseBuffer : IARFrameBuffer<RawPoseFrame>
    {
        Transform m_TransformToGetPose;

        IClock m_Clock;

        public IClock Clock
        {
            get => m_Clock;
            set => m_Clock = value;
        }

        private const int m_PoseDataSize = 3 * 4 * sizeof(float);
        private float m_SamplingIntervalMs;
        private bool m_IsCapturing;
        private readonly List<RawPoseFrame> m_Buffer;

        public IReadOnlyList<RawPoseFrame> Buffer => m_Buffer;

        public PoseBuffer(
            int initialBufferSize,
            Transform transformToGetPose,
            IClock clock,
            float samplingIntervalMs = 50
        )
        {
            m_Buffer = new List<RawPoseFrame>(initialBufferSize);
            m_TransformToGetPose = transformToGetPose;
            m_Clock = clock;
            m_SamplingIntervalMs = samplingIntervalMs;
        }

        public void StartCapture()
        {
            if (m_IsCapturing)
            {
                return;
            }
            m_IsCapturing = true;
            CapturePoseAsync();
        }

        public void StopCapture()
        {
            if (!m_IsCapturing)
            {
                return;
            }
            m_IsCapturing = false;
        }

        private async void CapturePoseAsync()
        {
            while (m_IsCapturing)
            {
                await Awaitable.WaitForSecondsAsync(m_SamplingIntervalMs / 1000);
                AddToBuffer(m_Clock.UtcNow);
            }
        }

        private void AddToBuffer(DateTime deviceTimestampAtCapture)
        {
            var newFrame = new RawPoseFrame
            {
                DeviceTimestamp = deviceTimestampAtCapture,
                Forward = m_TransformToGetPose.forward,
                Position = m_TransformToGetPose.position,
                Right = m_TransformToGetPose.right,
                Rotation = m_TransformToGetPose.rotation,
                Up = m_TransformToGetPose.up
            };
            m_Buffer.Add(newFrame);
        }

        public void ClearBuffer()
        {
            m_Buffer.Clear();
        }

        public RawPoseFrame TryAcquireLatestFrame()
        {
            return m_Buffer.LastOrDefault();
        }

        public ARFrame[] GetARFramesFromBuffer()
        {
            return m_Buffer.Select(frame => (ARFrame)frame).ToArray();
        }

        public void Dispose()
        {
            StopCapture();
            ClearBuffer();
        }
    }
}
