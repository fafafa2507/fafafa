/*
 * Copyright (c) 2015 Thomas Hourdel
 *
 * This software is provided 'as-is', without any express or implied
 * warranty. In no event will the authors be held liable for any damages
 * arising from the use of this software.
 *
 * Permission is granted to anyone to use this software for any purpose,
 * including commercial applications, and to alter it and redistribute it
 * freely, subject to the following restrictions:
 * 
 *    1. The origin of this software must not be misrepresented; you must not
 *    claim that you wrote the original software. If you use this software
 *    in a product, an acknowledgment in the product documentation would be
 *    appreciated but is not required.
 * 
 *    2. Altered source versions must be plainly marked as such, and must not be
 *    misrepresented as being the original software.
 * 
 *    3. This notice may not be removed or altered from any source
 *    distribution.
 */

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using ThreadPriority = System.Threading.ThreadPriority;

namespace SA.GIF
{
    [RequireComponent(typeof(Camera))]
    [DisallowMultipleComponent]
    sealed class RecorderComponent : MonoBehaviour
    {
        SG_Recorder.RecorderState m_State;

        #region Exposed fields

        // These fields aren't public, the user shouldn't modify them directly as they can't break
        // everything if not used correctly. Use Setup() instead.

        [SerializeField]
        bool m_AutoAspect = true;

        [SerializeField]
        int m_Width = 320;

        [SerializeField]
        int m_Height = 200;

        [SerializeField]
        [Range(1, 30)]
        int m_FramePerSecond = 15;

        [SerializeField]
        int m_Repeat = 0;

        [SerializeField]
        [Range(1, 100)]
        int m_Quality = 15;

        [SerializeField]
        float m_BufferSize = 3f;

        #endregion

        #region Public fields

        /// <summary>
        /// The folder to save the gif to. No trailing slash.
        /// </summary>
        public string SaveFolder { get; set; }

        /// <summary>
        /// Sets the worker threads priority. This will only affect newly created threads (on save).
        /// </summary>
        public ThreadPriority WorkerPriority = ThreadPriority.BelowNormal;

        /// <summary>
        /// Returns the estimated VRam used (in MB) for recording.
        /// </summary>
        public float EstimatedMemoryUse
        {
            get
            {
                var mem = m_FramePerSecond * m_BufferSize;
                mem *= m_Width * m_Height * 4;
                mem /= 1024 * 1024;
                return mem;
            }
        }

        #endregion

        #region Delegates

        /// <summary>
        /// Called when the pre-processing step has finished.
        /// </summary>
        public event Action OnPreProcessingDone = delegate { };

        /// <summary>
        /// Called by each worker thread every time a frame is processed during the save process.
        /// The first parameter holds the worker ID and the second one a value in range [0;1] for
        /// the actual progress. This callback is probably not thread-safe, use at your own risks.
        /// </summary>
        public event Action<int, float> OnFileSaveProgress = delegate { };

        /// <summary>
        /// Called once a gif file has been saved. The first parameter will hold the worker ID and
        /// the second one the absolute file path.
        /// </summary>
        public event Action<int, string> OnFileSaved = delegate { };

        /// <summary>
        /// Called once a gif recorder state is changed.
        /// </summary>
        public event Action<SG_Recorder.RecorderState> OnRecorderStateChanged = delegate { };

        #endregion

        #region Internal fields

        int m_MaxFrameCount;
        float m_Time;
        float m_TimePerFrame;
        Queue<RenderTexture> m_Frames;
        RenderTexture m_RecycledRenderTexture;
        ReflectionUtils<RecorderComponent> m_ReflectionUtils;

        int id = 0;
        float progress = 0.0f;
        string filePath = string.Empty;
        bool invokeFileProgress = false;
        bool invokeFileSaved = false;

        #endregion

        #region Public API

        public int FPS => m_FramePerSecond;

        /// <summary>
        /// Current state of the recorder.
        /// </summary>
        public SG_Recorder.RecorderState State
        {
            get => m_State;
            private set
            {
                m_State = value;
                OnRecorderStateChanged.Invoke(m_State);
            }
        }

        public Queue<RenderTexture> Frames => m_Frames;

