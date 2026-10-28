using UnityEngine;

namespace FrogEscape
{
    public enum PoliceState { Patrol, Chase, Avoid, Reposition, Attack, Recover, Cinematic }
    public class PoliceAI : MonoBehaviour
    {
        public StageController stage;
        public PoliceState state;
        public float distance=15,lateral, gap;
        public Transform redLamp,blueLamp;
        Vector3 redScale,blueScale;
        void Awake() { if(redLamp)redScale=redLamp.localScale;if(blueLamp)blueScale=blueLamp.localScale; }
        void Update()
        {
            if(stage.Session.mode==GameMode.Cinematic) { state=PoliceState.Cinematic; return; }
            if(!stage.Playing) return;
            float desired=stage.elapsed<8 ? stage.Settings.policeGap+18 : stage.Settings.policeGap;
            gap=stage.player.distance-distance;
            float speed=Mathf.Clamp(stage.player.Speed+(gap-desired)*1.8f,0,stage.Settings.policeSpeed);
            distance=Mathf.Min(distance+speed*Time.deltaTime,stage.player.distance-7);
            float target=Mathf.Clamp(stage.player.lateral*.7f,-3.8f,3.8f);
            float safe=stage.obstacles.ChooseClearLane(distance,target);
            state=stage.elapsed<8 ? PoliceState.Patrol : Mathf.Abs(target-safe)>.5f ? PoliceState.Avoid : PoliceState.Chase;
            if(stage.stageIndex==1 && stage.attack != null && stage.attack.Busy)
            { state=stage.attack.IsWarning?PoliceState.Reposition:PoliceState.Attack; safe=stage.attack.targetLane; }
            else if(stage.stageIndex==1 && stage.attack != null && stage.attack.Recovering) state=PoliceState.Recover;
            lateral=Mathf.MoveTowards(lateral,safe,4.5f*Time.deltaTime);
            transform.SetPositionAndRotation(stage.road.Position(distance,lateral,.15f),stage.road.Rotation(distance));
            if(redLamp) redLamp.localScale=redScale*(1+.15f*Mathf.Sin(Time.time*17));
            if(blueLamp) blueLamp.localScale=blueScale*(1-.15f*Mathf.Sin(Time.time*17));
        }
    }
}
