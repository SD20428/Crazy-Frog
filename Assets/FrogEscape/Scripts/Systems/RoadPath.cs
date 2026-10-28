using UnityEngine;

namespace FrogEscape
{
    public class RoadPath : MonoBehaviour
    {
        public int style;
        public float length = 2500, halfWidth = 5.5f;
        public Vector3 Center(float distance)
        {
            float s = distance;
            float x = style == 1 ? 13*Mathf.Sin(s/190) : 28*Mathf.Sin(s/200) + 8*Mathf.Sin(s/93);
            float y = style == 0 ? 20 + 2*Mathf.Sin(s/180) : style == 1 ? -6 + .7f*Mathf.Sin(s/220) : 1.4f*Mathf.Sin(s/240);
            return transform.TransformPoint(new Vector3(x,y,s));
        }
        public Vector3 Forward(float s) => (Center(s+.3f)-Center(s-.3f)).normalized;
        public Vector3 Right(float s) => Vector3.Cross(Vector3.up, Forward(s)).normalized;
        public Vector3 Position(float s, float lateral=0, float height=0) => Center(s) + Right(s)*lateral + Vector3.up*height;
        public Quaternion Rotation(float s) => Quaternion.LookRotation(Forward(s),Vector3.up);
    }
}
