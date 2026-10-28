using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace FrogEscape.Editor
{
    public static class SewerInteriorBuilder
    {
        const string Root="Assets/FrogEscape";
        static Material Material(string name,Color color,float smooth=.25f)
        {
            string path=Root+"/Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
            m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",smooth);m.enableInstancing=true;EditorUtility.SetDirty(m);return m;
        }
        static GameObject Part(Transform parent,string name,PrimitiveType type,Vector3 position,Vector3 size,Material mat,Quaternion rotation)
        {
            var go=GameObject.CreatePrimitive(type);go.name=name;go.transform.SetParent(parent,false);go.transform.position=position;go.transform.rotation=rotation;go.transform.localScale=size;
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());go.GetComponent<Renderer>().sharedMaterial=mat;go.isStatic=true;return go;
        }
        static void ConfigureExit(SewerAtmosphere atmosphere)
        {
            atmosphere.exitSky=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/Sky_2.mat");
            atmosphere.ceilingFixtures=atmosphere.GetComponentsInChildren<Transform>().Where(t=>t.name=="Ceiling lamp housing" || t.name=="Warm ceiling light").ToArray();
            EditorUtility.SetDirty(atmosphere);
        }
        public static string Apply()
        {
            if(EditorApplication.isPlaying || SceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Stop play and save the current scene first.");
            var scene=EditorSceneManager.OpenScene(Root+"/Scenes/Stage02_Sewer.unity");
            if(GameObject.Find("Enclosed Sewer Interior"))
            { ConfigureExit(UnityEngine.Object.FindFirstObjectByType<SewerAtmosphere>());EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);return "Sewer exit lighting updated"; }
            var stage=UnityEngine.Object.FindFirstObjectByType<StageController>();var road=stage.road;
            var root=new GameObject("Enclosed Sewer Interior");var atmosphere=root.AddComponent<SewerAtmosphere>();atmosphere.stage=stage;
            var brick=Material("SewerBrick",Color.white,.45f);
            string texturePath=Root+"/Materials/SewerBrickPattern.asset";
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if(!texture)
            {
                texture=new Texture2D(256,256,TextureFormat.RGBA32,true);texture.name="Damp staggered masonry";texture.wrapMode=TextureWrapMode.Repeat;texture.anisoLevel=4;
                for(int y=0;y<256;y++)for(int x=0;x<256;x++)
                {
                    int row=y/32;int bx=(x+(row%2)*32)%64;
                    bool mortar=y%32<3 || bx<3;
                    float grime=Mathf.PerlinNoise(x*.035f,y*.045f);
                    Color c=mortar?new Color(.14f,.16f,.13f):Color.Lerp(new Color(.28f,.31f,.21f),new Color(.51f,.43f,.29f),grime);
                    texture.SetPixel(x,y,c);
                }
                texture.Apply();AssetDatabase.CreateAsset(texture,texturePath);
            }
            brick.SetTexture("_BaseMap",texture);
            var iron=Material("SewerIron",new Color(.18f,.24f,.21f),.55f);
            var rust=Material("SewerRust",new Color(.43f,.24f,.12f),.35f);
            var water=Material("SewerWater",new Color(.15f,.34f,.13f),.9f);
            var roadMat=Material("SewerWalkway",new Color(.25f,.28f,.23f),.6f);
            var glow=Material("SewerLamp",new Color(1,.8f,.35f));glow.EnableKeyword("_EMISSION");glow.SetColor("_EmissionColor",new Color(1,.68f,.2f)*2);
            var roofs=new List<Renderer>();var distances=new List<float>();var lights=new List<Light>();
            for(float start=0;start<road.length;start+=20)
            {
                var vertices=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();
                for(int row=0;row<=5;row++)
                {
                    float s=start+row*4;
                    for(int j=0;j<=32;j++)
                    {
                        float a=j*Mathf.PI/32;
                        vertices.Add(road.Position(s,Mathf.Cos(a)*9,Mathf.Sin(a)*9-.2f));uv.Add(new Vector2(a*3,s/4));
                        if(row<5 && j<32){int i=row*33+j;triangles.AddRange(new[]{i,i+33,i+1,i+1,i+33,i+34});}
                    }
                }
                var mesh=new Mesh {name="Sewer arch "+start};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
                string meshPath=Root+"/Models/SewerArch_"+(int)start+".asset";
                var old=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);if(old){EditorUtility.CopySerialized(mesh,old);UnityEngine.Object.DestroyImmediate(mesh);mesh=old;}else AssetDatabase.CreateAsset(mesh,meshPath);
                var shell=new GameObject("Brick vault "+start,typeof(MeshFilter),typeof(MeshRenderer));shell.transform.SetParent(root.transform,false);shell.GetComponent<MeshFilter>().sharedMesh=mesh;
                var renderer=shell.GetComponent<MeshRenderer>();renderer.sharedMaterial=brick;renderer.shadowCastingMode=ShadowCastingMode.TwoSided;shell.isStatic=true;roofs.Add(renderer);distances.Add(start);
            }
            foreach(var r in GameObject.Find("Environment").GetComponentsInChildren<Renderer>())
            {
                if(r.name=="Road")r.sharedMaterial=roadMat;
                if(r.name=="WaterChannels")r.sharedMaterial=water;
                if(r.name=="Barrier")r.sharedMaterial=brick;
                if(r.name=="Tunnel Beam" || r.name=="Tunnel Column" || r.name=="Light Rail" || r.name=="Overhead Sign" || r.name=="Exit Arrow" || r.name=="Sign Pillar")r.gameObject.SetActive(false);
            }
            // Existing additions have combined meshes; replace only this scene's renderer material assignments.
            var arcade=GameObject.Find("Arcade Detail Pass");
            foreach(var r in arcade.GetComponentsInChildren<MeshRenderer>())
            {
                if(r.sharedMaterial && r.sharedMaterial.name=="ArcadeBlue")r.sharedMaterial=rust;
                else if(r.sharedMaterial && r.sharedMaterial.name=="Chrome")r.sharedMaterial=iron;
            }
            for(float s=16;s<road.length;s+=32)
            {
                var rot=road.Rotation(s);
                Part(root.transform,"Ceiling lamp housing",PrimitiveType.Cube,road.Position(s,0,8.35f),new Vector3(1.3f,.22f,1.4f),iron,rot);
                Part(root.transform,"Warm ceiling light",PrimitiveType.Sphere,road.Position(s,0,8.12f),new Vector3(.85f,.22f,.85f),glow,rot);
                var light=new GameObject("Local sewer light",typeof(Light));light.transform.SetParent(root.transform,false);light.transform.position=road.Position(s,0,6.8f);
                var l=light.GetComponent<Light>();l.type=LightType.Point;l.color=new Color(1,.8f,.43f);l.intensity=5;l.range=16;l.shadows=LightShadows.None;lights.Add(l);
                for(int side=-1;side<=1;side+=2)
                {
                    Part(root.transform,"Main drain pipe",PrimitiveType.Cylinder,road.Position(s,side*7,3.3f),new Vector3(.9f,16,.9f),rust,rot*Quaternion.Euler(90,0,0));
                    Part(root.transform,"Pipe flange",PrimitiveType.Cylinder,road.Position(s,side*7,3.3f),new Vector3(1.16f,.16f,1.16f),iron,rot*Quaternion.Euler(90,0,0));
                    var valve=Part(root.transform,"Valve hub",PrimitiveType.Cylinder,road.Position(s,side*6.45f,3.3f),new Vector3(.64f,.08f,.64f),rust,rot*Quaternion.Euler(0,0,90));
                    for(int j=0;j<3;j++)Part(root.transform,"Drain runoff",PrimitiveType.Cube,road.Position(s+j*3,side*6.5f,-.015f),new Vector3(.1f,.012f,1.8f),water,rot);
                }
            }
            atmosphere.roofSections=roofs.ToArray();atmosphere.sectionDistances=distances.ToArray();atmosphere.lamps=lights.ToArray();
            ConfigureExit(atmosphere);
            stage.chaseCamera.view.clearFlags=CameraClearFlags.SolidColor;stage.chaseCamera.view.backgroundColor=new Color(.07f,.11f,.075f);
            RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.34f,.39f,.3f);
            RenderSettings.sun.intensity=.3f;RenderSettings.sun.color=new Color(.75f,.85f,.65f);
            RenderSettings.fogColor=new Color(.09f,.15f,.1f);RenderSettings.fogDensity=.012f;RenderSettings.skybox=null;
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            return "Enclosed brick sewer saved: "+roofs.Count+" roof sections, "+lights.Count+" locally enabled lamps";
        }
    }
}
