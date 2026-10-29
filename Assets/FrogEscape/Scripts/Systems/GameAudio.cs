using UnityEngine;

namespace FrogEscape
{
    public class GameAudio : MonoBehaviour
    {
        public StageController stage;
        public AudioSource music,engine,sfx,siren;
        void Start()
        {
            music.clip=stage.config.music; music.loop=true; music.volume=.42f; music.Play();
            engine.clip=stage.config.engine; engine.loop=true; engine.volume=.06f; engine.Play();
            siren.clip=stage.config.siren; siren.loop=true; siren.volume=0; siren.Play();
        }
        void Update()
        {
            bool warning=stage.stageIndex==1 && stage.attack.IsWarning || stage.stageIndex==2 && stage.missile.state==MissileState.Warning;
            music.volume=Mathf.Lerp(music.volume,warning?.28f:.42f,Time.unscaledDeltaTime*8);
            engine.volume=stage.Playing?.065f:.025f;
            engine.pitch=stage.Playing?1+stage.player.Speed/75:.75f;
            siren.volume=stage.Playing && stage.elapsed>8?.025f:0;
            if(stage.Session.mode==GameMode.Loading || stage.Session.mode==GameMode.Failed) { engine.volume=0; siren.volume=0; }
        }
        public void Play(AudioClip clip,float volume=.7f) { if(clip) sfx.PlayOneShot(clip,volume); }
    }
}
