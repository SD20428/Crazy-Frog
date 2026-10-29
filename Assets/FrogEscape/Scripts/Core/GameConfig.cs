using System;
using UnityEngine;
using UnityEngine.Timeline;

namespace FrogEscape
{
    public enum GameMode { Menu, Playing, Cinematic, Paused, Failed, Cleared, Loading }
    [Serializable]
    public class DifficultySettings
    {
        public string label = "EASY";
        public int health = 4;
        public float speed = 24, lateralSpeed = 8, warningTime = 2.5f;
        public float invulnerability = 2, dodgeDuration = .85f, dodgeCooldown = 2.2f;
        public float missileDodgeWindow = 1.05f, attackInterval = 6, prediction = .25f;
        public float policeGap = 18, policeSpeed = 34, missileTurnRate = 95, missileMaxSpeed = 65;
        public int obstacleStride = 2;
    }
    [CreateAssetMenu(menuName = "Frog Escape/Game Config")]
    public class GameConfig : ScriptableObject
    {
        public DifficultySettings easy = new DifficultySettings();
        public DifficultySettings normal = new DifficultySettings { label="NORMAL", health=3, speed=28, warningTime=1.8f, invulnerability=1.5f, missileDodgeWindow=.65f, attackInterval=4.8f, prediction=.45f, obstacleStride=1, policeGap=14, policeSpeed=39, missileTurnRate=125, missileMaxSpeed=75 };
        public string menuScene, endingScene;
        public string[] stageScenes;
        public float[] stageDurations = {60, 65, 67};
        public TimelineAsset[] transitions;
        public TimelineAsset ending;
        public AudioClip music, engine, siren, warning, dodge, impact, landing, victory;
        [Min(1)] public float musicBpm = 120;
        [Min(0)] public float musicBeatOffset;
        public Material warningMaterial, successMaterial;
        public DifficultySettings Get(bool normalMode) => normalMode ? normal : easy;
    }
}
