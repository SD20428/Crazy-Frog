using UnityEngine;

namespace FrogEscape
{
    public enum AttackState { Recovery, Targeting, Warning, Fire, DodgeCheck }
    public class PoliceAttackController : MonoBehaviour
    {
        public StageController stage;
        public ProjectileController[] pool;
        public Transform warningZone;
        public AttackState state;
        public float targetLane, timer;
        public int pattern;
        int shotsRemaining;
        float nextAttack=9;
        public bool Busy => state != AttackState.Recovery;
        public bool Recovering => state==AttackState.Recovery;
        public bool IsWarning => state==AttackState.Warning || state==AttackState.Targeting;
        public string PatternName => pattern%3==0?"단발 조준":pattern%3==1?"시간차 조준":"예측 조준";
        void Update()
        {
            if(!stage.Playing || stage.stageIndex!=1) { warningZone.gameObject.SetActive(false); return; }
            timer+=Time.deltaTime;
            switch(state)
            {
                case AttackState.Recovery:
                    if(stage.elapsed>=nextAttack) { state=AttackState.Targeting; timer=0; shotsRemaining=pattern%3==1?2:1; }
                    break;
                case AttackState.Targeting:
                    targetLane=Mathf.Clamp(stage.player.lateral+(pattern%3==2?stage.player.lateralVelocity*stage.Settings.prediction:0),-4.4f,4.4f);
                    if(timer>.45f) { state=AttackState.Warning; timer=0; stage.audioSystem.Play(stage.config.warning); }
                    break;
                case AttackState.Warning:
                    warningZone.gameObject.SetActive(true);
                    warningZone.SetPositionAndRotation(stage.road.Position(stage.player.distance+9,targetLane,.06f),stage.road.Rotation(stage.player.distance));
                    if(timer>=stage.Settings.warningTime) { state=AttackState.Fire; timer=0; warningZone.gameObject.SetActive(false); }
                    break;
                case AttackState.Fire:
                    foreach(var shot in pool) if(!shot.gameObject.activeSelf) { shot.Launch(stage.player.distance-22,targetLane,stage.player.Speed+48); break; }
                    state=AttackState.DodgeCheck; timer=0; shotsRemaining--; break;
                case AttackState.DodgeCheck:
                    if(timer>1.2f)
                    {
                        if(shotsRemaining>0) { state=AttackState.Targeting; timer=0; }
                        else { state=AttackState.Recovery; nextAttack=stage.elapsed+stage.Settings.attackInterval; pattern++; timer=0; }
                    }
                    break;
            }
        }
    }
}
