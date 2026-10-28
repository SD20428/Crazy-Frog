#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

namespace FrogEscape.Tests
{
    // Editor-only acceptance driver: feeds a virtual keyboard through the actual Input System.
    // No autoplay code is included in the built player.
    public class PlaytestDriver : MonoBehaviour
    {
        [Serializable] public class Report
        {
            public string difficulty,status="running";
            public List<string> checks=new List<string>(),failures=new List<string>(),scenes=new List<string>(),observations=new List<string>();
            public float gameSeconds,wallSeconds,averageEditorFps;
            public int frames,missileMisses;
        }
        public Report report=new Report();
        public bool finished,autopilot;
        public string phase;
        public StageController stage;
        Keyboard keyboard;
        InputSettings.EditorInputBehaviorInPlayMode previousEditorInput;
        Key forced=Key.None;
        float startedAt,fpsTotal;int fpsSamples;
        bool earlyPressed,hitRecorded;
        int lastHealth=-1;
        readonly HashSet<int> visits=new HashSet<int>(),cinematics=new HashSet<int>();
        public static void Run(bool normal)
        {
            var go=new GameObject("Editor Acceptance Driver");DontDestroyOnLoad(go);
            var driver=go.AddComponent<PlaytestDriver>();driver.report.difficulty=normal?"Normal":"Easy";GameManager.Instance.normalMode=normal;driver.StartCoroutine(driver.Exercise());
        }
        void Awake() { previousEditorInput=InputSystem.settings.editorInputBehaviorInPlayMode;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;keyboard=InputSystem.AddDevice<Keyboard>("FrogEscapeTestKeyboard");keyboard.MakeCurrent();startedAt=Time.realtimeSinceStartup;Application.logMessageReceived+=OnLog; }
        void OnLog(string message,string stack,LogType type)
        { if(type==LogType.Error || type==LogType.Exception || type==LogType.Assert) report.failures.Add("Console: "+message); }
        void OnDestroy()
        { Application.logMessageReceived-=OnLog;if(keyboard!=null && keyboard.added)InputSystem.RemoveDevice(keyboard);InputSystem.settings.editorInputBehaviorInPlayMode=previousEditorInput;Time.timeScale=1; }
        void Check(bool condition,string name)
        { if(condition)report.checks.Add(name);else report.failures.Add(name); }
        IEnumerator Press(Key key,float duration=.12f)
        { forced=key;yield return new WaitForSecondsRealtime(duration);forced=Key.None;yield return new WaitForSecondsRealtime(.12f); }
        IEnumerator Await(Func<bool> condition,float timeout=15)
        { float until=Time.realtimeSinceStartup+timeout;while(!condition() && Time.realtimeSinceStartup<until)yield return null;Check(condition(),"Wait: "+phase); }
        IEnumerator Exercise()
        {
            phase="title keyboard start";yield return new WaitForSecondsRealtime(.5f);yield return Press(Key.Enter);
            yield return Await(()=>stage!=null && stage.stageIndex==0 && stage.Playing);
            if(stage==null || !stage.Playing) { Finish();yield break; }
            int frame=Time.frameCount;float distance=stage.player.distance;
            yield return new WaitForSecondsRealtime(.25f);Check(Time.frameCount>frame+2,"Player loop advances");Check(stage.player.distance>distance+2,"Automatic forward motion");
            phase="keyboard and road bounds";
            yield return Press(Key.A,.6f);Check(stage.player.lateral<-2,"A moves left");
            yield return Press(Key.RightArrow,1.6f);Check(stage.player.lateral>4,"Right arrow moves right");Check(stage.player.lateral<=stage.road.halfWidth-.699f,"Road boundary clamps");
            yield return Press(Key.LeftArrow,.4f);Check(stage.player.lateral<3,"Left arrow moves left");
            yield return Press(Key.D,.3f);Check(stage.player.lateral>2,"D moves right");
            phase="pause";yield return Press(Key.Escape);Check(GameManager.Instance.mode==GameMode.Paused,"ESC pauses");
            distance=stage.player.distance;yield return new WaitForSecondsRealtime(.3f);Check(Mathf.Abs(stage.player.distance-distance)<.01f,"Paused movement stops");
            yield return Press(Key.Escape);Check(stage.Playing,"ESC resumes");
            phase="dodge cooldown";yield return Press(Key.Space,.06f);float ready=stage.player.dodge.readyAt;
            Check(stage.player.dodge.Active,"SPACE starts dodge");yield return Press(Key.Space,.06f);Check(Mathf.Abs(ready-stage.player.dodge.readyAt)<.01f,"Repeated SPACE respects cooldown");
            phase="damage invulnerability";yield return new WaitForSecondsRealtime(1);
            int hp=GameManager.Instance.health;stage.playerHealth.TryDamage("Test collision",false);stage.playerHealth.TryDamage("Same-frame collision",false);
            Check(GameManager.Instance.health==hp-1,"Simultaneous damage counted once");
            stage.playerHealth.invincibleUntil=0;GameManager.Instance.health=1;stage.playerHealth.TryDamage("Test failure",false);Check(GameManager.Instance.mode==GameMode.Failed,"Zero health opens failure screen");
            phase="checkpoint retry";yield return Press(Key.Enter);yield return Await(()=>stage && stage.stageIndex==0 && stage.Playing && stage.elapsed<3);
            Check(GameManager.Instance.health==GameManager.Instance.Settings.health,"Retry restores health");Check(stage.elapsed<3,"Retry resets stage progress");
            autopilot=true;Time.timeScale=3;
            phase="stage 1 through wallride";yield return Await(()=>stage && stage.stageIndex==1 && stage.Playing,45);
            if(stage==null || stage.stageIndex!=1) { Finish();yield break; }
            Check(cinematics.Contains(0),"Wallride Timeline ran");
            phase="pooled projectile collision";autopilot=false;forced=Key.None;yield return new WaitForSecondsRealtime(.25f);
            hp=GameManager.Instance.health;var shot=stage.attack.pool[0];shot.Launch(stage.player.distance-5,stage.player.lateral,stage.player.Speed+48);
            yield return new WaitForSecondsRealtime(.3f);Check(GameManager.Instance.health==hp-1,"Projectile crossing applies damage");
            shot.gameObject.SetActive(false);shot.Launch(stage.player.distance-5,stage.player.lateral,stage.player.Speed+48);Check(!shot.resolved && shot.life==0,"Pool reuse resets state");
            yield return new WaitForSecondsRealtime(.2f);Check(GameManager.Instance.health==hp-1,"Projectile respects hit invulnerability");
            GameManager.Instance.health=GameManager.Instance.Settings.health;autopilot=true;
            phase="stage 2 through exit";yield return Await(()=>stage && stage.stageIndex==2 && stage.Playing,45);
            Check(cinematics.Contains(1),"Sewer exit Timeline ran");
            phase="missile timing and ending";yield return Await(()=>GameManager.Instance.mode==GameMode.Cleared,60);
            Check(earlyPressed,"Early dodge tested during LockOn");Check(report.missileMisses>0,"Early dodge does not avoid first missile");
            Check(visits.Contains(0)&&visits.Contains(1)&&visits.Contains(2)&&visits.Contains(3),"All independent gameplay and ending scenes visited");
            Check(cinematics.Contains(2),"Ending Timeline ran");Check(GameManager.Instance.mode==GameMode.Cleared,"Final successful dodge reaches CLEAR");
            report.gameSeconds=GameManager.Instance.runTime;Check(report.gameSeconds>175 && report.gameSeconds<260,"Gameplay duration within designed range");
            Time.timeScale=1;autopilot=false;yield return CaptureAfterFrame(20);yield return new WaitForSecondsRealtime(.3f);phase="title return";GameManager.Instance.ReturnToMenu();yield return Await(()=>stage && stage.stageIndex==-1 && GameManager.Instance.mode==GameMode.Menu);
            Check(GameManager.Instance.mode==GameMode.Menu,"Return to title works");Finish();
        }
        void Update()
        {
            if(finished)return;
            if(stage==null || stage.gameObject.scene!=SceneManager.GetActiveScene())stage=FindFirstObjectByType<StageController>();
            if(stage!=null)
            {
                if(visits.Add(stage.stageIndex)) {report.scenes.Add(SceneManager.GetActiveScene().name);StartCoroutine(CaptureAfterFrame(stage.stageIndex));}
                if(GameManager.Instance.health!=lastHealth) { lastHealth=GameManager.Instance.health;report.observations.Add($"stage={stage.stageIndex} elapsed={stage.elapsed:F1} health={lastHealth} feedback={stage.feedback}"); }
                if(autopilot && (stage.Playing || GameManager.Instance.mode==GameMode.Cinematic))Time.timeScale=3;
                if(GameManager.Instance.mode==GameMode.Cinematic && cinematics.Add(stage.cinematic.sequence))StartCoroutine(CaptureAfterFrame(10+stage.cinematic.sequence));
                if(stage.Playing && Time.timeScale<1.1f && Time.unscaledDeltaTime>.0001f){fpsTotal+=Time.unscaledDeltaTime;fpsSamples++;}
            }
            Key key=forced;
            if(autopilot && stage && stage.Playing)
            {
                float target=0;
                if(stage.stageIndex==0)
                {
                    float nearest=float.MaxValue;
                    foreach(var item in stage.obstacles.obstacles)
                        if(item.visual.gameObject.activeSelf && item.distance>stage.player.distance-3 && item.distance<nearest)nearest=item.distance;
                    if(nearest-stage.player.distance<95)
                    {
                        float best=float.MaxValue;
                        for(int lane=-1;lane<=1;lane++)
                        {
                            float x=lane*3.45f;bool safe=true;
                            foreach(var item in stage.obstacles.obstacles)if(item.visual.gameObject.activeSelf && Mathf.Abs(item.distance-nearest)<5 && Mathf.Abs(item.lateral-x)<2)safe=false;
                            if(safe && Mathf.Abs(x-stage.player.lateral)<best){best=Mathf.Abs(x-stage.player.lateral);target=x;}
                        }
                    }
                }
                else if(stage.stageIndex==1 && stage.attack.Busy)target=stage.attack.targetLane>=0?-3.6f:3.6f;
                float delta=target-stage.player.lateral;
                key=delta>.45f?Key.D:delta<-.45f?Key.A:Key.None;
                if(stage.stageIndex==2)
                {
                    var missile=stage.missile;
                    if(missile.wave==1 && missile.state==MissileState.LockOn && !earlyPressed) {key=Key.Space;earlyPressed=true;}
                    else if(missile.wave>=2 && missile.DodgeWindow)key=Key.Space;
                    if(missile.wave==1 && missile.state==MissileState.Hit && !hitRecorded) {hitRecorded=true;report.missileMisses++;}
                }
            }
            if(keyboard!=null)InputSystem.QueueStateEvent(keyboard,key==Key.None?new KeyboardState():new KeyboardState(key));
            report.frames++;
        }
        IEnumerator CaptureAfterFrame(int index)
        {
            yield return new WaitForSecondsRealtime(1);
            string folder=Path.Combine(Path.GetDirectoryName(Application.dataPath),"Documentation/FrogEscape/Screenshots");Directory.CreateDirectory(folder);
            ScreenCapture.CaptureScreenshot(Path.Combine(folder,report.difficulty+"-scene-"+index+".png"));
        }
        void Finish()
        {
            Time.timeScale=1;autopilot=false;forced=Key.None;finished=true;
            if(keyboard!=null)InputSystem.QueueStateEvent(keyboard,new KeyboardState());
            report.wallSeconds=Time.realtimeSinceStartup-startedAt;report.averageEditorFps=fpsSamples/Mathf.Max(.001f,fpsTotal);report.status=report.failures.Count==0?"PASS":"FAIL";
            string path=Path.Combine(Path.GetDirectoryName(Application.dataPath),"Documentation/FrogEscape/Playtest-"+report.difficulty+".json");File.WriteAllText(path,JsonUtility.ToJson(report,true));
            Debug.Log("Frog Escape acceptance "+report.difficulty+": "+report.status+" ("+report.checks.Count+" checks)");
        }
    }
}
#endif
