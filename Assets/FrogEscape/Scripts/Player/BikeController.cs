using UnityEngine;
using UnityEngine.InputSystem;

namespace FrogEscape
{
    public class BikeController : MonoBehaviour
    {
        public StageController stage;
        public Transform visual, rider;
        public DodgeController dodge;
        public float distance=45, lateral, lateralVelocity;
        public float steeringSmoothing=12;
        public float altitude, verticalVelocity;
        public float HopHeight => dodge && dodge.Active ? Mathf.Sin(dodge.Phase*Mathf.PI)*.95f : 0;
        public Vector3 CoursePosition => new Vector3(lateral, altitude+HopHeight+1.5f, distance);
        float lean, slowUntil;
        public float Speed => stage.Settings.speed * (Time.time < slowUntil ? .72f : 1);
        void Update()
        {
            if (!stage.Playing) return;
            var keyboard=Keyboard.current;
            float input=0, lift=0;
            if (keyboard != null)
            {
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) input-=1;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) input+=1;
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) lift+=1;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) lift-=1;
                if (keyboard.spaceKey.wasPressedThisFrame) dodge.TryDodge();
            }
            Step(input,lift,Time.deltaTime);
        }
        public void Step(float input,float dt)
        { Step(input,0,dt); }
        public void Step(float input,float lift,float dt)
        {
            verticalVelocity=Mathf.Lerp(verticalVelocity,Mathf.Clamp(lift,-1,1)*5.5f,1-Mathf.Exp(-10*dt));
            altitude=Mathf.Clamp(altitude+verticalVelocity*dt,0,4.8f);
            lateralVelocity=Mathf.Lerp(lateralVelocity,input*stage.Settings.lateralSpeed,1-Mathf.Exp(-steeringSmoothing*dt));
            lateral=Mathf.Clamp(lateral+lateralVelocity*dt,-stage.road.halfWidth+.7f,stage.road.halfWidth-.7f);
            distance=Mathf.Min(distance+Speed*dt,stage.road.length-100);
            lean=Mathf.Lerp(lean,-input*20,1-Mathf.Exp(-9*dt));
            Place();
            float arc=dodge.Active ? Mathf.Sin(dodge.Phase*Mathf.PI) : 0;
            visual.localPosition=new Vector3(0,arc*.95f,0);
            visual.localRotation=Quaternion.Euler(arc*-12,0,lean+arc*38);
        }
        public void Place() { transform.SetPositionAndRotation(stage.road.Position(distance,lateral,.15f+altitude),stage.road.Rotation(distance)); }
        public void Recover() { slowUntil=Time.time+1; lateral=Mathf.Clamp(lateral*.8f,-4.8f,4.8f); }
    }
}
