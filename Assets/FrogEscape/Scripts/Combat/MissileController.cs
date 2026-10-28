using UnityEngine;

namespace FrogEscape
{
    public enum MissileState { Despawn, Spawn, Pursue, LockOn, Warning, Dash, Hit, Miss }
    public class MissileController : MonoBehaviour
    {
        public StageController stage;
        public Transform model,warningZone;
        public MissileState state;
        public int wave;
        public float timer, nextSpawn=12, lockedLane;
        public bool acceptedDodge;
        bool finalAssistApplied;
        Vector3 dashDirection;
        public float WarningDuration => stage.Settings.warningTime + (wave==1?1:.35f);
        public bool DodgeWindow => (state==MissileState.Warning && WarningDuration-timer<=stage.Settings.missileDodgeWindow) || (state==MissileState.Dash && timer<.22f);
        public void NotifyDodge()
        {
            if(DodgeWindow) { acceptedDodge=true; stage.ShowFeedback("좋아요! 그대로 피하세요",1); }
            else if(state==MissileState.Warning || state==MissileState.LockOn) stage.ShowFeedback("조금 빨라요! 초록 신호를 기다리세요",1.8f);
        }
        void SetState(MissileState value) { state=value; timer=0; }
        void Update()
        {
            if(!stage.Playing || stage.stageIndex!=2) { if(warningZone) warningZone.gameObject.SetActive(false); return; }
            timer+=Time.deltaTime;
            if(state==MissileState.Despawn)
            {
                model.gameObject.SetActive(false);
                if(stage.elapsed>=nextSpawn)
                {
                    wave=Mathf.Min(wave+1,3); acceptedDodge=false;
                    if(wave==3 && !stage.Session.normalMode && !finalAssistApplied) { stage.Session.health=Mathf.Max(2,stage.Session.health);finalAssistApplied=true; }
                    transform.SetPositionAndRotation(stage.road.Position(stage.player.distance-30,0,2.3f),stage.road.Rotation(stage.player.distance));
                    model.gameObject.SetActive(true); SetState(MissileState.Spawn); stage.audioSystem.Play(stage.config.warning);
                }
                return;
            }
            if(state==MissileState.Spawn || state==MissileState.Pursue || state==MissileState.LockOn || state==MissileState.Warning)
            {
                float gap=state==MissileState.Warning?14:state==MissileState.LockOn?19:25;
                float x=stage.player.lateral+stage.player.lateralVelocity*stage.Settings.prediction*(wave==1?.3f:1);
                Vector3 target=stage.road.Position(stage.player.distance-gap,Mathf.Clamp(x,-4.8f,4.8f),1.8f);
                Vector3 delta=target-transform.position;
                if(delta.sqrMagnitude>.1f)
                {
                    Vector3 direction=Vector3.RotateTowards(transform.forward,delta.normalized,stage.Settings.missileTurnRate*Mathf.Deg2Rad*Time.deltaTime,0);
                    transform.rotation=Quaternion.LookRotation(direction);
                    float step=Mathf.Min(delta.magnitude,Mathf.Min(stage.Settings.missileMaxSpeed,stage.player.Speed+delta.magnitude*2)*Time.deltaTime);
                    transform.position+=direction*step;
                }
                if(state==MissileState.Spawn && timer>1) SetState(MissileState.Pursue);
                else if(state==MissileState.Pursue && timer>4) SetState(MissileState.LockOn);
                else if(state==MissileState.LockOn && timer>1.4f) { SetState(MissileState.Warning); stage.audioSystem.Play(stage.config.warning); }
                else if(state==MissileState.Warning)
                {
                    lockedLane=stage.player.lateral;
                    warningZone.gameObject.SetActive(true);
                    warningZone.SetPositionAndRotation(stage.road.Position(stage.player.distance+10,lockedLane,.08f),stage.road.Rotation(stage.player.distance));
                    if(timer>=WarningDuration)
                    {
                        dashDirection=(stage.road.Position(stage.player.distance+stage.player.Speed*.38f,lockedLane+(acceptedDodge?3:0),1.15f)-transform.position).normalized;
                        transform.rotation=Quaternion.LookRotation(dashDirection); SetState(MissileState.Dash);
                    }
                }
            }
            else if(state==MissileState.Dash)
            {
                transform.position+=dashDirection*(stage.Settings.missileMaxSpeed+35+wave*4)*Time.deltaTime;
                if(timer>.48f)
                {
                    bool success=acceptedDodge || Mathf.Abs(stage.player.lateral-lockedLane)>2.5f;
                    warningZone.gameObject.SetActive(false);
                    if(!success) stage.playerHealth.TryDamage("회피 타이밍을 다시 맞춰보세요",false);
                    SetState(success?MissileState.Miss:MissileState.Hit);
                    stage.FinishMissile(success);
                }
            }
            else if(timer>1.2f)
            {
                nextSpawn=stage.elapsed+(wave==3?3:9); SetState(MissileState.Despawn);
            }
        }
    }
}
