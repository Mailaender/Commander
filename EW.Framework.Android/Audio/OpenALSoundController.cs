using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using EW.Framework.Mobile;
using EW.Framework;
#if ANDROID
using System.Globalization;
using Android.Content.PM;
using Android.Content;
using Android.Media;
#endif

#if IOS
using AudioToolbox;
using AVFoundation;
#endif

namespace EW.Framework.Audio
{
    internal static class ALHelper
    {
        [System.Diagnostics.Conditional("DEBUG")]
        [System.Diagnostics.DebuggerHidden]
        internal static void CheckError(string message = "", params object[] args)
        {
            ALError error;
            if ((error = AL.GetError()) != ALError.NoError)
            {
                if (args != null && args.Length > 0)
                    message = String.Format(message, args);
                
                //throw new InvalidOperationException(message + " (Reason: " + AL.GetErrorString(error) + ")");
            }
        }

        //public static bool IsStereoFormat(ALFormat format)
        //{
        //    return (format == ALFormat.Stereo8
        //        || format == ALFormat.Stereo16
        //        || format == ALFormat.StereoFloat32
        //        || format == ALFormat.StereoIma4
        //        || format == ALFormat.StereoMSAdpcm);
        //}
    }

    internal static class AlcHelper
    {
        [System.Diagnostics.Conditional("DEBUG")]
        [System.Diagnostics.DebuggerHidden]
        internal static void CheckError(string message = "", params object[] args)
        {
            AlcError error;
            if ((error = Alc.GetError()) != AlcError.NoError)
            {
                if (args != null && args.Length > 0)
                    message = String.Format(message, args);

                throw new InvalidOperationException(message + " (Reason: " + error.ToString() + ")");
            }
        }
    }

    public sealed class OpenALSoundController : ISoundEngine
    {

        class PoolSlot
        {
            public bool IsActive;
            public int FrameStarted;
            public Vector3 Pos;
            public bool IsRelative;
            public OpenAlSoundSource SoundSource;
            public OpenAlSound Sound;
        }

        private static OpenALSoundController _instance = null;
        private static EffectsExtension _efx = null;
        private IntPtr _device;
        private IntPtr _context;
        float volume = 1f;

        const int MaxInstancesPerFrame = 3;
        const int GroupDistance = 2730;
        const int GroupDistanceSqr = GroupDistance * GroupDistance;

        public float Volume
        {
            get { return volume; }
            set
            {
                AL.Listenerf(ALSourcef.Gain, volume = value);
            }
        }


        IntPtr NullContext = IntPtr.Zero;
        private int[] allSourcesArray;
#if DESKTOPGL || ANGLE

        // MacOS & Linux shares a limit of 256.
        internal const int MAX_NUMBER_OF_SOURCES = 256;

#elif IOS

        // Reference: http://stackoverflow.com/questions/3894044/maximum-number-of-openal-sound-buffers-on-iphone
        internal const int MAX_NUMBER_OF_SOURCES = 32;

#elif ANDROID

        // Set to the same as OpenAL on iOS
        internal const int MAX_NUMBER_OF_SOURCES = 32;

#endif
#if ANDROID
        private const int DEFAULT_FREQUENCY = 48000;
        private const int DEFAULT_UPDATE_SIZE = 512;
        private const int DEFAULT_UPDATE_BUFFER_COUNT = 2;
#elif DESKTOPGL
        private static OggStreamer _oggstreamer;
#endif
        private List<int> availableSourcesCollection;
        private List<int> inUseSourcesCollection;
        bool _isDisposed;
        public bool SupportsIma4 { get; private set; }
        public bool SupportsAdpcm { get; private set; }
        public bool SupportsEfx { get; private set; }
        public bool SupportsIeee { get; private set; }

