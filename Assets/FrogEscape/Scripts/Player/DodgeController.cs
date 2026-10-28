using UnityEngine;

namespace FrogEscape
{
    public class DodgeController : MonoBehaviour
    {
        public StageController stage;
        public float startedAt=-100, readyAt;
        public bool Active => Time.time-startedAt < stage.Settings.dodgeDuration;
        public float Phase => Mathf.Clamp01((Time.time-startedAt)/stage.Settings.dodgeDuration);
        public float CooldownProgress => 1-Mathf.Clamp01((readyAt-Time.time)/stage.Settings.dodgeCooldown);
        public bool TryDodge()
        {
            if (!stage.Playing || Time.time < readyAt) return false;
            startedAt=Time.time; readyAt=Time.time+stage.Settings.dodgeCooldown;
            stage.audioSystem.Play(stage.config.dodge,.5f);
            if (stage.stageIndex==2) stage.missile.NotifyDodge();
            return true;
        }
    }
}
