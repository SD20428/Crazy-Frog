using System.Collections.Generic;
using UnityEngine;

namespace FrogEscape
{
    // Presentation only: never writes movement, health, attack timing or Timeline transforms.
    public class RiderPresentation : MonoBehaviour
    {
        public BikeController bike;
        public Material trailMaterial;
        [Range(0,1)] public float impactStrength=.16f;
        public float wheelRadius=.55f;
        readonly List<Transform> wheels=new List<Transform>();
        readonly List<Renderer> surfaces=new List<Renderer>();
        readonly List<Color> colors=new List<Color>();
        TrailRenderer[] trails;
        Transform scarf;
        Quaternion scarfRest;
        MaterialPropertyBlock block;
        int lastDamage;
        bool tinted;
        void Start()
        {
            if(!bike)bike=GetComponent<BikeController>();
            foreach(var t in bike.visual.GetComponentsInChildren<Transform>())
            {
                if(t.name=="Tire" || t.name=="WheelHub" || t.name=="Wheel accent")wheels.Add(t);
                if(t.name=="ScarfTail") { scarf=t;scarfRest=t.localRotation; }
            }
            foreach(var r in bike.visual.GetComponentsInChildren<Renderer>())
                if(r.sharedMaterial && r.sharedMaterial.HasProperty("_BaseColor"))
                { surfaces.Add(r);colors.Add(r.sharedMaterial.GetColor("_BaseColor")); }
            block=new MaterialPropertyBlock();trails=new TrailRenderer[2];
            for(int i=0;i<2;i++)
            {
                var go=new GameObject("Dodge ribbon "+i);go.transform.SetParent(bike.visual,false);go.transform.localPosition=new Vector3(i==0?-.58f:.58f,.7f,-1.2f);
                var trail=go.AddComponent<TrailRenderer>();trail.sharedMaterial=trailMaterial;trail.time=.28f;trail.minVertexDistance=.1f;
                trail.startWidth=.2f;trail.endWidth=0;trail.startColor=new Color(.35f,1,.9f,.85f);trail.endColor=new Color(.35f,1,.9f,0);
                trail.numCapVertices=3;trail.emitting=false;trail.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;trail.receiveShadows=false;trails[i]=trail;
            }
        }
        void LateUpdate()
        {
            var stage=bike.stage;
            if(stage.Session.mode==GameMode.Paused)return;
            bool playing=stage.Playing;
            foreach(var trail in trails)trail.emitting=playing && bike.dodge.Active;
            if(!playing)
            {
                foreach(var trail in trails)trail.Clear();
                ResetTint();return;
            }
            float spin=bike.Speed/Mathf.Max(.1f,wheelRadius)*Mathf.Rad2Deg*Time.deltaTime;
            foreach(var wheel in wheels)wheel.Rotate(Vector3.up,spin,Space.Self);
            if(scarf)scarf.localRotation=scarfRest*Quaternion.Euler(Mathf.Sin(Time.time*15)*7,Mathf.Sin(Time.time*11)*6,0);
            bike.rider.localRotation=Quaternion.Euler(Mathf.Sin(Time.time*10)*1.8f,0,-bike.lateralVelocity*.45f);
            if(lastDamage!=stage.playerHealth.damageEvents)
            {
                lastDamage=stage.playerHealth.damageEvents;stage.chaseCamera.Impact(impactStrength);
            }
            if(Time.time<stage.playerHealth.invincibleUntil)
            {
                float flash=(.5f+.5f*Mathf.Sin(Time.time*18))*.65f;
                for(int i=0;i<surfaces.Count;i++)
                { surfaces[i].GetPropertyBlock(block);block.SetColor("_BaseColor",Color.Lerp(colors[i],Color.white,flash));surfaces[i].SetPropertyBlock(block); }
                tinted=true;
            }
            else ResetTint();
        }
        void ResetTint()
        {
            if(!tinted)return;
            for(int i=0;i<surfaces.Count;i++)
            { surfaces[i].GetPropertyBlock(block);block.SetColor("_BaseColor",colors[i]);surfaces[i].SetPropertyBlock(block); }
            tinted=false;
        }
    }
}
