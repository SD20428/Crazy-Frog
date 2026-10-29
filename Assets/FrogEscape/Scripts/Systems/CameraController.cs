using UnityEngine;

namespace FrogEscape
{
    public class CameraController : MonoBehaviour
    {
        public StageController stage;
        public Camera view;
        public float followDistance=15, height=7, smoothing=8;
        public bool cinematic;
        float impact,impactUntil;
        public void Impact(float strength) { impact=Mathf.Clamp(strength,0,.25f);impactUntil=Time.time+.25f; }
        public void Snap() { if(!cinematic) Follow(1); }
        void LateUpdate()
        {
            if(cinematic || stage.Session.mode==GameMode.Paused) return;
            Follow(1-Mathf.Exp(-smoothing*Time.deltaTime));
        }
        void Follow(float blend)
        {
            float s=stage.player.distance;
            Vector3 desired,look;
            if(stage.stageIndex==-1)
            {
                desired=stage.road.Position(s+6,11,5.5f);
                look=stage.road.Position(s,0,1.2f);
            }
            else
            {
                desired=stage.road.Position(s-followDistance,stage.player.lateral*.5f,height+stage.player.altitude*.65f);
                look=stage.road.Position(s+11,stage.player.lateral*.3f,2.4f+stage.player.altitude*.55f);
            }
            transform.position=Vector3.Lerp(transform.position,desired,blend);
            if(stage.Playing && Time.time<impactUntil)
                transform.position+=transform.right*(Mathf.Sin(Time.time*55)*impact*Mathf.Clamp01((impactUntil-Time.time)/.25f));
            transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(look-transform.position),blend);
            float target=stage.stageIndex==-1?43:59+stage.player.Speed*.18f;
            if(stage.Playing)target+=Mathf.Clamp01((22-stage.police.gap)/12)*2;
            if(stage.Playing && stage.player.dodge.Active)target+=3*Mathf.Sin(stage.player.dodge.Phase*Mathf.PI);
            view.fieldOfView=Mathf.Lerp(view.fieldOfView,target,blend);
        }
        public void Pose(Vector3 position,Vector3 look,float fov=58)
        { transform.SetPositionAndRotation(position,Quaternion.LookRotation(look-position)); view.fieldOfView=fov; }
    }
}
