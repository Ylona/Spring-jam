using FMODUnity;
using UnityEngine;

namespace SpringJam2026.Data
{
    [CreateAssetMenu(menuName = "Spring Jam 2026/Audio/Audio Library")]
    public class AudioLibrary : ScriptableObject
    {
        [Header("Ambience")]
        public EventReference forestAmbience;
        
        [Header("Bee Task")]
        public EventReference beeMoving;
        public EventReference beeSting;
        public EventReference beeSuccessJingle;
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
        public EventReference cookingSuccess;
        public EventReference morningIntro;
        public EventReference nightOutro;
        public EventReference springVictory;
        public EventReference titleThemeLoop;
        public EventReference zoneMusicSwitch;

        [Header("Player")]
        public EventReference playerFootstepDirt;
        public EventReference playerFootstepGrass;
        public EventReference playerFootstepWood;
        public EventReference playerFootstepCobblestone;
        public EventReference playerPickupForage;
        
        [Header("UI")]
        public EventReference uiHover;
        public EventReference uiReturn;
        public EventReference uiSelect;
        public EventReference uiStartGame;
        
        [Header("Uncategorized")]
        public EventReference bunnyHops;
    }
}