using System.Collections.Generic;
using SpringJam2026.Audio;
using SpringJam2026.Utils;
using UnityEngine;

namespace SpringJam2026
{
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerFootstep : MonoBehaviour, IGameService
    {
        public enum SurfaceType
        {
            Grass,
            Dirt,
            Wood,
            Cobblestone
        }

        [Header("Step Settings")]
        [Tooltip("How often we play the footstep sound while moving")]
        [SerializeField] private float baseStepInterval = 0.4f;
        
        [Header("Detection Settings")]
        [SerializeField] private PlayerInputHandler input;
        [SerializeField] private SurfaceType defaultSurface = SurfaceType.Grass;
        
        private Rigidbody rb;
        private AudioService audioService;
        private float stepTimer;
        private SurfaceType currentSurface;
        private bool isAudioPaused = false;
        private readonly HashSet<Collider> activeSurfaceColliders = new();

        public int Priority => 60;

        public void Initialize()
        {
            rb = GetComponent<Rigidbody>();
            audioService = ServiceLocator.Get<AudioService>();
            currentSurface = defaultSurface;
        }

        public void Bind()
        {
            // Silence is golden
        }

        private void Update()
        {
            if (!IsMoving())
            {
                stepTimer = 0f;
                return;
            }
            
            stepTimer += Time.deltaTime;
            
            float speed = rb.linearVelocity.magnitude;
            float interval = baseStepInterval / Mathf.Max(speed, 1f);

            if (stepTimer >= interval)
            {
                TryPlayFootstep();
                stepTimer = 0f;
            }
        }

        private bool IsMoving()
        {
            if (input == null) return false;

            return input.MoveInput.sqrMagnitude > 0.01f;
        }

        private void TryPlayFootstep()
        {
            if (isAudioPaused)
                return;

            switch (currentSurface)
            {
                case SurfaceType.Grass:
                    audioService?.PlayPlayerFootstepGrass(transform.position);
                    return;
                case SurfaceType.Dirt:
                    audioService?.PlayPlayerFootstepDirt(transform.position);
                    return;
                case SurfaceType.Wood:
                    audioService?.PlayPlayerFootstepWood(transform.position);
                    return;
                case SurfaceType.Cobblestone:
                    audioService?.PlayPlayerFootstepCobblestone(transform.position);
                    return;
            }
        }
        
        #region Surface Detection (Using Triggers)
        
        private void OnTriggerEnter(Collider other)
        {
            if (IsSurface(other))
            {
                activeSurfaceColliders.Add(other);
                ResolveSurface();
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (IsSurface(other))
            {
                activeSurfaceColliders.Remove(other);
                ResolveSurface();
            }
        }
        
        private bool IsSurface(Collider other)
        {
            return other.CompareTag("Dirt") ||
                   other.CompareTag("Wood") ||
                   other.CompareTag("Cobblestone");
        }
        
        private void ResolveSurface()
        {
            bool hasCobble = false;
            bool hasWood = false;
            bool hasDirt = false;

            foreach (var col in activeSurfaceColliders)
            {
                if (col == null) continue;

                if (col.CompareTag("Cobblestone"))
                    hasCobble = true;
                else if (col.CompareTag("Wood"))
                    hasWood = true;
                else if (col.CompareTag("Dirt"))
                    hasDirt = true;
            }

            if (hasCobble)
            {
                currentSurface = SurfaceType.Cobblestone;
                return;
            }

            if (hasWood)
            {
                currentSurface = SurfaceType.Wood;
                return;
            }

            if (hasDirt)
            {
                currentSurface = SurfaceType.Dirt;
                return;
            }

            currentSurface = defaultSurface;
        }
        
        #endregion
        
        #region External Control

        /// <summary>
        /// Stops footstep sounds from playing but keeps timing logic running.
        /// I added this in case we have a scenario where we don't want the sounds to play, instead of gating when/if
        /// We can just "pause/mute" the sound output
        /// </summary>
        public void PauseFootsteps()
        {
            isAudioPaused = true;
        }

        /// <summary>
        /// Resumes footstep sound playback.
        /// </summary>
        public void ResumeFootsteps()
        {
            isAudioPaused = false;
        }

        #endregion
    }   
}
