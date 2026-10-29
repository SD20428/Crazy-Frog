using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace FrogEscape
{
    // Runtime art direction only; source materials and gameplay geometry stay untouched.
    public sealed class NeonArtDirection : MonoBehaviour
    {
        public StageController stage;
        readonly List<Material> owned = new List<Material>();
        readonly List<Transform> rails = new List<Transform>();
        readonly List<Transform> accents = new List<Transform>();
        Material neon;
        Color accent;
        void Start()
        {
            // The new flight corridor needs clearance above the old ground-only signs.
            // Adjust runtime instances, never the serialized scene or source art.
            if(stage.stageIndex==0 || stage.stageIndex==2)
                foreach(var item in FindObjectsByType<Transform>(FindObjectsSortMode.None))
                {
                    if(item.name=="Overhead Sign" || item.name=="Exit Arrow" ||
                        item.name=="Sign face" || item.name=="Sign border" || item.name=="Sign text")
                        item.position+=Vector3.up*5;
                    else if(item.name=="Sign Pillar" || item.name=="Sign post")
                    {
                        item.position+=Vector3.up*2.5f;
                        item.localScale+=Vector3.up*5;
                    }
                }
            accent = stage.stageIndex == 1 ? new Color(.08f,1,.72f) : stage.stageIndex == 2 ? new Color(1,.38f,.12f) : new Color(.12f,.85f,1);
            var palette = new Dictionary<string,Color> {
                {"Asphalt",new Color(.055f,.07f,.12f)}, {"Concrete",new Color(.25f,.31f,.43f)},
                {"ConcreteDark",new Color(.11f,.14f,.24f)}, {"BuildingA",new Color(.16f,.26f,.4f)},
                {"BuildingB",new Color(.28f,.18f,.38f)}, {"BuildingC",new Color(.13f,.34f,.36f)}
            };
            if(stage.stageIndex==1)
            {
                palette["SewerBrick"]=new Color(.4f,.59f,.61f);
                palette["SewerIron"]=new Color(.09f,.17f,.21f);
                palette["SewerRust"]=new Color(.38f,.22f,.2f);
                palette["SewerWater"]=new Color(.025f,.3f,.27f);
                palette["SewerWalkway"]=new Color(.11f,.19f,.23f);
                palette["SewerLamp"]=new Color(.36f,1,.78f);
                var atmosphere=FindFirstObjectByType<SewerAtmosphere>();
                if(atmosphere)foreach(var lamp in atmosphere.lamps)
                    if(lamp){lamp.color=new Color(.35f,1,.78f);lamp.intensity=6;}
                stage.chaseCamera.view.backgroundColor=new Color(.025f,.1f,.12f);
            }
            if(stage.stageIndex==2)
            {
                palette["Sand"]=new Color(.48f,.28f,.36f);
                palette["Rock"]=new Color(.28f,.2f,.37f);
                palette["ArcadeLeaf"]=new Color(.21f,.3f,.35f);
                palette["ArcadeLeafLight"]=new Color(.42f,.4f,.5f);
                if(RenderSettings.skybox)
                {
                    var sky=new Material(RenderSettings.skybox);owned.Add(sky);
                    if(sky.HasProperty("_SkyTint"))sky.SetColor("_SkyTint",new Color(.52f,.28f,.5f));
                    if(sky.HasProperty("_GroundColor"))sky.SetColor("_GroundColor",new Color(.25f,.13f,.24f));
                    RenderSettings.skybox=sky;
                }
                if(RenderSettings.sun){RenderSettings.sun.color=new Color(1,.73f,.53f);RenderSettings.sun.intensity=1.35f;}
            }
            var clones = new Dictionary<Material,Material>();
            foreach(var renderer in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            {
                var materials=renderer.sharedMaterials; bool changed=false;
                for(int i=0;i<materials.Length;i++)
                {
                    var source=materials[i]; if(!source || !palette.TryGetValue(source.name,out var color)) continue;
                    if(!clones.TryGetValue(source,out var copy))
                    { copy=new Material(source); copy.color=color; if(copy.HasProperty("_Smoothness"))copy.SetFloat("_Smoothness",.48f);
                      if(source.name=="SewerLamp")copy.SetColor("_EmissionColor",accent*2);
                      clones[source]=copy;owned.Add(copy); }
                    materials[i]=copy; changed=true;
                }
                if(changed)renderer.sharedMaterials=materials;
            }
            RenderSettings.fog=true;
            RenderSettings.fogMode=FogMode.ExponentialSquared;
            RenderSettings.fogDensity=stage.stageIndex==1?.009f:.0038f;
            RenderSettings.fogColor=stage.stageIndex==1?new Color(.025f,.12f,.14f):stage.stageIndex==2?new Color(.35f,.22f,.34f):new Color(.22f,.22f,.39f);
            RenderSettings.ambientLight=new Color(.42f,.48f,.64f);
            neon=new Material(stage.config.successMaterial);owned.Add(neon);
            neon.color=accent;neon.EnableKeyword("_EMISSION");neon.SetColor("_EmissionColor",accent*1.8f);
            for(int i=0;i<36;i++)
            {
                var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name="Neon roadside inlay";
                go.transform.SetParent(transform,false);var c=go.GetComponent<Collider>();c.enabled=false;Destroy(c);
                go.GetComponent<Renderer>().sharedMaterial=neon;rails.Add(go.transform);
            }
            if(stage.stageIndex==1 || stage.stageIndex==2)
                for(int i=0;i<24;i++)
                {
                    var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name="Stage light fin";
                    go.transform.SetParent(transform,false);var c=go.GetComponent<Collider>();c.enabled=false;Destroy(c);
                    go.GetComponent<Renderer>().sharedMaterial=neon;accents.Add(go.transform);
                }
        }
        void LateUpdate()
        {
            if(!stage.road || !stage.player)return;
            float origin=Mathf.Floor(stage.player.distance/12)*12;
            for(int i=0;i<rails.Count;i++)
            {
                float s=origin+(i/2-2)*12;float side=i%2==0?-1:1;
                rails[i].SetPositionAndRotation(stage.road.Position(s,side*(stage.road.halfWidth-.15f),.09f),stage.road.Rotation(s));
                rails[i].localScale=new Vector3(.09f,.035f,6);
            }
            for(int i=0;i<accents.Count;i++)
            {
                bool sewer=stage.stageIndex==1;
                accents[i].gameObject.SetActive(!sewer || stage.Session.mode!=GameMode.Cinematic);
                float s=Mathf.Floor(stage.player.distance/20)*20+(i/2-1)*20;
                float side=i%2==0?-1:1;
                accents[i].SetPositionAndRotation(stage.road.Position(s,side*(sewer?7.7f:8),sewer?3.9f:2.1f),
                    stage.road.Rotation(s)*Quaternion.Euler(0,0,side*(sewer?22:15)));
                accents[i].localScale=new Vector3(.12f,sewer?2.3f:3.8f,.14f);
            }
        }
        void OnDestroy(){foreach(var material in owned)if(material)Destroy(material);}
    }
}
