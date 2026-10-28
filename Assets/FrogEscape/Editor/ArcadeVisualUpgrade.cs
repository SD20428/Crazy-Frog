using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace FrogEscape.Editor
{
    // Additive, repeatable art pass. Does not change routes, colliders or gameplay settings.
    public static class ArcadeVisualUpgrade
    {
        const string Root="Assets/FrogEscape";
        static readonly Dictionary<string,Material> palette=new Dictionary<string,Material>();
        static Material Mat(string name,Color color)
        {
            string path=Root+"/Materials/"+name+".mat";
            var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!m) { m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path); }
            m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",.32f);m.enableInstancing=true;EditorUtility.SetDirty(m);palette[name]=m;return m;
        }
        static GameObject Part(Transform parent,string name,PrimitiveType shape,Vector3 p,Vector3 scale,string mat,Vector3 angle=default)
        {
            var go=GameObject.CreatePrimitive(shape);go.name=name;go.transform.SetParent(parent,false);
            go.transform.localPosition=p;go.transform.localRotation=Quaternion.Euler(angle);go.transform.localScale=scale;
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());go.GetComponent<Renderer>().sharedMaterial=palette[mat];return go;
        }
        static Transform Group(Transform parent,string name,Vector3 pos,Quaternion rotation)
        { var t=new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=pos;t.localRotation=rotation;return t; }
        static Transform Along(Transform root,RoadPath road,string name,float s,float x,float y=0)
        { return Group(root,name,road.Position(s,x,y),road.Rotation(s)); }
        static void Cube(Transform t,string name,Vector3 p,Vector3 size,string mat,Vector3 angles=default)
        { Part(t,name,PrimitiveType.Cube,p,size,mat,angles); }
        static void Text(Transform t,string text,Vector3 p,float size)
        {
            var g=new GameObject("Sign text");g.transform.SetParent(t,false);g.transform.localPosition=p;g.transform.localRotation=Quaternion.identity;
            var label=g.AddComponent<TextMeshPro>();label.font=TMP_Settings.defaultFontAsset;label.text=text;label.fontSize=size;label.fontStyle=FontStyles.Bold;label.alignment=TextAlignmentOptions.Center;label.color=Color.white;
            label.rectTransform.sizeDelta=new Vector2(8,2);label.enableAutoSizing=false;
        }
        [MenuItem("Frog Escape/Apply Arcade Visual Upgrade")]
        public static string Apply()
        {
            if(EditorApplication.isPlaying || SceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("Stop play mode and save the open scene first.");
            Mat("ArcadeBlue",new Color(.08f,.43f,.94f));Mat("ArcadePink",new Color(1,.25f,.48f));
            Mat("ArcadeLeaf",new Color(.27f,.78f,.14f));Mat("ArcadeLeafLight",new Color(.55f,.92f,.22f));Mat("ArcadeWood",new Color(.46f,.25f,.12f));
            Mat("ArcadeCloud",new Color(1,.98f,.93f));Mat("ArcadeMetal",new Color(.27f,.4f,.51f));Mat("ArcadeYellow",new Color(1,.77f,.12f));
            foreach(string name in new[]{"Black","Chrome","Paint","Mint","Orange","Glass","Warning","MintGlow"}) palette[name]=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/"+name+".mat");
            Mat("Asphalt",new Color(.22f,.29f,.38f));Mat("Concrete",new Color(.66f,.75f,.83f));Mat("ConcreteDark",new Color(.27f,.4f,.54f));
            Mat("BuildingA",new Color(.34f,.6f,.86f));Mat("BuildingB",new Color(.48f,.45f,.79f));Mat("BuildingC",new Color(.35f,.76f,.73f));
            Mat("Sand",new Color(.76f,.81f,.43f));Mat("Rock",new Color(.42f,.65f,.32f));Mat("Water",new Color(.03f,.61f,.78f));
            UpgradeBike();UpgradePolice();
            foreach(string name in FrogEscapeBuilder.SceneNames)
            {
                var scene=EditorSceneManager.OpenScene(Root+"/Scenes/"+name+".unity");
                var stage=UnityEngine.Object.FindFirstObjectByType<StageController>();
                stage.chaseCamera.height=stage.stageIndex==1?4.2f:5.5f;
                if(!GameObject.Find("Arcade Detail Pass"))
                {
                    var root=new GameObject("Arcade Detail Pass").transform;
                    Scenery(root,stage);Traffic(stage);Combine(root,name);
                }
                foreach(var label in GameObject.Find("Arcade Detail Pass").GetComponentsInChildren<TextMeshPro>())
                    label.transform.localRotation=Quaternion.identity;
                if(UnityEngine.Object.FindFirstObjectByType<SewerAtmosphere>())
                { EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);continue; }
                var sun=RenderSettings.sun;
                if(sun) { sun.color=new Color(1,.96f,.85f);sun.intensity=stage.stageIndex==1?1.25f:1.65f;sun.shadowStrength=.65f; }
                RenderSettings.ambientLight=new Color(.64f,.74f,.86f);
                RenderSettings.fogColor=stage.stageIndex==1?new Color(.2f,.52f,.64f):new Color(.59f,.81f,.96f);
                RenderSettings.fogDensity=stage.stageIndex==1?.006f:.0037f;
                if(RenderSettings.skybox)
                {
                    RenderSettings.skybox.SetColor("_SkyTint",new Color(.52f,.7f,.9f));
                    RenderSettings.skybox.SetColor("_GroundColor",new Color(.59f,.74f,.82f));
                    RenderSettings.skybox.SetFloat("_AtmosphereThickness",.7f);EditorUtility.SetDirty(RenderSettings.skybox);
                }
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();EditorSceneManager.OpenScene(Root+"/Scenes/MainMenu.unity");
            return "Arcade UI palette, detailed vehicles and 5 enhanced scenes saved.";
        }
        static void UpgradeBike()
        {
            string path=Root+"/Prefabs/FrogBike.prefab";var prefab=PrefabUtility.LoadPrefabContents(path);
            try
            {
                var visual=prefab.transform.Find("Visual");
                if(visual.Find("Arcade Bike Details"))return;
                var t=Group(visual,"Arcade Bike Details",Vector3.zero,Quaternion.identity);
                Cube(t,"Engine",new Vector3(0,.65f,0),new Vector3(.85f,.45f,.8f),"ArcadeMetal");
                for(int i=0;i<4;i++)Cube(t,"Engine fins",new Vector3(0,.5f+i*.1f,0),new Vector3(.96f,.045f,.8f),"Chrome");
                for(int side=-1;side<=1;side+=2)
                {
                    Part(t,"Exhaust",PrimitiveType.Cylinder,new Vector3(side*.56f,.65f,-.85f),new Vector3(.21f,.55f,.21f),"Chrome",new Vector3(90,0,0));
                    Part(t,"Exhaust outlet",PrimitiveType.Cylinder,new Vector3(side*.56f,.65f,-1.41f),new Vector3(.15f,.015f,.15f),"Black",new Vector3(90,0,0));
                    Cube(t,"Racing stripe",new Vector3(side*.56f,.96f,.3f),new Vector3(.025f,.18f,1.7f),"ArcadeYellow",new Vector3(-8,0,0));
                    Cube(t,"Front fork",new Vector3(side*.3f,.97f,1.1f),new Vector3(.1f,1,.12f),"Chrome",new Vector3(-18,0,0));
                    Part(t,"Mirror",PrimitiveType.Sphere,new Vector3(side*.84f,1.7f,.95f),new Vector3(.3f,.18f,.12f),"Glass");
                    for(int z=-1;z<=1;z+=2)Part(t,"Wheel accent",PrimitiveType.Cylinder,new Vector3(side*.251f,.57f,z*1.1f),new Vector3(.35f,.02f,.35f),"ArcadeYellow",new Vector3(0,0,90));
                }
                var rider=visual.Find("Rider");var bag=Group(rider,"Adventure backpack",Vector3.zero,Quaternion.identity);
                Cube(bag,"Pack",new Vector3(0,.38f,-.43f),new Vector3(.66f,.75f,.3f),"ArcadeBlue");
                Cube(bag,"Pocket",new Vector3(0,.22f,-.61f),new Vector3(.48f,.28f,.1f),"ArcadeYellow");
                for(int side=-1;side<=1;side+=2)Cube(bag,"Strap",new Vector3(side*.3f,.4f,.08f),new Vector3(.1f,.8f,.78f),"ArcadeBlue");
                PrefabUtility.SaveAsPrefabAsset(prefab,path);
            }
            finally { PrefabUtility.UnloadPrefabContents(prefab); }
        }
        static void UpgradePolice()
        {
            string path=Root+"/Prefabs/PoliceCar.prefab";var prefab=PrefabUtility.LoadPrefabContents(path);
            try
            {
                if(prefab.transform.Find("Arcade Police Details"))return;
                var t=Group(prefab.transform,"Arcade Police Details",Vector3.zero,Quaternion.identity);
                Cube(t,"Bumper",new Vector3(0,.48f,2.3f),new Vector3(2.45f,.18f,.2f),"Chrome");
                Cube(t,"Grille",new Vector3(0,.87f,2.27f),new Vector3(.75f,.24f,.06f),"Black");
                for(int i=-2;i<=2;i++)Cube(t,"Grille slat",new Vector3(i*.13f,.87f,2.31f),new Vector3(.05f,.18f,.04f),"Chrome");
                for(int side=-1;side<=1;side+=2)
                {
                    Cube(t,"Door badge",new Vector3(side*1.18f,1,0),new Vector3(.035f,.5f,.5f),"ArcadeYellow",new Vector3(45,0,0));
                    Cube(t,"Mirror",new Vector3(side*1.15f,1.45f,.58f),new Vector3(.35f,.2f,.3f),"Black");
                    for(int z=-1;z<=1;z+=2)Part(t,"Hub",PrimitiveType.Cylinder,new Vector3(side*1.29f,.5f,z*1.4f),new Vector3(.48f,.025f,.48f),"Chrome",new Vector3(0,0,90));
                }
                Cube(t,"Antenna",new Vector3(.7f,2.12f,-1),new Vector3(.035f,.8f,.035f),"Black");
                PrefabUtility.SaveAsPrefabAsset(prefab,path);
            }
            finally { PrefabUtility.UnloadPrefabContents(prefab); }
        }
        static void Scenery(Transform root,StageController stage)
        {
            var road=stage.road;bool sewer=stage.stageIndex==1;
            for(float s=12;s<road.length;s+=32)
            {
                for(int side=-1;side<=1;side+=2)
                {
                    var t=Along(root,road,"Road details",s,side*5.55f);
                    Cube(t,"Curb cap",new Vector3(0,.14f,0),new Vector3(.35f,.28f,12),((int)s/32)%2==0?"Paint":"ArcadeBlue");
                    Cube(t,"Reflector",new Vector3(0,1,0),new Vector3(.28f,.18f,.3f),"ArcadeYellow");
                    if(sewer)
                    {
                        var pipe=Along(root,road,"Pipe and grate",s,side*7.15f);
                        Part(pipe,"Service pipe",PrimitiveType.Cylinder,new Vector3(0,2.6f,0),new Vector3(.55f,16,.55f),"ArcadeBlue",new Vector3(90,0,0));
                        for(int j=-1;j<=1;j+=2)Part(pipe,"Pipe coupling",PrimitiveType.Cylinder,new Vector3(0,2.6f,j*12),new Vector3(.72f,.18f,.72f),"Chrome",new Vector3(90,0,0));
                        for(int j=0;j<5;j++)Cube(pipe,"Drain grate",new Vector3(-side*.48f,.04f,j*.23f),new Vector3(1.5f,.06f,.1f),"Black");
                        Cube(pipe,"Service panel",new Vector3(-side*.6f,1.35f,5),new Vector3(.24f,1.1f,.9f),"ArcadeYellow");
                    }
                    else if(((int)s/32)%2==0)
                    {
                        var tree=Along(root,road,"Blocky tree",s,side*(stage.stageIndex>=2?13:17),stage.stageIndex>=2?-2:-17);
                        Cube(tree,"Trunk",new Vector3(0,2.3f,0),new Vector3(.9f,5,.9f),"ArcadeWood");
                        Part(tree,"Crown",PrimitiveType.Sphere,new Vector3(0,5.3f,0),new Vector3(5.5f,5,5),"ArcadeLeaf");
                        Part(tree,"Crown highlight",PrimitiveType.Sphere,new Vector3(1.4f,6.5f,0),new Vector3(3.7f,3.6f,3.7f),"ArcadeLeafLight");
                        var shrub=Along(root,road,"Roadside shrubs",s+13,side*10,stage.stageIndex>=2?-1:-18);
                        for(int j=0;j<3;j++)Part(shrub,"Shrub",PrimitiveType.Sphere,new Vector3(j*1.1f,0,0),new Vector3(2,2,2),"ArcadeLeafLight");
                    }
                }
            }
            if(!sewer)
            {
                for(float s=40;s<road.length;s+=150)
                {
                    var cloud=Along(root,road,"Cloud cluster",s,Mathf.Sin(s)*85,38);
                    for(int j=-1;j<=1;j++)Part(cloud,"Cloud puff",PrimitiveType.Sphere,new Vector3(j*9,Mathf.Abs(j)*-2,0),new Vector3(21,9,13),"ArcadeCloud");
                }
                foreach(var tower in GameObject.Find("Environment").GetComponentsInChildren<Transform>().Where(t=>t.name=="City Tower").ToArray())
                {
                    var t=Group(root,"Tower details",tower.position,tower.rotation);var size=tower.localScale;
                    Cube(t,"Roof lip",new Vector3(0,size.y*.5f,0),new Vector3(size.x+1,.5f,size.z+1),"Paint");
                    Cube(t,"Roof AC",new Vector3(0,size.y*.5f+1.1f,0),new Vector3(3,2,4),"ArcadeMetal");
                    for(float y=-size.y*.5f+4;y<size.y*.5f-2;y+=5)
                        for(float x=-size.x*.5f+2;x<size.x*.5f-1;x+=3)
                            Cube(t,"Front window",new Vector3(x,y,-size.z*.5f-.025f),new Vector3(1.5f,2,.06f),((int)y%3==0)?"ArcadeYellow":"Glass");
                }
            }
            for(float s=115;s<road.length;s+=210)
            {
                var sign=Along(root,road,"Route sign",s,0,7.6f);
                Cube(sign,"Sign border",Vector3.zero,new Vector3(8.7f,1.8f,.3f),"Paint");
                Cube(sign,"Sign face",new Vector3(0,0,-.18f),new Vector3(8.4f,1.5f,.08f),sewer?"ArcadeBlue":"ArcadeLeaf");
                Text(sign,sewer?"SEWER  //  KEEP MOVING":stage.stageIndex>=2?"ESCAPE ROUTE   >>>":"FROG CITY   >>>",new Vector3(0,0,-.24f),5);
                for(int side=-1;side<=1;side+=2)Cube(sign,"Sign post",new Vector3(side*6.3f,-3.8f,0),new Vector3(.2f,7.6f,.2f),"Chrome");
            }
        }
        static void Traffic(StageController stage)
        {
            foreach(var item in stage.obstacles.obstacles)
            {
                if(!item.visual || item.visual.Find("Traffic details"))continue;
                var t=Group(item.visual,"Traffic details",Vector3.zero,Quaternion.identity);
                if(item.visual.Find("Traffic Car"))
                {
                    for(int side=-1;side<=1;side+=2)
                    {
                        for(int z=-1;z<=1;z+=2)Part(t,"Tire",PrimitiveType.Cylinder,new Vector3(side*.86f,.4f,z*1.1f),new Vector3(.7f,.12f,.7f),"Black",new Vector3(0,0,90));
                        Cube(t,"Taillight",new Vector3(side*.57f,.8f,-1.73f),new Vector3(.35f,.2f,.06f),"ArcadePink");
                    }
                    Cube(t,"Bumper",new Vector3(0,.4f,-1.75f),new Vector3(1.8f,.17f,.12f),"Chrome");
                }
                else
                {
                    for(int side=-1;side<=1;side+=2)
                    {
                        Cube(t,"Barrier foot",new Vector3(side*.65f,.1f,0),new Vector3(.35f,.2f,1.65f),"Black");
                        Part(t,"Warning beacon",PrimitiveType.Sphere,new Vector3(side*.66f,1.47f,0),new Vector3(.22f,.26f,.22f),"Warning");
                    }
                }
            }
        }
        static void Combine(Transform root,string scene)
        {
            // Combine in 120-unit spatial chunks: preserve frustum culling instead of one world-sized mesh.
            var renderers=root.GetComponentsInChildren<MeshRenderer>().Where(r=>r.GetComponent<MeshFilter>() && !r.GetComponent<TMP_Text>()).ToArray();
            var groups=renderers.GroupBy(r=>new {Material=r.sharedMaterial,Chunk=Mathf.FloorToInt(r.transform.position.z/120)});
            foreach(var group in groups)
            {
                var mesh=new Mesh {name="Arcade scenery",indexFormat=IndexFormat.UInt32};
                mesh.CombineMeshes(group.Select(r=>new CombineInstance {mesh=r.GetComponent<MeshFilter>().sharedMesh,transform=root.worldToLocalMatrix*r.transform.localToWorldMatrix}).ToArray());
                string path=Root+"/Models/Arcade_"+scene+"_"+group.Key.Material.name+"_"+group.Key.Chunk+".asset";
                var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if(existing) { EditorUtility.CopySerialized(mesh,existing);UnityEngine.Object.DestroyImmediate(mesh);mesh=existing; } else AssetDatabase.CreateAsset(mesh,path);
                var go=new GameObject("Batched "+group.Key.Material.name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(root,false);go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=group.Key.Material;go.isStatic=true;
                foreach(var r in group) { UnityEngine.Object.DestroyImmediate(r.GetComponent<MeshFilter>());UnityEngine.Object.DestroyImmediate(r); }
            }
        }
    }
}