        /// <summary>
        /// Initializes the component. Use this if you need to change the recorder settings in a script.
        /// This will flush the previously saved frames as settings can't be changed while recording.
        /// </summary>
        /// <param name="autoAspect">Automatically compute height from the current aspect ratio</param>
        /// <param name="width">Width in pixels</param>
        /// <param name="height">Height in pixels</param>
        /// <param name="fps">Recording FPS</param>
        /// <param name="bufferSize">Maximum amount of seconds to record to memory</param>
        /// <param name="repeat">-1: no repeat, 0: infinite, >0: repeat count</param>
        /// <param name="quality">Quality of color quantization (conversion of images to the maximum
        /// 256 colors allowed by the GIF specification). Lower values (minimum = 1) produce better
        /// colors, but slow processing significantly. Higher values will speed up the quantization
        /// pass at the cost of lower image quality (maximum = 100).</param>
        public void Setup(bool autoAspect, int width, int height, int fps, float bufferSize, int repeat, int quality)
        {
            if (State == SG_Recorder.RecorderState.PreProcessing)
            {
                Debug.LogWarning("Attempting to setup the component during the pre-processing step.");
                return;
            }

            // Start fresh
            FlushMemory();

            // Set values and validate them
            m_AutoAspect = autoAspect;
            m_ReflectionUtils.ConstrainMin(x => x.m_Width, width);

            if (autoAspect)
                m_ReflectionUtils.ConstrainMin(x => x.m_Height, Mathf.RoundToInt(m_Width / Camera.main.aspect));
            else
                m_ReflectionUtils.ConstrainMin(x => x.m_Height, height);

            m_ReflectionUtils.ConstrainRange(x => x.m_FramePerSecond, fps);
            m_ReflectionUtils.ConstrainMin(x => x.m_BufferSize, bufferSize);
            m_ReflectionUtils.ConstrainMin(x => x.m_Repeat, repeat);
            m_ReflectionUtils.ConstrainRange(x => x.m_Quality, quality);

            // Ready to go
            Init();
        }

        /// <summary>
        /// Pauses recording.
        /// </summary>
        public void Pause()
        {
            if (State == SG_Recorder.RecorderState.PreProcessing)
            {
                Debug.LogWarning("Attempting to pause recording during the pre-processing step. The recorder is automatically paused when pre-processing.");
                return;
            }
            else if (State == SG_Recorder.RecorderState.Stopped)
            {
                Debug.LogWarning("Attempting to resume recording after it has been stopped.");
                return;
            }

            State = SG_Recorder.RecorderState.Paused;
        }

        /// <summary>
        /// Starts or resumes recording. You can't resume while it's pre-processing data to be saved.
        /// </summary>
        public void Record()
        {
            if (State == SG_Recorder.RecorderState.PreProcessing)
            {
                Debug.LogWarning("Attempting to resume recording during the pre-processing step.");
                return;
            }
            else if (State == SG_Recorder.RecorderState.Stopped)
            {
                Debug.LogWarning("Attempting to resume recording after it has been stopped.");
                return;
            }

            State = SG_Recorder.RecorderState.Recording;
        }

        /// <summary>
        /// Stops the recording. You can't resume the record after it has been stopped.
        /// </summary>
        public void Stop()
        {
            State = SG_Recorder.RecorderState.Stopped;
        }

        /// <summary>
        /// Clears all saved frames from memory and starts fresh.
        /// </summary>
        public void FlushMemory()
        {
            if (State == SG_Recorder.RecorderState.PreProcessing)
            {
                Debug.LogWarning("Attempting to flush memory during the pre-processing step.");
                return;
            }

            Init();

            if (m_RecycledRenderTexture != null)
                Flush(m_RecycledRenderTexture);

            if (m_Frames == null)
                return;

            foreach (var rt in m_Frames)
                Flush(rt);

            m_Frames.Clear();
        }

        /// <summary>
        /// Saves the stored frames to a gif file. The filename will automatically be generated.
        /// Recording will be paused and won't resume automatically. You can use the 
        /// <code>OnPreProcessingDone</code> callback to be notified when the pre-processing
        /// step has finished.
        /// </summary>
        public void Save()
        {
            Save(GenerateFileName());
        }

        /// <summary>
        /// Saves the stored frames to a gif file. If the filename is null or empty, an unique one
        /// will be generated. You don't need to add the .gif extension to the name. Recording will
        /// be paused and won't resume automatically. You can use the <code>OnPreProcessingDone</code>
        /// callback to be notified when the pre-processing step has finished.
        /// </summary>
        /// <param name="filename">File name without extension</param>
        public void Save(string filename)
        {
            if (State == SG_Recorder.RecorderState.PreProcessing)
            {
                Debug.LogWarning("Attempting to save during the pre-processing step.");
                return;
            }

            if (m_Frames.Count == 0)
            {
                Debug.LogWarning("Nothing to save. Maybe you forgot to start the recorder ?");
                return;
            }

            State = SG_Recorder.RecorderState.PreProcessing;

            if (string.IsNullOrEmpty(filename))
                filename = GenerateFileName();

            StartCoroutine(PreProcess(filename));
        }

        #endregion

        #region Unity events

        void Awake()
        {
            m_ReflectionUtils = new ReflectionUtils<RecorderComponent>(this);
            m_Frames = new Queue<RenderTexture>();
            Init();
        }

        void Update()
        {
            if (invokeFileProgress)
            {
                invokeFileProgress = false;
                OnFileSaveProgress(id, progress);
            }

            if (invokeFileSaved)
            {
                invokeFileSaved = false;
                OnFileSaved(id, filePath);
            }
        }

        void OnDestroy()
        {
            FlushMemory();
        }

