using UnityEngine;

namespace FrogEscape
{
    public class PlayerHealth : MonoBehaviour
    {
        public StageController stage;
        public float invincibleUntil;
        public int damageEvents;
        public bool TryDamage(string reason, bool canDodge=true)
        {
            if (!stage.Playing || Time.time<invincibleUntil || (canDodge && stage.player.dodge.Active)) return false;
            invincibleUntil=Time.time+stage.Settings.invulnerability;
            stage.Session.health=Mathf.Max(0,stage.Session.health-1);
            stage.Session.hits++; damageEvents++;
            stage.player.Recover();
            stage.audioSystem.Play(stage.config.impact);
            stage.ShowFeedback(reason+" · 잠시 무적",2);
            if (stage.Session.health==0) stage.Session.mode=GameMode.Failed;
            return true;
        }
    }
}
