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
        
        public void PlayBeeMovement(Vector3? position = null)
        {
            PlayOneShot(library.beeMoving, position);
        }
        
        public void PlayBeeSting(Vector3? position = null)
        {
            PlayOneShot(library.beeSting, position);
        }
        
        public void StartBeeSwarmLoop()
        {
            controller.PlayLoop("beeSwarm", library.beeSwarmLoop);
        }

        public void StopBeeSwarmLoop()
        {
            controller.StopLoop("beeSwarm");
        }
        
        public void PlayLurePotPlace(Vector3? position = null)
        {
            PlayOneShot(library.lurePotPlace, position);
        }
        
        public void PlayMintBloom(Vector3? position = null)
        {
            PlayOneShot(library.mintBloom, position);
        }

        public void PlayCookingSequence(Vector3? position = null)
        {
            PlayOneShot(library.cookingSequence, position);
        }
        
        public void PlayPrepTable(Vector3? position = null)
        {
            PlayOneShot(library.prepTablePlace, position);
        }
        
        public void PlayerFlowerInteract(Vector3? position = null)
        {
            PlayOneShot(library.flowerInteract, position);
        }
        
        public void PlayFlowerRight(Vector3? position = null)
        {
            PlayOneShot(library.flowerRight, position);
        }
        
        public void PlayFlowerWrong(Vector3? position = null)
        {
            PlayOneShot(library.flowerWrong, position);
        }

        public void StartMusic()
        {
            controller.PlayLoop("music", library.fieldTheme);
        }

        public void StopMusic()
        {
            controller.StopLoop("music");
        }
        
        public void PlayPlayerFootstepGrass(Vector3? position = null)
        {
            PlayOneShot(library.playerFootstepGrass, position);
        }
        
        public void PlayPlayerPickupForage(Vector3? position = null)
        {
            PlayOneShot(library.playerPickupForage, position);
        }
        
        public void StopAllLoops()
        {
            controller.StopAll();
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
        
        #region Helper
        
        private void PlayOneShot(EventReference clip, Vector3? position = null)
        {
            if (position.HasValue)
            {
                controller.PlayOneShot(clip, position.Value);
            }
            else
            {
                controller.PlayOneShot(clip);
            }
        }
        
        #endregion
    }
}