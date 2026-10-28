using UnityEngine;

namespace FrogEscape
{
    public class SewerAtmosphere : MonoBehaviour
    {
        public StageController stage;
        public Renderer[] roofSections;
        public float[] sectionDistances;
        public Light[] lamps;
        public Transform[] ceilingFixtures;
        public Material exitSky;
        bool openedExit;
        void LateUpdate()
        {
            foreach(var lamp in lamps)if(lamp)lamp.enabled=!openedExit && Mathf.Abs(lamp.transform.position.z-stage.player.transform.position.z)<75;
            // The scripted breakout begins at a difficulty-dependent distance.
            // Open the roof ahead of that exit so the jump never goes through masonry.
            if(!openedExit && stage.Session.mode==GameMode.Cinematic && stage.cinematic.sequence==1)
            {
                openedExit=true;
                for(int i=0;i<roofSections.Length;i++)if(sectionDistances[i]+20>stage.player.distance-20)roofSections[i].enabled=false;
                foreach(var fixture in ceilingFixtures)if(fixture.position.z>stage.player.distance-20)fixture.gameObject.SetActive(false);
                RenderSettings.skybox=exitSky;stage.chaseCamera.view.clearFlags=CameraClearFlags.Skybox;
                RenderSettings.fogColor=new Color(.59f,.81f,.96f);
                RenderSettings.fogDensity=.005f;
            }
            if(openedExit)
            {
                float daylight=Mathf.Clamp01(stage.cinematic.progress*2);
                RenderSettings.sun.intensity=Mathf.Lerp(.3f,1.6f,daylight);
                RenderSettings.ambientLight=Color.Lerp(new Color(.34f,.39f,.3f),new Color(.64f,.74f,.86f),daylight);
            }
        }
    }
}
