using UnityEngine;
using FMODUnity;
using SpringJam2026.Data;
using SpringJam2026.Utils;

namespace SpringJam2026.Audio
{
    /*
     * The public facing API of dealing with audio such as playing one shot, music and setting/getting volume
     * ex. ServiceLocator<AudioService>.PlayMusic();
     */
    public sealed class AudioService : IGameService
    {
        public int Priority => 50;

        private AudioController controller;
        private AudioLibrary library;

        public void Initialize()
        {
            controller = ServiceLocator.Get<AudioController>();
            library = controller.library;
        }

        public void Bind() { }

        #region Gameplay Audio
        
        /**
         * This is in case we want to play something directly, im still torn between calling "PlayPickup" sound vs.
         * "Play(pickupSound)" since both cases require us to either access library in the script or a new func
         * is needed here
         */
        public void Play(EventReference sound, Vector3? pos = null)
        {
            if (pos.HasValue)
                controller.PlayOneShot(sound, pos.Value);
            else
                controller.PlayOneShot(sound);
        }

        public void PlayPickup(Vector3 position)
        {
            controller.PlayOneShot(library.pickupForage, position);
        }

        public void PlayMusic()
        {
            controller.PlayLoop("music", library.musicFieldTheme);
        }

        public void StopMusic()
        {
            controller.StopLoop("music");
        }

        #endregion

        #region Volume

        public void SetMasterVolume(float volume)
        {
            controller.SetMasterVolume(volume);
        }

        public float GetMasterVolume()
        {
            return controller.GetMasterVolume();
        }

        public void MuteMasterVolume(bool mute)
        {
            controller.MuteMaster(mute);
        }
        
        #endregion
    }
}