        readonly Dictionary<int, PoolSlot> sourcePool = new Dictionary<int, PoolSlot>(MAX_NUMBER_OF_SOURCES);
        /// <summary>
        /// Sets up the hardware resources used by the controller.
        /// </summary>
		private OpenALSoundController()
        {
#if WINDOWS
            // On Windows, set the DLL search path for correct native binaries
            NativeHelper.InitDllDirectory();
#endif

            if (!OpenSoundController())
            {
                throw new NoAudioHardwareException("OpenAL device could not be initialized, see console output for details.");
            }

            if (Alc.IsExtensionPresent(_device, "ALC_EXT_CAPTURE"))
                Microphone.PopulateCaptureDevices();

            // We have hardware here and it is ready

			allSourcesArray = new int[MAX_NUMBER_OF_SOURCES];
			AL.GenSources(allSourcesArray);
            ALHelper.CheckError("Failed to generate sources.");
            Filter = 0;
            //if (Efx.IsInitialized)
            //{
            //    Filter = Efx.GenFilter();
            //}
            availableSourcesCollection = new List<int>(allSourcesArray);
			inUseSourcesCollection = new List<int>();
            for(var i = 0; i < availableSourcesCollection.Count; i++)
            {
                sourcePool.Add(availableSourcesCollection[i], new PoolSlot() { IsActive = false });
            }
		}

        ~OpenALSoundController()
        {
            Dispose(false);
        }

