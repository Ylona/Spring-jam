using FMODUnity;
using UnityEngine;

namespace SpringJam2026.Data
{
    [CreateAssetMenu(menuName = "Spring Jam 2026/Audio/Audio Library")]
    public class AudioLibrary : ScriptableObject
    {
        [Header("Bee Task")]
        public EventReference beeMoving;
        public EventReference beeSting;
        public EventReference beeSwarmLoop;
        public EventReference lurePotPlace;
        public EventReference mintBloom;

        [Header("Cooking Task")]
        public EventReference cookingSequence;
        public EventReference prepTablePlace;

        [Header("Flower Task")]
        public EventReference flowerInteract;
        public EventReference flowerRight;
        public EventReference flowerWrong;

        [Header("Music")]
        public EventReference fieldTheme;

        [Header("Player")]
        public EventReference playerFootstepGrass;
        public EventReference playerPickupForage;
    }
}