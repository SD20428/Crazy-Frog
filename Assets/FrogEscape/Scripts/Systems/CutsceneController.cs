using UnityEngine;
using UnityEngine.Playables;

namespace FrogEscape
{
    public class CutsceneController : MonoBehaviour
    {
        public StageController stage;
        public PlayableDirector director;
        public Transform endingMissile;
        public ParticleSystem comicBurst;
        public Transform transitionSet;
        public int sequence;
        public float progress;
        float startDistance;
        Vector3 riderHome;
        bool active,burstPlayed;
        void OnEnable() { director.stopped+=Stopped; }
        void OnDisable() { director.stopped-=Stopped; }
        public void Begin(int index)
        {
            if(active) return;
            sequence=index; active=true; burstPlayed=false; progress=0;
            stage.Session.mode=GameMode.Cinematic;
            startDistance=stage.player.distance;
            if(transitionSet) { transitionSet.gameObject.SetActive(index<2);transitionSet.SetPositionAndRotation(stage.road.Position(startDistance),stage.road.Rotation(startDistance)); }
            riderHome=stage.player.rider.localPosition;
            stage.chaseCamera.cinematic=true;
            director.playableAsset=index<2?stage.config.transitions[index]:stage.config.ending;
            director.timeUpdateMode=DirectorUpdateMode.GameTime;
            director.extrapolationMode=DirectorWrapMode.None;
            foreach(var binding in director.playableAsset.outputs) director.SetGenericBinding(binding.sourceObject,this);
            director.Play();
            stage.audioSystem.Play(index==2?stage.config.victory:stage.config.dodge);
        }
        public void Evaluate(float t)
        {
            if(!active) return;
            progress=Mathf.Clamp01(t);
            var road=stage.road;
            float s=startDistance+t*(sequence==2?100:110);
            Vector3 center=road.Position(s,0,.2f);
            Quaternion rotation=road.Rotation(s);
            if(sequence==0)
            {
                float rise=Mathf.Sin(Mathf.Clamp01(t/.8f)*Mathf.PI)*9-22*Mathf.SmoothStep(0,1,Mathf.InverseLerp(.62f,.92f,t));
                float side=Mathf.Sin(t*Mathf.PI)*6;
                stage.player.transform.SetPositionAndRotation(center+road.Right(s)*side+Vector3.up*rise,rotation*Quaternion.Euler(-15*Mathf.Sin(t*Mathf.PI),0,-65*Mathf.Sin(t*Mathf.PI)));
                stage.chaseCamera.Pose(road.Position(s-7,-16,6+rise),stage.player.transform.position+Vector3.up,54);
                stage.police.transform.SetPositionAndRotation(road.Position(s-13,0,.1f),rotation);
                if(t>.88f && !burstPlayed) { burstPlayed=true; stage.audioSystem.Play(stage.config.landing); }
            }
            else if(sequence==1)
            {
                float rise=10*Mathf.SmoothStep(0,1,t);
                stage.player.transform.SetPositionAndRotation(center+Vector3.up*rise,rotation*Quaternion.Euler(-14*Mathf.Sin(t*Mathf.PI),0,0));
                stage.chaseCamera.Pose(road.Position(s-12,-12,6+rise),stage.player.transform.position+road.Forward(s)*7,60);
                stage.police.transform.SetPositionAndRotation(road.Position(s-18,1,.2f),rotation);
                if(endingMissile)
                { endingMissile.gameObject.SetActive(t>.48f); endingMissile.SetPositionAndRotation(road.Position(s-22,2,4+rise*.8f),rotation); }
                if(t>.55f && !burstPlayed) { burstPlayed=true; stage.audioSystem.Play(stage.config.warning); }
            }
            else
            {
                // Slow visual travel during the board-and-balance beat, with Timeline itself advancing normally.
                float travel=t<.25f?t*.5f:.125f+(t-.25f)*1.15f;
                s=startDistance+travel*100;
                center=road.Position(s,0,.2f); rotation=road.Rotation(s);
                stage.player.transform.SetPositionAndRotation(center,rotation);
                stage.police.transform.SetPositionAndRotation(road.Position(s+35,-2,.1f),rotation);
                endingMissile.gameObject.SetActive(t<.78f);
                Vector3 missile=road.Position(s+2,3,2.1f);
                if(t>.59f) missile=Vector3.Lerp(missile,stage.police.transform.position,Mathf.Clamp01((t-.59f)/.18f));
                endingMissile.SetPositionAndRotation(missile,rotation*Quaternion.Euler(0,t>.59f?-10:0,0));
                Vector3 riderTarget=stage.player.transform.TransformPoint(riderHome);
                if(t>.12f && t<.6f)
                {
                    float board=Mathf.SmoothStep(0,1,(t-.12f)/.15f);
                    float off=Mathf.SmoothStep(0,1,(t-.48f)/.12f);
                    riderTarget=Vector3.Lerp(riderTarget,missile+Vector3.up*.8f,board*(1-off));
                    riderTarget+=Vector3.up*(Mathf.Sin(Mathf.Clamp01((t-.12f)/.15f)*Mathf.PI)*2+Mathf.Sin(off*Mathf.PI)*1.8f);
                }
                stage.player.rider.position=riderTarget;
                stage.player.rider.localRotation=Quaternion.Euler(0,0,t>.27f && t<.48f?Mathf.Sin(t*100)*12:0);
                if(t<.58f) stage.chaseCamera.Pose(road.Position(s-4,12,5),Vector3.Lerp(center,missile,.5f)+Vector3.up,49);
                else stage.chaseCamera.Pose(road.Position(s-10,-9,5),center+road.Forward(s)*4+Vector3.up,52);
                if(t>.77f && !burstPlayed)
                {
                    burstPlayed=true;
                    comicBurst.transform.position=stage.police.transform.position+Vector3.up*2;
                    comicBurst.Play(); stage.audioSystem.Play(stage.config.impact);
                }
                if(t>.8f) stage.police.transform.localScale=Vector3.one*Mathf.Max(.01f,1-(t-.8f)*5);
            }
            stage.player.visual.localRotation=Quaternion.identity;
            stage.player.visual.localPosition=Vector3.zero;
        }
        void Stopped(PlayableDirector source)
        {
            if(!active || stage.Session.mode==GameMode.Loading) return;
            active=false;
            if(sequence<2) stage.Session.Load(stage.config.stageScenes[sequence+1]);
            else { stage.Session.mode=GameMode.Cleared; stage.audioSystem.Play(stage.config.victory); }
        }
    }
}
