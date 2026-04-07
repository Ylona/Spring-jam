using System;
using System.Collections.Generic;
using FMOD.Studio;
using FMODUnity;
using SpringJam2026.Data;
using SpringJam2026.Utils;
using UnityEngine;
using STOP_MODE = FMOD.Studio.STOP_MODE;

namespace SpringJam2026.Audio
{
    /**
     * The controller is the one that talks directly to FMod, do not call these functions directly except from AudioService
     */
    public sealed class AudioController : MonoBehaviour, IGameService
    {
        [SerializeField] public AudioLibrary library;

        public int Priority => 40;

        private Dictionary<string, EventInstance> activeLoops = new();
        private Bus masterBus;
        private bool masterBusInitialized;
        private float lastVolumeBeforeMute = 1f;
        
        #region IGameService

        public void Initialize()
        {
            if (masterBusInitialized)
                return;

            masterBus = RuntimeManager.GetBus("bus:/");
            masterBusInitialized = true;
        }

        public void Bind() { }

        #endregion

        #region OneShot

        public void PlayOneShot(EventReference sound)
        {
            RuntimeManager.PlayOneShot(sound);
        }

        public void PlayOneShot(EventReference sound, Vector3 position)
        {
            RuntimeManager.PlayOneShot(sound, position);
        }
        
        #endregion

        #region Looping

        public EventInstance PlayLoop(string id, EventReference sound)
        {
            if (activeLoops.ContainsKey(id))
                return activeLoops[id];

            var instance = RuntimeManager.CreateInstance(sound);
            instance.start();

            activeLoops[id] = instance;
            return instance;
        }

        public void StopLoop(string id, STOP_MODE stopMode = STOP_MODE.ALLOWFADEOUT)
        {
            if (!activeLoops.TryGetValue(id, out var instance))
                return;

            instance.stop(stopMode);
            instance.release();
            activeLoops.Remove(id);
        }
        
        #endregion
        
        #region FMod Parameters

        public void SetParameter(string id, string param, float value)
        {
            if (activeLoops.TryGetValue(id, out var instance))
            {
                instance.setParameterByName(param, value);
            }
        }

        public void SetParameterWithLabel(string id, string param, string label)
        {
            if (activeLoops.TryGetValue(id, out var instance))
            {
                instance.setParameterByNameWithLabel(param, label);
            }
        }
        
        #endregion
        
        #region Helpers

        public void StopAll(STOP_MODE stopMode = STOP_MODE.IMMEDIATE)
        {
            foreach (var instance in activeLoops.Values)
            {
                instance.stop(stopMode);
                instance.release();
            }

            activeLoops.Clear();
        }
        
        public void SetMasterVolume(float volume)
        {
            masterBus.setVolume(Mathf.Clamp01(volume));
        }

        public float GetMasterVolume()
        {
            masterBus.getVolume(out float volume);
            return volume;
        }

        public void MuteMaster(bool mute)
        {
            if (mute)
            {
                masterBus.getVolume(out lastVolumeBeforeMute);
                masterBus.setVolume(0f);
            }
            else
            {
                masterBus.setVolume(lastVolumeBeforeMute);
            }
        }
        
        #endregion
    }   
}
