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
        
        public void PlayFlowerBloom(Vector3? position = null)
        {
            if (position.HasValue)
            {
                controller.PlayOneShot(library.flowerBloom, position.Value);
            }
            else
            {
                controller.PlayOneShot(library.flowerBloom);
            }
        }

        public void PlayPickupForage(Vector3? position = null)
        {
            if (position.HasValue)
            {
                controller.PlayOneShot(library.pickupForage, position.Value);
            }
            else
            {
                controller.PlayOneShot(library.pickupForage);
            }
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