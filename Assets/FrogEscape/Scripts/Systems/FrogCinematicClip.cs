using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace FrogEscape
{
    public class FrogCinematicClip : PlayableAsset, ITimelineClipAsset
    {
        public ClipCaps clipCaps => ClipCaps.None;
        public override Playable CreatePlayable(PlayableGraph graph,GameObject owner) => ScriptPlayable<FrogCinematicBehaviour>.Create(graph);
    }
    public class FrogCinematicBehaviour : PlayableBehaviour
    {
        public override void ProcessFrame(Playable playable,FrameData info,object playerData)
        {
            if(playerData is CutsceneController controller && Application.isPlaying)
                controller.Evaluate((float)(playable.GetTime()/System.Math.Max(.001,playable.GetDuration())));
        }
    }
}