        /// <summary>
        /// Open the sound device, sets up an audio context, and makes the new context
        /// the current context. Note that this method will stop the playback of
        /// music that was running prior to the game start. If any error occurs, then
        /// the state of the controller is reset.
        /// </summary>
        /// <returns>True if the sound controller was setup, and false if not.</returns>
        private bool OpenSoundController()
        {
            try
            {
                _device = Alc.OpenDevice(string.Empty);
                EffectsExtension.device = _device;
            }
            catch (DllNotFoundException ex)
            {
                throw ex;
            }
            catch (Exception ex)
            {
                throw new NoAudioHardwareException("OpenAL device could not be initialized.", ex);
            }

            AlcHelper.CheckError("Could not open OpenAL device");

            if (_device != IntPtr.Zero)
            {
#if ANDROID
                // Attach activity event handlers so we can pause and resume all playing sounds
                AndroidGameView.OnPauseGameThread += Activity_Paused;
                AndroidGameView.OnResumeGameThread += Activity_Resumed;

                // Query the device for the ideal frequency and update buffer size so
                // we can get the low latency sound path.

                /*
                The recommended sequence is:

                Check for feature "android.hardware.audio.low_latency" using code such as this:
                import android.content.pm.PackageManager;
                ...
                PackageManager pm = getContext().getPackageManager();
                boolean claimsFeature = pm.hasSystemFeature(PackageManager.FEATURE_AUDIO_LOW_LATENCY);
                Check for API level 17 or higher, to confirm use of android.media.AudioManager.getProperty().
                Get the native or optimal output sample rate and buffer size for this device's primary output stream, using code such as this:
                import android.media.AudioManager;
                ...
                AudioManager am = (AudioManager) getSystemService(Context.AUDIO_SERVICE);
                String sampleRate = am.getProperty(AudioManager.PROPERTY_OUTPUT_SAMPLE_RATE));
                String framesPerBuffer = am.getProperty(AudioManager.PROPERTY_OUTPUT_FRAMES_PER_BUFFER));
                Note that sampleRate and framesPerBuffer are Strings. First check for null and then convert to int using Integer.parseInt().
                Now use OpenSL ES to create an AudioPlayer with PCM buffer queue data locator.

                See http://stackoverflow.com/questions/14842803/low-latency-audio-playback-on-android
                */

                int frequency = DEFAULT_FREQUENCY;
                int updateSize = DEFAULT_UPDATE_SIZE;
                int updateBuffers = DEFAULT_UPDATE_BUFFER_COUNT;
                if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.JellyBeanMr1)
                {
                    Android.Util.Log.Debug("OAL", Game.Activity.PackageManager.HasSystemFeature(PackageManager.FeatureAudioLowLatency) ? "Supports low latency audio playback." : "Does not support low latency audio playback.");

                    var audioManager = Game.Activity.GetSystemService(Context.AudioService) as AudioManager;
                    if (audioManager != null)
                    {
                        var result = audioManager.GetProperty(AudioManager.PropertyOutputSampleRate);
                        if (!string.IsNullOrEmpty(result))
                            frequency = int.Parse(result, CultureInfo.InvariantCulture);
                        result = audioManager.GetProperty(AudioManager.PropertyOutputFramesPerBuffer);
                        if (!string.IsNullOrEmpty(result))
                            updateSize = int.Parse(result, CultureInfo.InvariantCulture);
                    }

                    // If 4.4 or higher, then we don't need to double buffer on the application side.
                    // See http://stackoverflow.com/a/15006327
                    //if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.Kitkat)
                    //{
                    //    updateBuffers = 1;
                    //}
                }
                else
                {
                    Android.Util.Log.Debug("OAL", "Android 4.2 or higher required for low latency audio playback.");
                }
                Android.Util.Log.Debug("OAL", "Using sample rate " + frequency + "Hz and " + updateBuffers + " buffers of " + updateSize + " frames.");

                // These are missing and non-standard ALC constants
                const int AlcFrequency = 0x1007;
                const int AlcUpdateSize = 0x1014;
                const int AlcUpdateBuffers = 0x1015;

                int[] attribute = new[]
                {
                    AlcFrequency, frequency,
                    AlcUpdateSize, updateSize,
                    AlcUpdateBuffers, updateBuffers,
                    0
                };
#elif IOS
                EventHandler<AVAudioSessionInterruptionEventArgs> handler = delegate(object sender, AVAudioSessionInterruptionEventArgs e) {
                    switch (e.InterruptionType)
                    {
                        case AVAudioSessionInterruptionType.Began:
                            AVAudioSession.SharedInstance().SetActive(false);
                            Alc.MakeContextCurrent(IntPtr.Zero);
                            Alc.SuspendContext(_context);
                            break;
                        case AVAudioSessionInterruptionType.Ended:
                            AVAudioSession.SharedInstance().SetActive(true);
                            Alc.MakeContextCurrent(_context);
                            Alc.ProcessContext(_context);
                            break;
                    }
                };
                AVAudioSession.Notifications.ObserveInterruption(handler);

                int[] attribute = new int[0];
#else
                int[] attribute = new int[0];
#endif

                _context = Alc.CreateContext(_device, attribute);
#if DESKTOPGL
                _oggstreamer = new OggStreamer();
#endif

                AlcHelper.CheckError("Could not create OpenAL context");

                if (_context != NullContext)
                {
                    Alc.MakeContextCurrent(_context);
                    AlcHelper.CheckError("Could not make OpenAL context current");
                    SupportsIma4 = AL.IsExtensionPresent("AL_EXT_IMA4");
                    SupportsAdpcm = AL.IsExtensionPresent("AL_SOFT_MSADPCM");
                    SupportsEfx = AL.IsExtensionPresent("AL_EXT_EFX");
                    SupportsIeee = AL.IsExtensionPresent("AL_EXT_float32");
                    return true;
                }
            }
            return false;
        }

		public static OpenALSoundController GetInstance
        {
			get
            {
				if (_instance == null)
					_instance = new OpenALSoundController();
				return _instance;
			}
		}

        //public static EffectsExtension Efx
        //{
        //    get
        //    {
        //        if (_efx == null)
        //            _efx = new EffectsExtension();
        //        return _efx;
        //    }
        //}

        public int Filter
        {
            get; private set;
        }

        //public static void DestroyInstance()
        //{
        //    if (_instance != null)
        //    {
        //        _instance.Dispose();
        //        _instance = null;
        //    }
        //}

