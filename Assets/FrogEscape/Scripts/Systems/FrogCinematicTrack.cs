using UnityEngine;
using UnityEngine.Timeline;

namespace FrogEscape
{
    [TrackColor(.1f,.85f,.7f)]
    [TrackBindingType(typeof(CutsceneController))]
    [TrackClipType(typeof(FrogCinematicClip))]
    public class FrogCinematicTrack : TrackAsset { }
}
