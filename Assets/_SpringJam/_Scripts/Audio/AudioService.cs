using System.Collections;
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
        
        private int forestZoneCount = 0;
        private const string FOREST_ID = "forest-ambience";
        private bool isMusicPlaying;

        public void Initialize()
        {
            controller = ServiceLocator.Get<AudioController>();
            library = controller.library;
        }

        public void Bind() { }

        #region Gameplay Audio
        
        public void EnterForest()
        {
            forestZoneCount++;

            if (forestZoneCount > 1)
                return;

            var instance = controller.PlayLoop(FOREST_ID, library.forestAmbience);

            controller.SetLoopVolume(FOREST_ID, 0f);
            controller.FadeLoop(FOREST_ID, 1f, 2f);
        }
        
        public void ExitForest()
        {
            forestZoneCount--;

            if (forestZoneCount > 0)
                return;

            controller.FadeLoop(FOREST_ID, 0f, 2f);
            controller.StartCoroutine(StopForestAfterFade());
        }

        private IEnumerator StopForestAfterFade()
        {
            yield return new WaitForSeconds(2f);

            controller.StopLoop(FOREST_ID);
        }

        public void PlayMorningIntro(Vector3? position = null)
        {
            PlayOneShot(library.morningIntro, position);
        }

        public void PlayNightOutro(Vector3? position = null)
        {
            PlayOneShot(library.nightOutro, position);
        }
        
        public void PlayBeeMovement(Vector3? position = null)
        {
            PlayOneShot(library.beeMoving, position);
        }
        
        public void PlayBeeSting(Vector3? position = null)
        {
            PlayOneShot(library.beeSting, position);
        }
        
        public void PlayBeeSuccess(Vector3? position = null)
        {
            PlayOneShot(library.beeSuccessJingle, position);
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
        
        public void PlayFlowerInteract(Vector3? position = null)
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
        
        public void PlayBunnyHop(Vector3? position = null)
        {
            PlayOneShot(library.bunnyHops, position);
        }

        public void StartMusic()
        {
            if (isMusicPlaying) return;

            controller.PlayLoop("music", library.zoneMusicSwitch);
            isMusicPlaying = true;
        }

        public void StopMusic()
        {
            if (!isMusicPlaying) return;

            controller.StopLoop("music");
            isMusicPlaying = false;
        }
        
        public void PlayPlayerFootstepGrass(Vector3? position = null)
        {
            PlayOneShot(library.playerFootstepGrass, position);
        }
        
        public void PlayPlayerFootstepDirt(Vector3? position = null)
        {
            PlayOneShot(library.playerFootstepDirt, position);
        }
        
        public void PlayPlayerFootstepWood(Vector3? position = null)
        {
            PlayOneShot(library.playerFootstepWood, position);
        }
        
        public void PlayPlayerFootstepCobblestone(Vector3? position = null)
        {
            PlayOneShot(library.playerFootstepCobblestone, position);
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
        
        #region UI Sounds
        
        public void PlayUIHover()
        {
            PlayOneShot(library.uiHover);
        }
        
        public void PlayUIReturn()
        {
            PlayOneShot(library.uiReturn);
        }
        
        public void PlayUISelect()
        {
            PlayOneShot(library.uiSelect);
        }
        
        public void PlayUIStartGame()
        {
            PlayOneShot(library.uiStartGame);
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
        
        public void SetMusicZone(int value)
        {
            controller.SetParameter("music", "Zone Switch", value);
        }
        
        #endregion
    }
}