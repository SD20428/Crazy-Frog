using UnityEngine;

namespace FrogEscape
{
    public class ProjectileController : MonoBehaviour
    {
        public StageController stage;
        public float distance,lateral,speed,life;
        public bool resolved;
        public void Launch(float s,float x,float velocity)
        { distance=s; lateral=x; speed=velocity; life=0; resolved=false; gameObject.SetActive(true); Place(); }
        void Place() { transform.SetPositionAndRotation(stage.road.Position(distance,lateral,1.2f),stage.road.Rotation(distance)); }
        void Update()
        {
            if(!stage.Playing) return;
            float previous=distance-stage.player.distance;
            distance+=speed*Time.deltaTime; life+=Time.deltaTime; Place();
            float current=distance-stage.player.distance;
            if(!resolved && previous<1.4f && current>=-1.4f)
            {
                resolved=true;
                if(Mathf.Abs(lateral-stage.player.lateral)<1.25f)
                {
                    if(stage.player.dodge.Active) { stage.Session.dodges++; stage.ShowFeedback("회피 성공!"); }
                    else stage.playerHealth.TryDamage("조준선을 피해보세요");
                }
                else { stage.Session.dodges++; stage.ShowFeedback("회피 성공!"); }
            }
            if(life>3 || current>55) gameObject.SetActive(false);
        }
    }
}