        /// <summary>
        /// Destroys the AL context and closes the device, when they exist.
        /// </summary>
        private void CleanUpOpenAL()
        {
            Alc.MakeContextCurrent(NullContext);

            if (_context != NullContext)
            {
                Alc.DestroyContext (_context);
                _context = NullContext;
            }
            if (_device != IntPtr.Zero)
            {
                Alc.CloseDevice (_device);
                _device = IntPtr.Zero;
            }
        }

        /// <summary>
        /// Dispose of the OpenALSoundCOntroller.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose of the OpenALSoundCOntroller.
        /// </summary>
        /// <param name="disposing">If true, the managed resources are to be disposed.</param>
		void Dispose(bool disposing)
		{
            if (!_isDisposed)
            {
                if (disposing)
                {
#if DESKTOPGL
                    if(_oggstreamer != null)
                        _oggstreamer.Dispose();
#endif
                    for (int i = 0; i < allSourcesArray.Length; i++)
                    {
                        AL.DeleteSource(allSourcesArray[i]);
                        ALHelper.CheckError("Failed to delete source.");
                    }

                    //if (Filter != 0 && Efx.IsInitialized)
                    //    Efx.DeleteFilter(Filter);

                    Microphone.StopMicrophones();
                    CleanUpOpenAL();                    
                }
                _isDisposed = true;
            }
		}

        /// <summary>
        /// Reserves a sound buffer and return its identifier. If there are no available sources
        /// or the controller was not able to setup the hardware then an
        /// <see cref="InstancePlayLimitException"/> is thrown.
        /// </summary>
        /// <returns>The source number of the reserved sound buffer.</returns>
		//public int ReserveSource()
		//{
  //          int sourceNumber;

  //          lock (availableSourcesCollection)
  //          {                
  //              if (availableSourcesCollection.Count == 0)
  //              {
  //                  throw new InstancePlayLimitException();
  //              }

  //              sourceNumber = availableSourcesCollection.Last();
  //              inUseSourcesCollection.Add(sourceNumber);
  //              availableSourcesCollection.Remove(sourceNumber);
  //          }

  //          return sourceNumber;
		//}

  //      public void RecycleSource(int sourceId)
		//{
  //          lock (availableSourcesCollection)
  //          {
  //              inUseSourcesCollection.Remove(sourceId);
  //              availableSourcesCollection.Add(sourceId);
  //          }
		//}

  //      public void FreeSource(SoundEffectInstance inst)
  //      {
  //          RecycleSource(inst.SourceId);
  //          inst.SourceId = 0;
  //          inst.HasSourceId = false;
  //          inst.SoundState = SoundState.Stopped;
		//}

  //      public double SourceCurrentPosition (int sourceId)
		//{
  //          int pos;
		//	AL.GetSource (sourceId, ALGetSourcei.SampleOffset, out pos);
  //          ALHelper.CheckError("Failed to set source offset.");
		//	return pos;
		//}


        public ISoundSource AddSoundSourceFromMemory(byte[] data,int channels,int sampleBits,int sampleRate)
        {
            return new OpenAlSoundSource(data, channels, sampleBits, sampleRate);
        }

        bool TryGetSourceFromPool(out int source)
        {
            foreach(var kv in sourcePool)
            {
                if (!kv.Value.IsActive)
                {
                    sourcePool[kv.Key].IsActive = true;
                    source = kv.Key;
                    return true;
                }
            }

            var freeSources = new List<int>();
            foreach(var kv in sourcePool)
            {
                var sound = kv.Value.Sound;
                if(sound != null && sound.Complete)
                {
                    var freeSource = kv.Key;
                    freeSources.Add(freeSource);
                    AL.SourceRewind((uint)freeSource);
                    AL.Source(freeSource, ALSourcei.Buffer, 0);
                }
            }

            if(freeSources.Count == 0)
            {
                source = 0;
                return false;
            }

            foreach(var freeSource in freeSources)
            {
                var slot = sourcePool[freeSource];
                slot.SoundSource = null;
                slot.Sound = null;
                slot.IsActive = false;
            }

            source = freeSources[0];
            sourcePool[source].IsActive = true;
            return true;
        }