        void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (State != SG_Recorder.RecorderState.Recording)
            {
                Graphics.Blit(source, destination);
                return;
            }

            m_Time += Time.unscaledDeltaTime;

            if (m_Time >= m_TimePerFrame)
            {
                // Limit the amount of frames stored in memory
                if (m_Frames.Count >= m_MaxFrameCount)
                    m_RecycledRenderTexture = m_Frames.Dequeue();

                m_Time -= m_TimePerFrame;

                // Frame data
                var rt = m_RecycledRenderTexture;
                m_RecycledRenderTexture = null;

                if (rt == null)
                {
                    rt = new RenderTexture(m_Width, m_Height, 0, RenderTextureFormat.ARGB32);
                    rt.wrapMode = TextureWrapMode.Clamp;
                    rt.filterMode = FilterMode.Bilinear;
                    rt.anisoLevel = 0;
                }

                Graphics.Blit(source, rt);
                m_Frames.Enqueue(rt);
            }

            Graphics.Blit(source, destination);
        }

        #endregion

        #region Methods

        // Used to reset internal values, called on Start(), Setup() and FlushMemory()
        void Init()
        {
            State = SG_Recorder.RecorderState.Paused;
            ComputeHeight();
            m_MaxFrameCount = Mathf.RoundToInt(m_BufferSize * m_FramePerSecond);
            m_TimePerFrame = 1f / m_FramePerSecond;
            m_Time = 0f;

            // Make sure the output folder is set or use the default one
            if (string.IsNullOrEmpty(SaveFolder))
            {
#if UNITY_EDITOR
                SaveFolder = Application.dataPath; // Defaults to the asset folder in the editor for faster access to the gif file
#else
				SaveFolder = Application.persistentDataPath;
#endif
            }
        }

        // Automatically computes height from the current aspect ratio if auto aspect is set to true
        public void ComputeHeight()
        {
            if (!m_AutoAspect)
                return;

            if (Camera.main != null) m_Height = Mathf.RoundToInt(m_Width / Camera.main.aspect);
        }

        // Flushes a single Texture object
        void Flush(Texture texture)
        {
#if UNITY_EDITOR
            DestroyImmediate(texture);
#else
			Texture2D.Destroy(texture);
#endif
        }

        // Gets a filename : GifCapture-yyyyMMddHHmmssffff
        string GenerateFileName()
        {
            var timestamp = DateTime.Now.ToString("yyyyMMddHHmmssffff");
            return "GifCapture-" + timestamp;
        }

        // Pre-processing coroutine to extract frame data and send everything to a separate worker thread
        IEnumerator PreProcess(string filename)
        {
            var filepath = SaveFolder + "/" + filename + ".gif";
            var frames = new List<Frame>(m_Frames.Count);

            // Get a temporary texture to read RenderTexture data
            var temp = new Texture2D(m_Width, m_Height, TextureFormat.RGB24, false);
            temp.hideFlags = HideFlags.HideAndDontSave;
            temp.wrapMode = TextureWrapMode.Clamp;
            temp.filterMode = FilterMode.Bilinear;
            temp.anisoLevel = 0;

            // Process the frame queue
            var textures = m_Frames.ToArray();
            foreach (var rt in textures)
            {
                var frame = ToGifFrame(rt, temp);
                frames.Add(frame);
                yield return null;
            }

            // Dispose the temporary texture
            Flush(temp);

            // Switch the state to pause, let the user choose to keep recording or not
            State = SG_Recorder.RecorderState.Paused;

            // Callback
            if (OnPreProcessingDone != null)
                OnPreProcessingDone();

            // Setup a worker thread and let it do its magic
            var encoder = new Encoder(m_Repeat, m_Quality);
            encoder.SetDelay(Mathf.RoundToInt(m_TimePerFrame * 1000f));
            var worker = new Worker(WorkerPriority)
            {
                m_Encoder = encoder,
                m_Frames = frames,
                m_FilePath = filepath,
                m_OnFileSaved = FileSaved,
                m_OnFileSaveProgress = FileSaveProgress
            };
            worker.Start();
        }

        void FileSaved(int id, string path)
        {
            this.id = id;
            filePath = path;
            invokeFileSaved = true;
        }

        void FileSaveProgress(int id, float progress)
        {
            this.id = id;
            this.progress = progress;
            invokeFileProgress = true;
        }

        // Converts a RenderTexture to a GifFrame
        // Should be fast enough for low-res textures but will tank the framerate at higher res
        Frame ToGifFrame(RenderTexture source, Texture2D target)
        {
            // TODO: Experiment with Compute Shaders, it may be faster to return data from a ComputeBuffer
            // than ReadPixels

            RenderTexture.active = source;
            target.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
            target.Apply();
            RenderTexture.active = null;

            return new Frame() { Width = target.width, Height = target.height, Data = target.GetPixels32() };
        }

        #endregion
    }
}
