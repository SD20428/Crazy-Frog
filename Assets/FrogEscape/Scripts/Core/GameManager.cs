using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FrogEscape
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        public GameConfig config;
        public bool normalMode;
        public int health, dodges, hits;
        public float runTime;
        public GameMode mode = GameMode.Menu;
        public GameMode modeBeforePause;
        public DifficultySettings Settings => config.Get(normalMode);
        public bool IsLoading => mode == GameMode.Loading;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; }
        public static GameManager Ensure(GameConfig config)
        {
            if (Instance == null) Instance = new GameObject("FrogEscape Session").AddComponent<GameManager>();
            Instance.config = config;
            return Instance;
        }
        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Application.targetFrameRate = 60;
            Application.runInBackground = true;
        }
        public void StartGame()
        {
            if (IsLoading) return;
            health = Settings.health; dodges = 0; hits = 0; runTime = 0;
            Load(config.stageScenes[0]);
        }
        public void Retry()
        {
            if (IsLoading) return;
            health = Settings.health;
            Load(SceneManager.GetActiveScene().path);
        }
        public void ReturnToMenu() { if (!IsLoading) Load(config.menuScene); }
        public void TogglePause()
        {
            if (mode == GameMode.Paused) { mode = modeBeforePause; Time.timeScale = 1; AudioListener.pause = false; }
            else if (mode == GameMode.Playing || mode == GameMode.Cinematic)
            { modeBeforePause = mode; mode = GameMode.Paused; Time.timeScale = 0; AudioListener.pause = true; }
        }
        public void Load(string path)
        {
            if (IsLoading) return;
            Time.timeScale = 1; AudioListener.pause = false;
            mode = GameMode.Loading;
            StartCoroutine(LoadRoutine(path));
        }
        IEnumerator LoadRoutine(string path)
        {
            yield return null;
            var operation = SceneManager.LoadSceneAsync(path);
            if (operation == null) { Debug.LogError("Cannot load Frog Escape scene: " + path); mode = GameMode.Failed; yield break; }
            while (!operation.isDone) yield return null;
        }
        void OnDestroy() { if (Instance == this) { Time.timeScale=1; AudioListener.pause=false; Instance=null; } }
    }
}