        public ISound Play2D(ISoundSource soundSource,bool loop,bool relative,Vector3 pos,float volume,bool attenuateVolume)
        {
            if(soundSource == null)
            {
                Android.Util.Log.Debug("sound", "Attempt to Play2D a null 'ISoundSource'");
                return null;
            }

            var alSoundSource = (OpenAlSoundSource)soundSource;

            var currFrame = 0;
            var atten = 1f;

            if (attenuateVolume)
            {
                int instances = 0, activeCount = 0;
                foreach(var s in sourcePool.Values)
                {
                    if (!s.IsActive)
                        continue;
                    if (s.IsRelative != relative)
                        continue;

                    ++activeCount;
                    if (s.SoundSource != alSoundSource)
                        continue;

                    if (currFrame - s.FrameStarted >= 5)
                        continue;

                    //Too far away to count?
                    var lensqr = (s.Pos - pos).LengthSquared;
                    if (lensqr >= GroupDistanceSqr)
                        continue;

                    if (++instances == MaxInstancesPerFrame)
                        return null;

                }

                //Attenuate a little bit based on number of active sounds
                atten = 0.66f * ((MAX_NUMBER_OF_SOURCES - activeCount * 0.5f) / MAX_NUMBER_OF_SOURCES);
            }

            int source;
            if (!TryGetSourceFromPool(out source))
                return null;

            var slot = sourcePool[source];
            slot.Pos = pos;
            slot.FrameStarted = currFrame;
            slot.IsRelative = relative;
            slot.SoundSource = alSoundSource;
            slot.Sound = new OpenAlSound(source, loop, relative, pos, volume * atten, alSoundSource.SampleRate, (uint)alSoundSource.Buffer);

            return slot.Sound;
        }


        public ISound Play2DStream(System.IO.Stream stream,int channels,int sampleBits,int sampleRate,bool loop,bool relative,Vector3 pos,float volume)
        {
            var currFrame = 0;

            int source;

            if (!TryGetSourceFromPool(out source))
                return null;

            var slot = sourcePool[source];
            slot.Pos = pos;
            slot.FrameStarted = currFrame;
            slot.IsRelative = relative;
            slot.SoundSource = null;
            slot.Sound = new OpenAlAsyncLoadSound(source, loop, relative, pos, volume, channels, sampleBits, sampleRate, stream);
            return slot.Sound;
        }

        public void PauseSound(ISound sound,bool paused)
        {
            if (sound == null)
                return;

            var source = ((OpenAlSound)sound).Source;
            PauseSound(source, paused);
        }

        void PauseSound(int source,bool paused)
        {
            int state;
            AL.GetSource(source, ALGetSourcei.SourceState, out state);
            if (paused)
            {

            }
            else if (!paused && state != (int)ALSourceState.Playing)
                AL.SourcePlay(source);
        }


        public void StopSound(ISound sound)
        {
            if (sound == null)
                return;

            ((OpenAlSound)sound).Stop();
        }

        public void StopAllSounds()
        {
            foreach (var slot in sourcePool.Values)
                if (slot.Sound != null)
                    slot.Sound.Stop();
        }

        public void SetListenerPosition(Vector3 position)
        {
            AL.Listener3f(ALListener3f.Position, position.X, position.Y, position.Z);

            var orientation = new[] { 0f, 0f, 1f, 0f, -1f, 0f };
            AL.Listenerfv(4111, orientation);
            AL.Listenerf(ALSourcef.ReferenceDistance, .01f);
        }

