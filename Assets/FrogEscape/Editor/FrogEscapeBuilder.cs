using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;

namespace FrogEscape.Editor
{
    // Scene generation is supplemented by FeelPassSetup and ArcadeVisualUpgrade.
    public static class FrogEscapeBuilder
    {
        const string Root="Assets/FrogEscape";
        static readonly Dictionary<string,Material> materials=new Dictionary<string,Material>();
        static GameConfig config;
        static System.Random random;
        static GameObject bikePrefab,policePrefab,missilePrefab;
        public static readonly string[] SceneNames={"MainMenu","Stage01_Overpass","Stage02_Sewer","Stage03_MissileChase","Ending"};
        [MenuItem("Frog Escape/Create Game Assets (new scenes only)")]
        public static void BuildAll()
        {
            if(EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before creating scenes.");
            var active=SceneManager.GetActiveScene();
            if(active.isDirty) throw new InvalidOperationException("Save the open scene before generating Frog Escape scenes.");
            foreach(var folder in new[]{"Scenes","Prefabs","Materials","Models","Animations","Timelines","Audio","ScriptableObjects","Resources"}) Directory.CreateDirectory(Root+"/"+folder);
            AssetDatabase.Refresh();
            MakeMaterials();
            config=AssetDatabase.LoadAssetAtPath<GameConfig>(Root+"/ScriptableObjects/GameConfig.asset");
            if(config==null) { config=ScriptableObject.CreateInstance<GameConfig>();AssetDatabase.CreateAsset(config,Root+"/ScriptableObjects/GameConfig.asset"); }
            config.menuScene=Root+"/Scenes/MainMenu.unity";config.endingScene=Root+"/Scenes/Ending.unity";
            config.stageScenes=SceneNames.Skip(1).Take(3).Select(x=>Root+"/Scenes/"+x+".unity").ToArray();
            config.transitions=new[]{Timeline("WallRide",10),Timeline("SewerBreakout",10)};config.ending=Timeline("MissileSwitch",17);
            config.warningMaterial=materials["Warning"];config.successMaterial=materials["MintGlow"];
            MakeAudio();
            bikePrefab=MakeBike();policePrefab=MakePolice();missilePrefab=MakeMissile();
            for(int i=0;i<SceneNames.Length;i++)
            {
                string path=Root+"/Scenes/"+SceneNames[i]+".unity";
                if(!File.Exists(path)) MakeScene(i-1,path);
            }
            var added=SceneNames.Select(x=>new EditorBuildSettingsScene(Root+"/Scenes/"+x+".unity",true)).ToList();
            added.AddRange(EditorBuildSettings.scenes.Where(s=>!added.Any(n=>n.path==s.path)));
            EditorBuildSettings.scenes=added.ToArray();
            EditorUtility.SetDirty(config);AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(config.menuScene);
            Debug.Log("Frog Escape: 5 scenes, original assets, Timeline transitions and audio created. Original scenes retained.");
        }
        static Material Mat(string name,Color color,bool emissive=false)
        {
            string path=Root+"/Materials/"+name+".mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null) { material=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(material,path); }
            material.SetColor("_BaseColor",color);material.SetFloat("_Smoothness",name=="Water"?.88f:.24f);
            if(emissive) { material.EnableKeyword("_EMISSION");material.SetColor("_EmissionColor",color*1.2f); }
            materials[name]=material;return material;
        }
        static void MakeMaterials()
        {
            Mat("Asphalt",new Color(.12f,.18f,.23f));Mat("Concrete",new Color(.43f,.52f,.57f));Mat("ConcreteDark",new Color(.2f,.29f,.34f));
            Mat("Paint",new Color(.88f,.94f,.88f));Mat("Mint",new Color(.12f,.85f,.66f));Mat("MintGlow",new Color(.12f,1,.72f),true);
            Mat("Lime",new Color(.55f,.94f,.2f));Mat("Frog",new Color(.22f,.68f,.3f));Mat("Belly",new Color(.78f,.95f,.48f));
            Mat("Black",new Color(.025f,.055f,.07f));Mat("Chrome",new Color(.5f,.64f,.66f));Mat("Glass",new Color(.07f,.4f,.52f));
            Mat("Orange",new Color(1,.33f,.12f));Mat("Warning",new Color(1,.72f,.08f),true);Mat("RedGlow",new Color(1,.1f,.12f),true);Mat("BlueGlow",new Color(.05f,.4f,1),true);
            Mat("Water",new Color(.025f,.36f,.4f));Mat("Sand",new Color(.65f,.47f,.32f));Mat("Rock",new Color(.44f,.3f,.24f));
            Mat("BuildingA",new Color(.18f,.33f,.4f));Mat("BuildingB",new Color(.28f,.43f,.49f));Mat("BuildingC",new Color(.4f,.54f,.57f));
        }
        static GameObject Part(string name,PrimitiveType type,Transform parent,Vector3 pos,Vector3 scale,string material,Vector3 angles=default)
        {
            var go=GameObject.CreatePrimitive(type);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=pos;go.transform.localScale=scale;go.transform.localRotation=Quaternion.Euler(angles);
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());go.GetComponent<Renderer>().sharedMaterial=materials[material];return go;
        }
        static GameObject SavePrefab(GameObject root,string name)
        {
            string path=Root+"/Prefabs/"+name+".prefab";
            var old=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if(old!=null) { UnityEngine.Object.DestroyImmediate(root);return old; }
            var prefab=PrefabUtility.SaveAsPrefabAsset(root,path);UnityEngine.Object.DestroyImmediate(root);return prefab;
        }
        static GameObject MakeBike()
        {
            var root=new GameObject("Frog Rider");var visual=new GameObject("Visual").transform;visual.SetParent(root.transform,false);
            Part("Fairing",PrimitiveType.Cube,visual,new Vector3(0,.85f,.25f),new Vector3(1.1f,.7f,2.4f),"Mint",new Vector3(-8,0,0));
            Part("Nose",PrimitiveType.Sphere,visual,new Vector3(0,.95f,1.28f),new Vector3(1.05f,.6f,.8f),"Mint");
            Part("Seat",PrimitiveType.Cube,visual,new Vector3(0,1.25f,-.45f),new Vector3(.8f,.22f,1.35f),"Black");
            Part("Headlight",PrimitiveType.Cube,visual,new Vector3(0,1.1f,1.65f),new Vector3(.66f,.18f,.08f),"Warning");
            Part("TailLight",PrimitiveType.Cube,visual,new Vector3(0,1,-1.15f),new Vector3(.6f,.16f,.1f),"RedGlow");
            for(int z=-1;z<=1;z+=2)
            {
                Part("Tire",PrimitiveType.Cylinder,visual,new Vector3(0,.57f,z*1.1f),new Vector3(1.1f,.22f,1.1f),"Black",new Vector3(0,0,90));
                Part("WheelHub",PrimitiveType.Cylinder,visual,new Vector3(0,.57f,z*1.1f),new Vector3(.65f,.24f,.65f),"Chrome",new Vector3(0,0,90));
            }
            Part("Handlebar",PrimitiveType.Cylinder,visual,new Vector3(0,1.55f,.85f),new Vector3(.12f,.68f,.12f),"Chrome",new Vector3(0,0,90));
            var rider=new GameObject("Rider").transform;rider.SetParent(visual,false);rider.localPosition=new Vector3(0,1.25f,-.25f);
            Part("Body",PrimitiveType.Sphere,rider,new Vector3(0,.4f,0),new Vector3(.9f,1.1f,.8f),"Frog");
            Part("Belly",PrimitiveType.Sphere,rider,new Vector3(0,.42f,.32f),new Vector3(.67f,.7f,.25f),"Belly");
            Part("Head",PrimitiveType.Sphere,rider,new Vector3(0,1.12f,.18f),new Vector3(1.2f,.85f,.95f),"Lime");
            Part("Smile",PrimitiveType.Cube,rider,new Vector3(0,.98f,.64f),new Vector3(.52f,.055f,.035f),"Black");
            for(int side=-1;side<=1;side+=2)
            {
                Part("Eye",PrimitiveType.Sphere,rider,new Vector3(side*.4f,1.48f,.43f),new Vector3(.52f,.5f,.45f),"Belly");
                Part("GoggleRim",PrimitiveType.Sphere,rider,new Vector3(side*.4f,1.47f,.61f),new Vector3(.47f,.4f,.17f),"Black");
                Part("GoggleGlass",PrimitiveType.Sphere,rider,new Vector3(side*.4f,1.47f,.7f),new Vector3(.36f,.29f,.1f),"Glass");
                Part("GoggleGlint",PrimitiveType.Sphere,rider,new Vector3(side*.4f-.06f,1.53f,.75f),new Vector3(.09f,.07f,.035f),"Paint");
                Part("Arm",PrimitiveType.Capsule,rider,new Vector3(side*.5f,.35f,.45f),new Vector3(.24f,.46f,.24f),"Frog",new Vector3(55,0,side*-20));
                Part("Leg",PrimitiveType.Capsule,rider,new Vector3(side*.53f,-.2f,-.15f),new Vector3(.3f,.5f,.3f),"Frog",new Vector3(-25,0,side*10));
                Part("Boot",PrimitiveType.Cube,rider,new Vector3(side*.58f,-.55f,.05f),new Vector3(.35f,.24f,.65f),"Orange");
            }
            Part("Scarf",PrimitiveType.Cube,rider,new Vector3(0,.77f,.12f),new Vector3(1,.14f,.8f),"Orange");
            Part("ScarfTail",PrimitiveType.Cube,rider,new Vector3(.35f,.85f,-.65f),new Vector3(.25f,.1f,1.3f),"Orange",new Vector3(-15,-12,8));
            return SavePrefab(root,"FrogBike");
        }
        static GameObject MakePolice()
        {
            var root=new GameObject("Pursuit Unit");var t=root.transform;
            Part("Body",PrimitiveType.Cube,t,new Vector3(0,.75f,0),new Vector3(2.3f,.65f,4.5f),"Paint");
            Part("Stripe",PrimitiveType.Cube,t,new Vector3(0,.88f,0),new Vector3(2.34f,.22f,4.3f),"Black");
            Part("Cabin",PrimitiveType.Cube,t,new Vector3(0,1.35f,-.3f),new Vector3(1.95f,.65f,2.15f),"Glass");
            Part("Roof",PrimitiveType.Cube,t,new Vector3(0,1.72f,-.3f),new Vector3(2.05f,.13f,2.2f),"Black");
            Part("RedLamp",PrimitiveType.Cube,t,new Vector3(-.5f,1.95f,-.3f),new Vector3(.65f,.22f,.4f),"RedGlow");
            Part("BlueLamp",PrimitiveType.Cube,t,new Vector3(.5f,1.95f,-.3f),new Vector3(.65f,.22f,.4f),"BlueGlow");
            for(int x=-1;x<=1;x+=2)for(int z=-1;z<=1;z+=2)
                Part("Wheel",PrimitiveType.Cylinder,t,new Vector3(x*1.1f,.5f,z*1.4f),new Vector3(.9f,.18f,.9f),"Black",new Vector3(0,0,90));
            for(int x=-1;x<=1;x+=2)Part("Headlight",PrimitiveType.Cube,t,new Vector3(x*.7f,.9f,2.28f),new Vector3(.4f,.22f,.06f),"Warning");
            return SavePrefab(root,"PoliceCar");
        }
        static GameObject MakeMissile()
        {
            var root=new GameObject("Comet Chaser");var model=new GameObject("Model").transform;model.SetParent(root.transform,false);
            Part("Body",PrimitiveType.Capsule,model,Vector3.zero,new Vector3(.7f,1.4f,.7f),"Orange",new Vector3(90,0,0));
            Part("Nose",PrimitiveType.Sphere,model,new Vector3(0,0,1.1f),new Vector3(.72f,.72f,.8f),"Paint");
            Part("FinA",PrimitiveType.Cube,model,new Vector3(0,0,-.7f),new Vector3(1.6f,.13f,.7f),"Black");
            Part("FinB",PrimitiveType.Cube,model,new Vector3(0,0,-.7f),new Vector3(.13f,1.3f,.7f),"Black");
            Part("Flame",PrimitiveType.Sphere,model,new Vector3(0,0,-1.5f),new Vector3(.45f,.45f,1),"Warning");
            return SavePrefab(root,"CometMissile");
        }
        static TimelineAsset Timeline(string name,float duration)
        {
            string path=Root+"/Timelines/"+name+".playable";var asset=AssetDatabase.LoadAssetAtPath<TimelineAsset>(path);
            if(asset!=null) return asset;
            asset=ScriptableObject.CreateInstance<TimelineAsset>();AssetDatabase.CreateAsset(asset,path);
            var track=asset.CreateTrack<FrogCinematicTrack>(null,name+" choreography");
            var clip=track.CreateClip<FrogCinematicClip>();clip.start=0;clip.duration=duration;clip.displayName=name;
            asset.durationMode=TimelineAsset.DurationMode.FixedLength;asset.fixedDuration=duration;
            EditorUtility.SetDirty(asset);return asset;
        }
        static void MakeAudio()
        {
            config.music=Wave("Original_Pulse",16,0);config.engine=Wave("Bike_Engine",2,1);config.siren=Wave("Comic_Siren",2,2);
            config.warning=Wave("Warning",.23f,3);config.dodge=Wave("Dodge",.3f,4);config.impact=Wave("Comic_Impact",.45f,5);config.landing=Wave("Landing",.3f,5);config.victory=Wave("Victory",1.8f,6);
        }
        static AudioClip Wave(string name,float duration,int kind)
        {
            string path=Root+"/Audio/"+name+".wav";
            if(!File.Exists(path))
            {
                const int rate=22050;int samples=(int)(rate*duration);var noise=new System.Random(42);
                using(var writer=new BinaryWriter(File.Create(path)))
                {
                    writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));writer.Write(36+samples*2);writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));writer.Write(16);writer.Write((short)1);writer.Write((short)1);writer.Write(rate);writer.Write(rate*2);writer.Write((short)2);writer.Write((short)16);writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));writer.Write(samples*2);
                    int[] notes={0,7,12,7,3,10,15,10,5,12,17,12,7,14,19,14};
                    for(int i=0;i<samples;i++)
                    {
                        float t=(float)i/rate,v=0,env=Mathf.Sin(Mathf.PI*Mathf.Clamp01(t/duration));
                        if(kind==0)
                        {
                            float beat=t%.25f;int step=(int)(t/.25f);float freq=130.81f*Mathf.Pow(2,notes[step%notes.Length]/12f);
                            v=.28f*Mathf.Sin(2*Mathf.PI*freq*t)*Mathf.Exp(-beat*10)+.2f*Mathf.Sin(2*Mathf.PI*65.4f*t);
                            float kick=t%.5f;v+=.35f*Mathf.Sin(2*Mathf.PI*(58*kick+20*(1-Mathf.Exp(-kick*30))/30))*Mathf.Exp(-kick*20);
                            v+=(float)(noise.NextDouble()*2-1)*.09f*Mathf.Exp(-beat*70);
                        }
                        else if(kind==1) v=.45f*Mathf.Sin(2*Mathf.PI*55*t)+.2f*Mathf.Sin(2*Mathf.PI*110*t);
                        else if(kind==2) v=.5f*Mathf.Sin(2*Mathf.PI*520*t+80*Mathf.Sin(Mathf.PI*t));
                        else if(kind==3) v=.55f*Mathf.Sin(2*Mathf.PI*880*t)*env;
                        else if(kind==4) v=.45f*Mathf.Sin(2*Mathf.PI*(360*t+900*t*t))*env;
                        else if(kind==5) v=((float)(noise.NextDouble()*2-1)*.5f+.4f*Mathf.Sin(2*Mathf.PI*90*t))*Mathf.Exp(-t*9);
                        else { int[] tune={0,4,7,12,12,12};float f=523.25f*Mathf.Pow(2,tune[Mathf.Min(5,(int)(t/.3f))]/12f);v=.45f*Mathf.Sin(2*Mathf.PI*f*t)*Mathf.Exp(-(t%.3f)*7); }
                        v*=Mathf.Min(1,t*100)*Mathf.Min(1,(duration-t)*100);writer.Write((short)(Mathf.Clamp(v,-1,1)*26000));
                    }
                }
            }
            AssetDatabase.ImportAsset(path);
            var importer=(AudioImporter)AssetImporter.GetAtPath(path);var settings=importer.defaultSampleSettings;settings.loadType=AudioClipLoadType.DecompressOnLoad;settings.compressionFormat=AudioCompressionFormat.Vorbis;settings.quality=.7f;importer.defaultSampleSettings=settings;importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }
        static void MakeScene(int index,string path)
        {
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            random=new System.Random(130+index);
            var root=new GameObject("Frog Escape — "+SceneNames[index+1]);var stage=root.AddComponent<StageController>();stage.stageIndex=index;stage.config=config;
            var road=new GameObject("Route").AddComponent<RoadPath>();stage.road=road;road.style=index==1?1:index>=2?2:0;road.length=index<0?360:index==3?700:index==2?3100:2300;
            CreateEnvironment(road,index);
            var bike=(GameObject)PrefabUtility.InstantiatePrefab(bikePrefab);bike.name="Player";
            var player=bike.AddComponent<BikeController>();player.stage=stage;player.visual=bike.transform.Find("Visual");player.rider=player.visual.Find("Rider");stage.player=player;
            var dodge=bike.AddComponent<DodgeController>();dodge.stage=stage;player.dodge=dodge;
            var health=bike.AddComponent<PlayerHealth>();health.stage=stage;stage.playerHealth=health;
            bike.transform.SetPositionAndRotation(road.Position(index<0?80:45,0,.15f),road.Rotation(45));
            var police=(GameObject)PrefabUtility.InstantiatePrefab(policePrefab);police.name="Police Pursuit";stage.police=police.AddComponent<PoliceAI>();stage.police.stage=stage;
            stage.police.redLamp=police.transform.Find("RedLamp");stage.police.blueLamp=police.transform.Find("BlueLamp");police.transform.position=road.Position(20,2);
            var cameraObject=new GameObject("Chase Camera",typeof(Camera),typeof(AudioListener),typeof(CameraController));cameraObject.tag="MainCamera";
            var camera=cameraObject.GetComponent<Camera>();camera.nearClipPlane=.15f;camera.farClipPlane=320;camera.fieldOfView=65;camera.allowHDR=true;
            var follow=cameraObject.GetComponent<CameraController>();follow.view=camera;follow.stage=stage;stage.chaseCamera=follow;
            if(index==1) { follow.height=4.2f;follow.followDistance=12; }
            cameraObject.transform.SetPositionAndRotation(road.Position(25,0,7),Quaternion.LookRotation(road.Position(55,0,1)-road.Position(25,0,7)));
            var obstacles=new GameObject("Safe Obstacle Rows").AddComponent<ObstacleSpawner>();obstacles.stage=stage;stage.obstacles=obstacles;
            if(index==0) MakeObstacles(stage);
            var attack=new GameObject("Police Attack FSM").AddComponent<PoliceAttackController>();attack.stage=stage;stage.attack=attack;
            attack.warningZone=Part("Bullet Warning Zone",PrimitiveType.Cube,attack.transform,Vector3.zero,new Vector3(2.4f,.03f,10),"Warning").transform;attack.warningZone.gameObject.SetActive(false);
            attack.pool=new ProjectileController[6];
            for(int i=0;i<attack.pool.Length;i++)
            {
                var shot=Part("Pooled Cartoon Pulse "+i,PrimitiveType.Sphere,attack.transform,Vector3.zero,new Vector3(.6f,.6f,1.4f),"Warning");
                attack.pool[i]=shot.AddComponent<ProjectileController>();attack.pool[i].stage=stage;shot.SetActive(false);
            }
            var missile=(GameObject)PrefabUtility.InstantiatePrefab(missilePrefab);missile.name="Missile FSM";stage.missile=missile.AddComponent<MissileController>();stage.missile.stage=stage;stage.missile.model=missile.transform.Find("Model");stage.missile.model.gameObject.SetActive(false);
            stage.missile.warningZone=Part("Missile Warning Zone",PrimitiveType.Cube,root.transform,Vector3.zero,new Vector3(2.7f,.035f,9),"Orange").transform;stage.missile.warningZone.gameObject.SetActive(false);
            var cutscene=new GameObject("Timeline Director",typeof(PlayableDirector),typeof(CutsceneController));
            stage.cinematic=cutscene.GetComponent<CutsceneController>();stage.cinematic.stage=stage;stage.cinematic.director=cutscene.GetComponent<PlayableDirector>();stage.cinematic.director.playOnAwake=false;
            var endingMissile=(GameObject)PrefabUtility.InstantiatePrefab(missilePrefab);endingMissile.name="Cinematic Comet";endingMissile.SetActive(false);stage.cinematic.endingMissile=endingMissile.transform;
            var burstObject=new GameObject("Comic Confetti");var ps=burstObject.AddComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=ps.main;main.loop=false;main.playOnAwake=false;main.duration=1;main.startLifetime=2;main.startSpeed=12;main.startSize=.5f;main.gravityModifier=.8f;main.startColor=new ParticleSystem.MinMaxGradient(Color.yellow,new Color(1,.3f,.1f));
            var emission=ps.emission;emission.rateOverTime=0;emission.SetBursts(new[]{new ParticleSystem.Burst(0,80)});ps.GetComponent<ParticleSystemRenderer>().sharedMaterial=materials["Warning"];stage.cinematic.comicBurst=ps;
            var audio=new GameObject("Audio").AddComponent<GameAudio>();audio.stage=stage;stage.audioSystem=audio;
            audio.music=audio.gameObject.AddComponent<AudioSource>();audio.engine=audio.gameObject.AddComponent<AudioSource>();audio.sfx=audio.gameObject.AddComponent<AudioSource>();audio.siren=audio.gameObject.AddComponent<AudioSource>();
            foreach(var source in audio.GetComponents<AudioSource>()) { source.playOnAwake=false;source.spatialBlend=0; }
            var hud=new GameObject("Frog Escape UI",typeof(RectTransform),typeof(GameHUD));stage.hud=hud.GetComponent<GameHUD>();stage.hud.stage=stage;
            AddCinematicSet(stage);
            EditorSceneManager.SaveScene(scene,path);
        }
        static void AddCinematicSet(StageController stage)
        {
            if(stage.stageIndex<0 || stage.stageIndex>1 || stage.cinematic.transitionSet) return;
            var set=new GameObject("Transition Set").transform;stage.cinematic.transitionSet=set;
            if(stage.stageIndex==0)
            {
                Part("Wallride Face",PrimitiveType.Cube,set,new Vector3(7.8f,5,36),new Vector3(1.3f,13,72),"Concrete");
                Part("Wallride Stripe",PrimitiveType.Cube,set,new Vector3(7.05f,5,35),new Vector3(.12f,.45f,60),"MintGlow");
                Part("Drain Landing",PrimitiveType.Cube,set,new Vector3(0,-22.8f,102),new Vector3(17,1.4f,42),"ConcreteDark");
                Part("Water Basin",PrimitiveType.Cube,set,new Vector3(0,-22.1f,108),new Vector3(12,.08f,30),"Water");
                Part("Drain Roof",PrimitiveType.Cube,set,new Vector3(0,-15,120),new Vector3(19,1,15),"Concrete");
            }
            else
            {
                Part("Exit Ramp",PrimitiveType.Cube,set,new Vector3(0,4.5f,55),new Vector3(11,.5f,105),"ConcreteDark",new Vector3(-5.4f,0,0));
                for(int side=-1;side<=1;side+=2)Part("Exit Pillar",PrimitiveType.Cube,set,new Vector3(side*7,12,102),new Vector3(1.4f,12,2),"Concrete");
                Part("Exit Header",PrimitiveType.Cube,set,new Vector3(0,18,102),new Vector3(15,1,2),"MintGlow");
            }
            set.gameObject.SetActive(false);
        }
        public static void AddCinematicSetsToExistingScenes()
        {
            MakeMaterials();
            foreach(var name in SceneNames.Skip(1).Take(2))
            {
                var scene=EditorSceneManager.OpenScene(Root+"/Scenes/"+name+".unity");
                AddCinematicSet(UnityEngine.Object.FindFirstObjectByType<StageController>());
                var controller=UnityEngine.Object.FindFirstObjectByType<StageController>();
                if(controller.stageIndex==1) { controller.chaseCamera.height=4.2f;controller.chaseCamera.followDistance=12;EditorUtility.SetDirty(controller.chaseCamera); }
                EditorSceneManager.SaveScene(scene);
            }
            EditorSceneManager.OpenScene(Root+"/Scenes/MainMenu.unity");AssetDatabase.SaveAssets();
        }
        static void AddQuad(List<Vector3> vertices,List<int> indices,RoadPath road,float start,float end,float left,float right,float height)
        {
            int a=vertices.Count;
            vertices.Add(road.Position(start,left,height));vertices.Add(road.Position(start,right,height));vertices.Add(road.Position(end,left,height));vertices.Add(road.Position(end,right,height));
            indices.AddRange(new[]{a,a+2,a+1,a+1,a+2,a+3});
        }
        static void SaveMesh(string name,List<Vector3> vertices,List<int> indices,Transform parent,string mat,int index)
        {
            var mesh=new Mesh { name=name,indexFormat=IndexFormat.UInt32 };mesh.SetVertices(vertices);mesh.SetTriangles(indices,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
            string path=Root+"/Models/"+index+"_"+name+".asset";
            if(File.Exists(path)) { var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);UnityEngine.Object.DestroyImmediate(mesh);mesh=old; } else AssetDatabase.CreateAsset(mesh,path);
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent);go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=materials[mat];go.isStatic=true;
        }
        static GameObject WorldBox(string name,Transform parent,RoadPath road,float s,float x,float y,Vector3 size,string mat,Vector3 rotation=default)
        {
            var go=Part(name,PrimitiveType.Cube,parent,Vector3.zero,size,mat);go.transform.SetPositionAndRotation(road.Position(s,x,y),road.Rotation(s)*Quaternion.Euler(rotation));go.isStatic=true;return go;
        }
        static void CreateEnvironment(RoadPath road,int index)
        {
            var scenery=new GameObject("Environment").transform;bool sewer=road.style==1,outside=road.style==2;
            var vertices=new List<Vector3>();var triangles=new List<int>();
            for(float s=0;s<road.length;s+=8) AddQuad(vertices,triangles,road,s,Mathf.Min(road.length,s+8),-5.5f,5.5f,0);
            SaveMesh("Road",vertices,triangles,scenery,"Asphalt",index);
            vertices.Clear();triangles.Clear();
            for(float s=0;s<road.length;s+=14)for(int lane=-1;lane<=1;lane+=2)AddQuad(vertices,triangles,road,s,s+7,lane*1.85f-.045f,lane*1.85f+.045f,.025f);
            for(float s=0;s<road.length;s+=8)for(int side=-1;side<=1;side+=2)AddQuad(vertices,triangles,road,s,s+8,side*5.1f-.065f,side*5.1f+.065f,.03f);
            SaveMesh("RoadMarkings",vertices,triangles,scenery,"Paint",index);
            if(sewer)
            {
                vertices.Clear();triangles.Clear();
                for(float s=0;s<road.length;s+=8)for(int side=-1;side<=1;side+=2)AddQuad(vertices,triangles,road,s,s+8,side*6.5f-1,side*6.5f+1,-.08f);
                SaveMesh("WaterChannels",vertices,triangles,scenery,"Water",index);
            }
            for(float s=0;s<road.length;s+=16)
            {
                for(int side=-1;side<=1;side+=2)
                {
                    WorldBox("Barrier",scenery,road,s,side*(sewer?8:6),sewer?2:.55f,new Vector3(sewer?1.5f:.4f,sewer?5:1.1f,16.3f),sewer?"ConcreteDark":"Concrete",sewer?new Vector3(0,0,side*18):Vector3.zero);
                    if(((int)s/16)%2==0) WorldBox("Route Light",scenery,road,s,side*(sewer?7.2f:5.78f),sewer?.4f:.9f,new Vector3(.08f,.1f,5),"MintGlow");
                }
                if(!sewer && !outside)WorldBox("Deck",scenery,road,s,0,-.6f,new Vector3(12.5f,1,16.3f),"ConcreteDark");
            }
            for(float s=10;s<road.length;s+=48)
            {
                for(int side=-1;side<=1;side+=2)
                {
                    if(sewer)
                    {
                        WorldBox("Tunnel Column",scenery,road,s,side*7,3,new Vector3(.8f,6,.8f),"Concrete");
                        WorldBox("Light Rail",scenery,road,s,side*6.6f,5,new Vector3(.2f,.2f,9),"MintGlow");
                    }
                    else if(outside)
                    {
                        var rock=Part("Lowpoly Hill",PrimitiveType.Sphere,scenery,road.Position(s,side*(25+random.Next(28)),-2),new Vector3(20+random.Next(25),8+random.Next(20),35),"Rock");rock.isStatic=true;
                        WorldBox("Road Lamp",scenery,road,s,side*7,3,new Vector3(.18f,6,.18f),"Black");
                        WorldBox("Lamp Head",scenery,road,s,side*6.3f,6,new Vector3(1.5f,.16f,.6f),"Warning");
                    }
                    else
                    {
                        float h=18+random.Next(45),x=side*(27+random.Next(25));
                        WorldBox("City Tower",scenery,road,s,x,h*.5f-22,new Vector3(12+random.Next(10),h,18+random.Next(12)),"Building"+(char)('A'+random.Next(3)));
                        for(int floor=0;floor<(int)(h/7);floor++) WorldBox("Window Band",scenery,road,s,x-side*7,floor*7-18,new Vector3(.15f,1.4f,12),"MintGlow");
                        WorldBox("Bridge Support",scenery,road,s,side*4,-10,new Vector3(1.7f,20,2),"ConcreteDark");
                        WorldBox("Lamp",scenery,road,s,side*6.5f,3.8f,new Vector3(.14f,7.6f,.14f),"Black");
                        WorldBox("Lamp Arm",scenery,road,s,side*5.7f,7.5f,new Vector3(1.8f,.15f,.6f),"Warning");
                    }
                }
                if(sewer)WorldBox("Tunnel Beam",scenery,road,s,0,6.2f,new Vector3(15,.7f,2),"ConcreteDark");
            }
            for(float s=180;s<road.length;s+=240)
            {
                WorldBox("Overhead Sign",scenery,road,s,0,7,new Vector3(9,1.8f,.3f),"Black");
                for(int side=-1;side<=1;side+=2)WorldBox("Sign Pillar",scenery,road,s,side*6.5f,3.5f,new Vector3(.3f,7,.3f),"Chrome");
                for(int j=-1;j<=1;j++)WorldBox("Exit Arrow",scenery,road,s-.2f,j*2.7f,7,new Vector3(.45f,.9f,.15f),"MintGlow",new Vector3(0,0,45));
            }
            var ground=Part("Ground",PrimitiveType.Cube,scenery,new Vector3(0,sewer?-8:outside?-5:-8,road.length*.5f),new Vector3(900,2,road.length+600),outside?"Sand":"ConcreteDark");ground.isStatic=true;
            var sun=new GameObject("Sun",typeof(Light));sun.transform.rotation=Quaternion.Euler(sewer?35:42,-35,0);var light=sun.GetComponent<Light>();light.type=LightType.Directional;light.intensity=sewer?1.1f:1.7f;light.color=outside?new Color(1,.84f,.66f):new Color(.85f,.95f,1);light.shadows=LightShadows.Soft;
            RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=sewer?new Color(.28f,.4f,.46f):new Color(.55f,.65f,.72f);
            RenderSettings.fog=true;RenderSettings.fogMode=FogMode.ExponentialSquared;RenderSettings.fogDensity=sewer?.009f:.005f;
            RenderSettings.fogColor=sewer?new Color(.09f,.23f,.27f):outside?new Color(.78f,.6f,.45f):new Color(.45f,.7f,.75f);
            string skyPath=Root+"/Materials/Sky_"+index+".mat";var sky=AssetDatabase.LoadAssetAtPath<Material>(skyPath);
            if(sky==null) { sky=new Material(Shader.Find("Skybox/Procedural"));sky.SetColor("_SkyTint",RenderSettings.fogColor);sky.SetColor("_GroundColor",RenderSettings.fogColor*.6f);sky.SetFloat("_Exposure",1.1f);AssetDatabase.CreateAsset(sky,skyPath); }
            RenderSettings.skybox=sky;RenderSettings.sun=light;
        }
        static void MakeObstacles(StageController stage)
        {
            int row=0;
            for(float s=290;s<1900;s+=62)
            {
                int safe=row%3;int count=row<4?1:2;
                for(int n=0;n<count;n++)
                {
                    int lane=(safe+1+n)%3;float x=(lane-1)*3.45f;
                    var obstacle=new GameObject("Obstacle row "+row+" safe lane "+safe);obstacle.transform.SetParent(stage.obstacles.transform);obstacle.transform.SetPositionAndRotation(stage.road.Position(s,x),stage.road.Rotation(s));
                    if(row%3==0)
                    {
                        Part("Traffic Car",PrimitiveType.Cube,obstacle.transform,new Vector3(0,.65f,0),new Vector3(1.8f,1.1f,3.4f),"Orange");
                        Part("Cabin",PrimitiveType.Cube,obstacle.transform,new Vector3(0,1.35f,-.1f),new Vector3(1.5f,.6f,1.6f),"Glass");
                    }
                    else
                    {
                        Part("Construction Block",PrimitiveType.Cube,obstacle.transform,new Vector3(0,.65f,0),new Vector3(1.8f,1.3f,1.2f),"Concrete");
                        for(int i=-1;i<=1;i++)Part("Hazard Stripe",PrimitiveType.Cube,obstacle.transform,new Vector3(i*.5f,.8f,-.62f),new Vector3(.23f,.7f,.05f),"Warning",new Vector3(0,0,-25));
                    }
                    stage.obstacles.obstacles.Add(new RoadObstacle {visual=obstacle.transform,distance=s,lateral=x,row=row,width=1.8f,speed=row%7==6?5:0});
                }
                row++;
            }
        }
        [MenuItem("Frog Escape/Build Windows Player")]
        public static string BuildWindows()
        {
            if(EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before building.");
            Directory.CreateDirectory("Builds/FrogEscape");
            string oldName=PlayerSettings.productName;int oldWidth=PlayerSettings.defaultScreenWidth,oldHeight=PlayerSettings.defaultScreenHeight;var oldMode=PlayerSettings.fullScreenMode;
            try
            {
                PlayerSettings.productName="Frog Escape";PlayerSettings.defaultScreenWidth=1920;PlayerSettings.defaultScreenHeight=1080;PlayerSettings.fullScreenMode=FullScreenMode.FullScreenWindow;
                var result=BuildPipeline.BuildPlayer(new BuildPlayerOptions {scenes=SceneNames.Select(x=>Root+"/Scenes/"+x+".unity").ToArray(),locationPathName="Builds/FrogEscape/FrogEscape.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
                File.WriteAllText("Documentation/FrogEscape/BuildResult.json",JsonUtility.ToJson(new BuildResult {result=result.summary.result.ToString(),errors=result.summary.totalErrors,warnings=result.summary.totalWarnings,bytes=result.summary.totalSize,seconds=result.summary.totalTime.TotalSeconds},true));
                if(result.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new InvalidOperationException("Build failed: "+result.summary.result);
                return result.summary.outputPath;
            }
            finally { PlayerSettings.productName=oldName;PlayerSettings.defaultScreenWidth=oldWidth;PlayerSettings.defaultScreenHeight=oldHeight;PlayerSettings.fullScreenMode=oldMode; }
        }
        [Serializable] public class BuildResult { public string result;public int errors,warnings;public ulong bytes;public double seconds; }
    }
}
