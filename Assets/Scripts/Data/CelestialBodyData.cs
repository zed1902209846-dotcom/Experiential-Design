using UnityEngine;

namespace SolarSystemAR
{
    [CreateAssetMenu(menuName = "Solar System/Celestial Body Data", fileName = "NewCelestialBody")]
    public class CelestialBodyData : ScriptableObject
    {
        public string id;
        public string displayName;
        public string englishName;

        [TextArea(2, 4)] public string shortDescription;
        [TextArea(2, 4)] public string shortDescriptionEn;
        [TextArea(4, 10)] public string fullDescription;
        [TextArea(4, 10)] public string fullDescriptionEn;

        public string diameter;
        public string rotationPeriod;
        public string orbitalPeriod;
        public string age;

        [TextArea(2, 6)] public string characteristics;

        public Sprite icon;
        public AudioClip narration;
    }
}