        public void SetSoundVolume(float volume,ISound music,ISound video)
        {
            var sounds = sourcePool.Keys.Where(key =>
            {
                int state;
                AL.GetSource(key, ALGetSourcei.SourceState, out state);

                return (state == (int)ALSourceState.Playing || state == (int)ALSourceState.Paused) &&
                (music == null || key != ((OpenAlSound)music).Source) &&
                (video == null || key != ((OpenAlSound)video).Source);
            });

            foreach(var s in sounds)
                AL.Source(s, ALSourcef.Gain, volume);
        }

        public void SetAllSoundsPaused(bool paused)
        {
            foreach (var source in sourcePool.Keys)
                PauseSound(source, paused);
        }

#if ANDROID
        void Activity_Paused(object sender, EventArgs e)
        {
            // Pause all currently playing sounds by pausing the mixer
            Alc.DevicePause(_device);
        }

        void Activity_Resumed(object sender, EventArgs e)
        {
            // Resume all sounds that were playing when the activity was paused
            Alc.DeviceResume(_device);
        }
#endif

        internal static  ALFormat MakeALFormat(int channels,int bits)
        {
            if (channels == 1)
                return bits == 16 ? ALFormat.Mono16 : ALFormat.Mono8;
            else
                return bits == 16 ? ALFormat.Stereo16 : ALFormat.Stereo8;
        }


        class OpenAlSoundSource : ISoundSource
        {
            int buffer;
            bool disposed;

            public int Buffer { get { return buffer; } }

            public int SampleRate { get; private set; }

            public OpenAlSoundSource(byte[] data,int channels,int sampleBits,int sampleRate)
            {
                SampleRate = sampleRate;
                AL.GenBuffers(1, out buffer);
                AL.BufferData(buffer, OpenALSoundController.MakeALFormat(channels, sampleBits), data, data.Length, sampleRate);
            }

            ~OpenAlSoundSource()
            {
                Dispose(false);
            }


