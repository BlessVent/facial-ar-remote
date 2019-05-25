﻿using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace Unity.Labs.FacialRemote
{
    /// <summary>
    /// Asset for storing recorded sessions.
    /// </summary>
    [Serializable]
    [CreateAssetMenu(fileName = "PlaybackData", menuName = "AR Face Capture/Playback Data")]
    public class PlaybackData : ScriptableObject
    {
        const int k_MinBufferAmount = 32;
        const int k_BufferCreateAmount = 6;
        const int k_ThreadSleepTime = 1;

        [SerializeField]
        [Tooltip("Individual recorded playback buffers from a streaming source.")]
        PlaybackBuffer[] m_PlaybackBuffers;

        readonly Queue<byte[]> m_BufferQueue = new Queue<byte[]>();
        readonly List<byte[]> m_RecordedBuffers = new List<byte[]>();

        PlaybackBuffer m_ActivePlaybackBuffer;
        int m_CurrentBufferSize = -1;

        public PlaybackBuffer[] playbackBuffers { get { return m_PlaybackBuffers; } }

#if UNITY_EDITOR
        void OnEnable()
        {
            UnityEditor.EditorApplication.playModeStateChanged += PlayModeStateChanged;
        }

        void OnDisable()
        {
            UnityEditor.EditorApplication.playModeStateChanged -= PlayModeStateChanged;
        }

        void PlayModeStateChanged(UnityEditor.PlayModeStateChange state)
        {
            FinishRecording();
        }
#endif

        /// <summary>
        /// Start recording a session using data from the remote device.
        /// </summary>
        /// <param name="streamSettings">The stream settings used for this recording</param>
        /// <param name="take">The take number</param>
        public void StartRecording(IStreamSettings streamSettings, int take)
        {
            var playbackBuffer = new PlaybackBuffer(streamSettings)
            {
                name = string.Format("{0:yyyy_MM_dd_HH_mm}-Take{1:00}", DateTime.Now, take)
            };

            Debug.Log(string.Format("Starting take: {0}", playbackBuffer.name));

            m_ActivePlaybackBuffer = playbackBuffer;

            var bufferSize = streamSettings.bufferSize;
            if (bufferSize != m_CurrentBufferSize)
                m_BufferQueue.Clear();

            m_CurrentBufferSize = bufferSize;
            new Thread(() =>
            {
                while (m_ActivePlaybackBuffer != null)
                {
                    if (m_BufferQueue.Count < k_MinBufferAmount)
                    {
                        for (var i = 0; i < k_BufferCreateAmount; i++)
                        {
                            m_BufferQueue.Enqueue(new byte[m_CurrentBufferSize]);
                        }
                    }

                    Thread.Sleep(k_ThreadSleepTime);
                }
            }).Start();
        }

        public void AddDataToRecording(byte[] buffer, int offset = 0)
        {
            byte[] copyBuffer;
            if (m_BufferQueue.Count < 1)
            {
                Debug.LogWarning("Buffer Queue Empty");
                copyBuffer = new byte[m_CurrentBufferSize];
            }
            else
            {
                copyBuffer = m_BufferQueue.Dequeue();
            }

            Buffer.BlockCopy(buffer, offset, copyBuffer, 0, m_CurrentBufferSize);

            m_RecordedBuffers.Add(copyBuffer);
        }

        public void FinishRecording()
        {
            if (m_ActivePlaybackBuffer == null)
            {
                RecycleRecordedBuffers();
                return;
            }

            var bufferCount = m_RecordedBuffers.Count;
            if (string.IsNullOrEmpty(m_ActivePlaybackBuffer.name) || bufferCount == 0)
            {
                RecycleRecordedBuffers();
                m_ActivePlaybackBuffer = null;
                return;
            }

            var recordStream = new byte[bufferCount * m_CurrentBufferSize];
            m_ActivePlaybackBuffer.recordStream = recordStream;
            for (var i = 0; i < bufferCount; i++)
            {
                var buffer = m_RecordedBuffers[i];
                Buffer.BlockCopy(buffer, 0, recordStream, i * m_CurrentBufferSize, m_CurrentBufferSize);
                m_BufferQueue.Enqueue(buffer);
            }

            var length = m_PlaybackBuffers.Length;
            var buffers = new PlaybackBuffer[length + 1];
            for (var i = 0; i < length; i++)
            {
                buffers[i] = m_PlaybackBuffers[i];
            }

            buffers[length] = m_ActivePlaybackBuffer;
            m_PlaybackBuffers = buffers;

            m_ActivePlaybackBuffer = null;

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        void RecycleRecordedBuffers()
        {
            if (m_RecordedBuffers.Count > 0)
            {
                foreach (var bytes in m_RecordedBuffers)
                {
                    m_BufferQueue.Enqueue(bytes);
                }
            }
        }

        void OnValidate()
        {
            foreach (var playbackBuffer in m_PlaybackBuffers)
            {
                if (playbackBuffer.locations.Length == 0)
                {
                    playbackBuffer.UseDefaultLocations();
                }
            }
        }
    }
}
