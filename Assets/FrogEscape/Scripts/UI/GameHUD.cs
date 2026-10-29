using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace FrogEscape
{
    public class GameHUD : MonoBehaviour
    {
        public StageController stage;
        static TMP_FontAsset koreanFont;
        static Sprite rounded;
        RectTransform root,overlay,playHUD,feedbackCard,warningCard;
        TextMeshProUGUI title,subtitle,health,chapter,feedback,warning,dodge,tutorial,modeLabel,stats;
        UnityEngine.UI.Image progressFill,dodgeFill,fade,hitFlash,warningFill;
        readonly List<UnityEngine.UI.Button> buttons=new List<UnityEngine.UI.Button>();
        GameMode previous=(GameMode)(-1);
        int selected;
        float lastBeep;
        readonly Color ink=new Color(.12f,.23f,.48f,.97f),cyan=new Color(.48f,1,.22f),yellow=new Color(1,.86f,.18f);
        static readonly string[] Chapters={"STAGE 1   고가도로!","STAGE 2   하수도!","STAGE 3   유도탄 추격!"};
        static readonly string[] Names={"고가도로 · 도시를 가로질러","하수도 · 조준선을 피하세요","외부 도로 · 마지막 추격"};
        void Awake()
        {
            if(koreanFont==null)
            {
                koreanFont=TMP_FontAsset.CreateFontAsset("Malgun Gothic","Regular",64);
                if(koreanFont==null) koreanFont=TMP_FontAsset.CreateFontAsset("Arial","Regular",64);
                if(koreanFont) DontDestroyOnLoad(koreanFont);
            }
            Build();
        }
        static Sprite Rounded()
        {
            if(rounded) return rounded;
            var texture=new Texture2D(64,64,TextureFormat.RGBA32,false);
            texture.name="Arcade rounded panel";texture.wrapMode=TextureWrapMode.Clamp;
            for(int y=0;y<64;y++) for(int x=0;x<64;x++)
            {
                float dx=Mathf.Max(Mathf.Abs(x-31.5f)-15.5f,0),dy=Mathf.Max(Mathf.Abs(y-31.5f)-15.5f,0);
                texture.SetPixel(x,y,new Color(1,1,1,Mathf.Clamp01(16-Mathf.Sqrt(dx*dx+dy*dy))));
            }
            texture.Apply();DontDestroyOnLoad(texture);
            rounded=Sprite.Create(texture,new Rect(0,0,64,64),Vector2.one*.5f,100,0,SpriteMeshType.FullRect,new Vector4(18,18,18,18));
            DontDestroyOnLoad(rounded);return rounded;
        }
        RectTransform Box(string name,Transform parent,Vector2 anchor,Vector2 pivot,Vector2 position,Vector2 size,Color color)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(UnityEngine.UI.Image));
            go.transform.SetParent(parent,false);
            var rt=(RectTransform)go.transform;
            rt.anchorMin=rt.anchorMax=anchor; rt.pivot=pivot; rt.anchoredPosition=position; rt.sizeDelta=size;
            var img=go.GetComponent<UnityEngine.UI.Image>(); img.color=color; img.raycastTarget=false;
            if(color.a>.7f && size.y>35)
            {
                img.sprite=Rounded();img.type=UnityEngine.UI.Image.Type.Sliced;
                var shadow=go.AddComponent<UnityEngine.UI.Shadow>();shadow.effectColor=new Color(.035f,.075f,.2f,.75f);shadow.effectDistance=new Vector2(0,-7);
                var outline=go.AddComponent<UnityEngine.UI.Outline>();outline.effectColor=new Color(.035f,.075f,.2f,1);outline.effectDistance=new Vector2(3,-3);
            }
            return rt;
        }
        TextMeshProUGUI Label(string name,Transform parent,Vector2 anchor,Vector2 pivot,Vector2 pos,Vector2 size,float fontSize,Color color,string text="")
        {
            var go=new GameObject(name,typeof(RectTransform)); go.transform.SetParent(parent,false);
            var rt=(RectTransform)go.transform; rt.anchorMin=rt.anchorMax=anchor; rt.pivot=pivot; rt.anchoredPosition=pos; rt.sizeDelta=size;
            var label=go.AddComponent<TextMeshProUGUI>(); label.font=koreanFont; label.fontSize=fontSize; label.color=color; label.text=text;
            label.raycastTarget=false; label.textWrappingMode=TextWrappingModes.Normal; label.alignment=TextAlignmentOptions.MidlineLeft;
            label.fontStyle=FontStyles.Bold;
            var shadow=go.AddComponent<UnityEngine.UI.Shadow>();shadow.effectColor=new Color(.025f,.06f,.16f,.9f);shadow.effectDistance=new Vector2(2,-2);
            return label;
        }
        void Fill(RectTransform rt) { rt.anchorMin=Vector2.zero;rt.anchorMax=Vector2.one;rt.offsetMin=rt.offsetMax=Vector2.zero; }
        void Build()
        {
            var canvas=gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay; canvas.sortingOrder=20;
            var scaler=gameObject.AddComponent<UnityEngine.UI.CanvasScaler>(); scaler.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
            gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>(); root=(RectTransform)transform;
            if(FindFirstObjectByType<EventSystem>()==null)
            {
                var events=new GameObject("EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));
                var module=events.GetComponent<InputSystemUIInputModule>();module.AssignDefaultActions();module.move=null;module.submit=null;module.cancel=null;
            }
            playHUD=Box("HUD",root,Vector2.zero,Vector2.zero,Vector2.zero,Vector2.one,Color.clear);Fill(playHUD);
            var top=Box("ChapterCard",playHUD,new Vector2(0,1),new Vector2(0,1),new Vector2(42,-36),new Vector2(540,130),ink);
            chapter=Label("Chapter",top,new Vector2(0,1),new Vector2(0,1),new Vector2(28,-20),new Vector2(490,40),26,cyan);
            tutorial=Label("Location",top,new Vector2(0,1),new Vector2(0,1),new Vector2(28,-65),new Vector2(490,44),25,Color.white);
            var hp=Box("HealthCard",playHUD,Vector2.one,Vector2.one,new Vector2(-42,-36),new Vector2(330,130),ink);
            Label("HealthCaption",hp,new Vector2(0,1),new Vector2(0,1),new Vector2(24,-15),new Vector2(280,32),22,Color.white,"LIFE   남은 체력");
            health=Label("Health",hp,new Vector2(0,1),new Vector2(0,1),new Vector2(24,-52),new Vector2(280,55),38,yellow);
            var bar=Box("ProgressTrack",playHUD,new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(0,-48),new Vector2(430,28),ink);
            progressFill=Box("Progress",bar,Vector2.zero,Vector2.zero,new Vector2(4,4),new Vector2(0,20),cyan).GetComponent<UnityEngine.UI.Image>();
            Label("Goal",bar,new Vector2(.5f,0),new Vector2(.5f,1),new Vector2(0,-12),new Vector2(430,32),22,Color.white,"GO!  ━━━━━━━━━  FINISH").alignment=TextAlignmentOptions.Center;
            warningCard=Box("WarningCard",playHUD,new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(0,-175),new Vector2(1160,106),ink);
            warning=Label("AttackWarning",warningCard,new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(0,-6),new Vector2(1120,80),33,yellow);warning.alignment=TextAlignmentOptions.Center;
            warningFill=Box("TimingMeter",warningCard,Vector2.zero,Vector2.zero,new Vector2(24,12),new Vector2(0,8),yellow).GetComponent<UnityEngine.UI.Image>();
            feedbackCard=Box("FeedbackCard",playHUD,new Vector2(.5f,0),new Vector2(.5f,0),new Vector2(0,165),new Vector2(1200,70),ink);
            feedback=Label("Feedback",feedbackCard,Vector2.one*.5f,Vector2.one*.5f,Vector2.zero,new Vector2(1160,60),30,cyan);feedback.alignment=TextAlignmentOptions.Center;
            var control=Box("Controls",playHUD,new Vector2(0,0),Vector2.zero,new Vector2(42,36),new Vector2(780,96),ink);
            Label("Keys",control,new Vector2(0,.5f),new Vector2(0,.5f),new Vector2(25,0),new Vector2(730,70),23,Color.white,"A / D  이동     SPACE  회피     ESC  일시정지");
            var dodgeCard=Box("DodgeCard",playHUD,new Vector2(1,0),new Vector2(1,0),new Vector2(-42,36),new Vector2(360,96),ink);
            dodge=Label("Dodge",dodgeCard,new Vector2(0,1),new Vector2(0,1),new Vector2(22,-10),new Vector2(320,55),27,cyan);
            dodgeFill=Box("Cooldown",dodgeCard,Vector2.zero,Vector2.zero,new Vector2(22,16),new Vector2(316,6),cyan).GetComponent<UnityEngine.UI.Image>();
            overlay=Box("Menu",root,Vector2.zero,Vector2.zero,Vector2.zero,Vector2.one,Color.clear);Fill(overlay);
            hitFlash=Box("DamageFlash",root,Vector2.zero,Vector2.zero,Vector2.zero,Vector2.one,new Color(1,.2f,.1f,0)).GetComponent<UnityEngine.UI.Image>();Fill(hitFlash.rectTransform);
            fade=Box("SceneFade",root,Vector2.zero,Vector2.zero,Vector2.zero,Vector2.one,Color.clear).GetComponent<UnityEngine.UI.Image>();Fill(fade.rectTransform);
        }
        UnityEngine.UI.Button AddButton(Transform parent,string text,float y,UnityEngine.Events.UnityAction action)
        {
            Color buttonColor=buttons.Count==0?new Color(.25f,.78f,.10f):buttons.Count==1?new Color(.13f,.52f,.98f):new Color(1,.42f,.17f);
            var rt=Box(text,parent,new Vector2(0,1),new Vector2(0,1),new Vector2(48,y),new Vector2(560,74),buttonColor);
            var image=rt.GetComponent<UnityEngine.UI.Image>();image.raycastTarget=true;
            var b=rt.gameObject.AddComponent<UnityEngine.UI.Button>();b.targetGraphic=image;b.onClick.AddListener(action);
            var colors=b.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(1.2f,1.2f,1.2f);colors.selectedColor=colors.highlightedColor;colors.pressedColor=new Color(.72f,.78f,.9f);colors.fadeDuration=.08f;b.colors=colors;
            Box("Button Shine",rt,new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(0,-6),new Vector2(520,5),new Color(1,1,1,.3f));
            Label("Label",rt,new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(520,68),29,Color.white,text).alignment=TextAlignmentOptions.Center;
            buttons.Add(b);return b;
        }
        void RebuildMenu(GameMode mode)
        {
            foreach(Transform child in overlay) Destroy(child.gameObject);
            buttons.Clear();selected=0;
            overlay.gameObject.SetActive(mode==GameMode.Menu || mode==GameMode.Paused || mode==GameMode.Failed || mode==GameMode.Cleared);
            if(!overlay.gameObject.activeSelf) return;
            bool menu=mode==GameMode.Menu;
            if(!menu) Box("Dim",overlay,new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(2400,1400),new Color(0,.02f,.035f,.6f));
            var card=Box("Card",overlay,new Vector2(menu?0:.5f,.5f),new Vector2(menu?0:.5f,.5f),new Vector2(menu?85:0,0),new Vector2(656,850),ink);
            var badge=Box("Adventure Badge",card,new Vector2(.5f,1),new Vector2(.5f,.5f),new Vector2(0,-23),new Vector2(360,56),new Color(1,.57f,.12f));
            Label("Eyebrow",badge,Vector2.one*.5f,Vector2.one*.5f,Vector2.zero,new Vector2(340,48),24,Color.white,"★  개구리 대탈출  ★").alignment=TextAlignmentOptions.Center;
            title=Label("Title",card,new Vector2(0,1),new Vector2(0,1),new Vector2(44,-78),new Vector2(580,200),menu?88:65,cyan,menu?"FROG\n<color=#FFDE39>ESCAPE!</color>":mode==GameMode.Paused?"잠깐 쉬어가기!":mode==GameMode.Failed?"앗! 다시 도전!":"YOU WIN!\n<color=#FFDE39>탈출 성공!</color>");
            title.alignment=TextAlignmentOptions.Center;
            subtitle=Label("Subtitle",card,new Vector2(0,1),new Vector2(0,1),new Vector2(48,-290),new Vector2(560,100),25,Color.white,menu?"달리고! 피하고! 추격을 뒤집고!\n3개 스테이지 · 약 4분의 모험":mode==GameMode.Paused?"준비되면 이어서 달려요.\nESC 키로도 돌아갈 수 있어요.":mode==GameMode.Failed?"현재 스테이지에서 바로 재도전해요.\n경고를 보고, 빈 공간으로 이동하세요.":$"추격을 따돌렸어요!\n회피 {stage.Session.dodges}회  ·  플레이 {stage.Session.runTime:F0}초");
            subtitle.alignment=TextAlignmentOptions.Center;
            if(menu)
            {
                AddButton(card,"시작하기   [ ENTER ]",-420,()=>stage.Session.StartGame());
                var difficulty=AddButton(card,"",-505,()=>{stage.Session.normalMode=!stage.Session.normalMode;UpdateDifficulty();});
                modeLabel=difficulty.GetComponentInChildren<TextMeshProUGUI>();UpdateDifficulty();
                AddButton(card,"게임 종료",-590,Quit);
            }
            else if(mode==GameMode.Paused)
            { AddButton(card,"계속하기   [ ENTER ]",-420,()=>stage.Session.TogglePause());AddButton(card,"현재 스테이지 다시 시작",-505,()=>stage.Session.Retry());AddButton(card,"처음 화면으로",-590,()=>stage.Session.ReturnToMenu()); }
            else if(mode==GameMode.Failed)
            { AddButton(card,"이 스테이지 다시 도전   [ ENTER ]",-420,()=>stage.Session.Retry());AddButton(card,"처음 화면으로",-505,()=>stage.Session.ReturnToMenu()); }
            else
            { AddButton(card,"한 번 더 달리기   [ ENTER ]",-420,()=>stage.Session.StartGame());AddButton(card,"처음 화면으로",-505,()=>stage.Session.ReturnToMenu()); }
            stats=Label("Footer",card,new Vector2(0,0),Vector2.zero,new Vector2(48,40),new Vector2(560,95),21,new Color(.65f,.76f,.79f),"← →  좌우 이동    SPACE  회피\n메뉴 선택: ↑ ↓  /  ENTER  또는 마우스");
            if(buttons.Count>0 && EventSystem.current) EventSystem.current.SetSelectedGameObject(buttons[0].gameObject);
        }
        void UpdateDifficulty() { if(modeLabel) modeLabel.text=stage.Session.normalMode?"난이도  NORMAL  ·  체력 3":"난이도  EASY  ·  체력 4  [추천]"; }
        void Quit()
        {
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying=false;
            #else
            Application.Quit();
            #endif
        }
        void Update()
        {
            var session=stage.Session;if(session==null)return;
            if(previous!=session.mode) { previous=session.mode;RebuildMenu(previous); }
            playHUD.gameObject.SetActive(session.mode==GameMode.Playing || session.mode==GameMode.Cinematic || session.mode==GameMode.Paused);
            if(overlay.gameObject.activeSelf && Keyboard.current!=null)
            {
                // Keyboard navigation is handled here; InputSystemUIInputModule is used for pointer events only.
                var k=Keyboard.current;
                if(k.downArrowKey.wasPressedThisFrame || k.sKey.wasPressedThisFrame) selected=(selected+1)%Mathf.Max(1,buttons.Count);
                if(k.upArrowKey.wasPressedThisFrame || k.wKey.wasPressedThisFrame) selected=(selected-1+buttons.Count)%Mathf.Max(1,buttons.Count);
                if((k.upArrowKey.wasPressedThisFrame || k.downArrowKey.wasPressedThisFrame || k.wKey.wasPressedThisFrame || k.sKey.wasPressedThisFrame) && buttons.Count>0) EventSystem.current.SetSelectedGameObject(buttons[selected].gameObject);
                if(k.enterKey.wasPressedThisFrame && buttons.Count>0) buttons[selected].onClick.Invoke();
            }
            int index=Mathf.Clamp(stage.stageIndex,0,2);
            chapter.text=stage.stageIndex==3?"FINALE  /  THE GREAT ESCAPE":Chapters[index];
            tutorial.text=stage.stageIndex==3?"유도탄을 타고, 추격을 뒤집다":Names[index];
            health.text=new string('♥',Mathf.Max(0,session.health))+new string('♡',Mathf.Max(0,stage.Settings.health-session.health));health.color=new Color(1,.4f,.5f);
            progressFill.rectTransform.sizeDelta=new Vector2(422*stage.Progress,20);
            dodge.text=stage.player.dodge.CooldownProgress>=.99f?"SPACE   회피 준비 완료":"SPACE   충전 중…";
            dodgeFill.rectTransform.sizeDelta=new Vector2(316*stage.player.dodge.CooldownProgress,6);
            feedback.text=Time.time<stage.feedbackUntil?stage.feedback:stage.stageIndex==0 && stage.elapsed<12?"자동으로 달려요! A / D로 움직이고 SPACE로 피해요.":stage.stageIndex==1 && stage.elapsed<9?"노란 조준선이 보이면 옆으로! SPACE로도 회피할 수 있어요.":"";
            feedbackCard.gameObject.SetActive(!string.IsNullOrEmpty(feedback.text));
            warning.text="";warning.color=yellow;
            if(stage.beatRush && stage.beatRush.Presenting && stage.Playing)
            {
                var rush=stage.beatRush;
                warning.text=rush.Cue+"  /  "+rush.Countdown.ToString("0.0")+"s";
                warning.color=rush.JumpRequired?yellow:cyan;
                tutorial.text="BEAT RUSH  /  COMBO "+rush.Combo+"  /  CLEAR "+rush.Cleared;
            }
            if(stage.stageIndex==1 && stage.attack.IsWarning) warning.text="!   "+stage.attack.PatternName+"   ·   조준선에서 벗어나세요";
            if(stage.stageIndex==2)
            {
                var missile=stage.missile;
                if(missile.state==MissileState.Pursue || missile.state==MissileState.LockOn) warning.text=$"유도탄 {missile.wave} / 3   ·   초록 신호를 기다리세요";
                if(missile.state==MissileState.Warning || missile.state==MissileState.Dash)
                {
                    warning.text=missile.DodgeWindow?"지금!   [ SPACE ]   회피!":"잠깐!   조준 중…";
                    warning.color=missile.DodgeWindow?cyan:yellow;
                    if(missile.DodgeWindow && Time.time-lastBeep>.42f && stage.Playing) { lastBeep=Time.time;stage.audioSystem.Play(stage.config.warning,.55f); }
                }
            }
            float alpha=session.mode==GameMode.Loading?1:0;
            if(session.mode==GameMode.Cinematic)
            {
                warning.text=stage.cinematic.sequence==0?"WALL RIDE  /  벽을 타고 아래로!":stage.cinematic.sequence==1?"BREAKOUT  /  새로운 추격이 시작된다":"THE SWITCH  /  이제 내 차례!";
                if(stage.cinematic.sequence<2) alpha=Mathf.InverseLerp(.90f,.98f,stage.cinematic.progress);
            }
            fade.color=new Color(.015f,.04f,.06f,alpha);
            warningCard.gameObject.SetActive(!string.IsNullOrEmpty(warning.text));
            bool timed=stage.stageIndex==2 && (stage.missile.state==MissileState.Warning || stage.missile.state==MissileState.Dash);
            warningFill.gameObject.SetActive(timed && session.mode!=GameMode.Cinematic);
            if(timed)
            {
                warningFill.color=stage.missile.DodgeWindow?cyan:yellow;
                warningFill.rectTransform.sizeDelta=new Vector2(1112*(stage.missile.state==MissileState.Dash?1:Mathf.Clamp01(stage.missile.timer/stage.missile.WarningDuration)),8);
            }
            float flash=stage.Playing?Mathf.Clamp01((stage.playerHealth.invincibleUntil-Time.time)/stage.Settings.invulnerability):0;
            hitFlash.color=new Color(1,.18f,.1f,flash*.14f);
        }
    }
}