            protected virtual void Dispose(bool disposing)
            {
                if (!disposed)
                {
                    AL.DeleteBuffers(1, ref buffer);
                    disposed = true;
                }
            }


            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }
        }


        class OpenAlSound : ISound
        {
            public readonly int Source;
            protected readonly float SampleRate;
            private float _alVolume = 1f;
            public float Volume
            {
                get
                {
                    return _alVolume;
                }
                set
                {
                    _alVolume = value;
                    AL.Source(Source, ALSourcef.Gain, _alVolume);
                }
            }


            public OpenAlSound(int source,bool looping,bool relative,Vector3 pos,float volume,int sampleRate,uint buffer) : this(source, looping, relative, pos, volume, sampleRate)
            {
                AL.Source(source, ALSourcei.Buffer, (int)buffer);
                AL.SourcePlay(source);
            }

            protected OpenAlSound(int source,bool looping,bool relative,Vector3 pos,float volume,int sampleRate)
            {
                Source = source;
                SampleRate = sampleRate;
                Volume = volume;
                //Pitch
                AL.Source(source, ALSourcef.Pitch, 1f);
                ALHelper.CheckError("Failed to set source pitch.");
                //Pan
                AL.Source(source, ALSource3f.Position, pos.X, pos.Y, pos.Z);
                ALHelper.CheckError("Failed to set source pan.");
                //Velocity
                AL.Source(source, ALSource3f.Velocity, 0f, 0f, 0f);
                ALHelper.CheckError("Failed to set source velocity.");
                //Looping
                AL.Source(source, ALSourceb.Looping, looping);
                ALHelper.CheckError("Failed to set source loop state.");
                AL.Source(source, ALSourcei.SourceRelative, relative?1:0);
                ALHelper.CheckError("Failed set source relative.");
                //Distance Model
                //AL.DistanceModel(ALDistanceModel.InverseDistanceClamped);
                AL.Source(source, ALSourcef.ReferenceDistance, 6826);
                AL.Source(source, ALSourcef.MaxDistance, 136533);
                ALHelper.CheckError("Failed set source distance.");

            }


            public virtual float SeekPosition
            {
                get
                {
                    int sampleOffset;
                    AL.GetSource(Source, ALGetSourcei.SampleOffset, out sampleOffset);
                    return sampleOffset / SampleRate;
                }

            }

            public void SetPosition(Vector3 pos)
            {
                AL.Source(Source, ALSource3f.Position, pos.X, pos.Y, pos.Z);
            }
            
            public virtual bool Complete
            {
                get
                {
                    int state;
                    AL.GetSource(Source, ALGetSourcei.SourceState, out state);
                    return state == (int)ALSourceState.Stopped;
                }
            }


            protected void StopSource()
            {
                int state;
                AL.GetSource(Source, ALGetSourcei.SourceState, out state);
                if(state == (int)ALSourceState.Playing || state == (int)ALSourceState.Paused)
                {
                    AL.SourceStop(Source);
                }
            }

            public virtual void Stop()
            {
                StopSource();
                AL.Source(Source, ALSourcei.Buffer, 0);
            }

        
        }

        /// <summary>
        /// 
        /// </summary>
        class OpenAlAsyncLoadSound : OpenAlSound
        {
            static readonly byte[] SilentDta = new byte[2];

            readonly CancellationTokenSource cts = new CancellationTokenSource();
            readonly Task playTask;

            public OpenAlAsyncLoadSound(int source,bool looping,bool relative,Vector3 pos,
                float volume,int channels,int sampleBits,int sampleRate,System.IO.Stream stream) : base(source, looping, relative, pos, volume, sampleRate)
            {
                var silentSource = new OpenAlSoundSource(SilentDta, channels, sampleBits, sampleRate);

                AL.Source(source, ALSourcei.Buffer, (int)silentSource.Buffer);

                playTask = Task.Run(async () =>
                {
                    System.IO.MemoryStream memoryStream;
                    using (stream)
                    {
                        memoryStream = new MemoryStream();
                        try
                        {
                            await stream.CopyToAsync(memoryStream, 81920, cts.Token);
                        }
                        catch (TaskCanceledException)
                        {
                            AL.SourceStop(source);
                            AL.Source(source, ALSourcei.Buffer, 0);
                            silentSource.Dispose();
                            return;
                        }
                    }

                    var data = memoryStream.ToArray();
                    var bytesPerSample = sampleBits / 8f;
                    var lengthInSecs = data.Length / (channels * bytesPerSample * sampleRate);
                    using(var soundSource = new OpenAlSoundSource(data, channels, sampleBits, sampleRate))
                    {
                        AL.SourceStop(source);
                        AL.Source(source, ALSourcei.Buffer, (int)soundSource.Buffer);
                        silentSource.Dispose();

                        lock (cts)
                        {
                            if (!cts.IsCancellationRequested)
                            {
                                int state;
                                AL.GetSource(Source, ALGetSourcei.SourceState, out state);
                                if (state != (int)ALSourceState.Stopped)
                                    AL.SourcePlay(source);
                                else
                                {
                                    AL.SourceRewind((uint)source);
                                }
                            }
                        }

                        while (!cts.IsCancellationRequested)
                        {
                            var currentSeek = SeekPosition;

                            int state;
                            AL.GetSource(Source, ALGetSourcei.SourceState, out state);
                            if (state == (int)ALSourceState.Stopped)
                                break;

                            try
                            {
                                var delaySecs = Math.Max(lengthInSecs - currentSeek, 1 / 60f);
                                await Task.Delay(TimeSpan.FromSeconds(delaySecs), cts.Token);
                            }
                            catch (TaskCanceledException)
                            {

                            }
                        }

                        AL.Source(Source, ALSourcei.Buffer, 0);


                    }


                });
            }

            public override void Stop()
            {
                lock (cts)
                {
                    StopSource();
                    cts.Cancel();
                }

                try
                {
                    playTask.Wait();
                }
                catch (AggregateException) { }
            }

            public override bool Complete { get { return playTask.IsCompleted; } }
        }

    }
}
