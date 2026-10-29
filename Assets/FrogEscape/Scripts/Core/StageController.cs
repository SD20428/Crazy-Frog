using UnityEngine;
using UnityEngine.InputSystem;

namespace FrogEscape
{
    public class StageController : MonoBehaviour
    {
        public GameConfig config;
        [Tooltip("-1: title, 0..2: stages, 3: ending")] public int stageIndex;
        public RoadPath road;
        public BikeController player;
        public PlayerHealth playerHealth;
        public PoliceAI police;
        public PoliceAttackController attack;
        public MissileController missile;
        public CameraController chaseCamera;
        public CutsceneController cinematic;
        public GameHUD hud;
        public GameAudio audioSystem;
        public ObstacleSpawner obstacles;
        public BeatRushDirector beatRush { get; private set; }
        public float elapsed;
        public string feedback;
        public float feedbackUntil;
        public GameManager Session => GameManager.Instance;
        public DifficultySettings Settings => Session.Settings;
        public bool Playing => Session != null && Session.mode == GameMode.Playing;
        public float Progress => stageIndex >= 0 && stageIndex < 3 ? Mathf.Clamp01(elapsed/config.stageDurations[stageIndex]) : 0;
        void Awake() { GameManager.Ensure(config); }
        void Start()
        {
            elapsed = 0;
            var art = gameObject.AddComponent<NeonArtDirection>();
            art.stage = this;
            var detailArt = gameObject.AddComponent<DetailArtPass>();
            detailArt.stage = this;
            if (stageIndex == -1) { Session.mode=GameMode.Menu; player.distance=80; }
            else if (stageIndex == 3) { Session.mode=GameMode.Cinematic; player.distance=50; cinematic.Begin(2); }
            else
            {
                if (Session.health <= 0) Session.health=Settings.health;
                Session.mode=GameMode.Playing;
                player.distance=45;
                obstacles.Configure(Settings);
                if (stageIndex >= 0 && stageIndex <= 2)
                {
                    // Replace traffic rows with authored rhythm attacks, without saving scene changes.
                    foreach (var obstacle in obstacles.obstacles)
                        if (obstacle.visual) obstacle.visual.gameObject.SetActive(false);
                    beatRush = gameObject.AddComponent<BeatRushDirector>();
                    beatRush.stage = this;
                    if (stageIndex == 1)
                    {
                        // The beat director owns sewer attacks; do not stack the old gun scheduler.
                        attack.enabled = false;
                        attack.warningZone.gameObject.SetActive(false);
                    }
                }
            }
            player.Place();
            chaseCamera.Snap();
        }
        void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) Session.TogglePause();
            if (!Playing) return;
            elapsed += Time.deltaTime; Session.runTime += Time.deltaTime;
            if (stageIndex < 2 && Progress >= 1) cinematic.Begin(stageIndex);
        }
        public void ShowFeedback(string value, float duration=1.6f)
        { feedback=value; feedbackUntil=Time.time+duration; }
        public void FinishMissile(bool success)
        {
            if (!Playing) return;
            if (success) { Session.dodges++; audioSystem.Play(config.dodge); ShowFeedback("완벽한 회피!",2); }
            if (missile.wave >= 3)
            {
                if (success) Session.Load(config.endingScene);
                else if (Session.mode != GameMode.Failed) ShowFeedback("한 번 더! 초록 신호에 SPACE",3);
            }
        }
    }
}
