using FMODUnity;
using UnityEngine;

namespace SpringJam2026.Data
{
    [CreateAssetMenu(menuName = "Spring Jam 2026/Audio/Audio Library")]
    public class AudioLibrary : ScriptableObject
    {
        public EventReference pickupForage;
        public EventReference flowerBloom;

        public EventReference musicFieldTheme;
    }
}