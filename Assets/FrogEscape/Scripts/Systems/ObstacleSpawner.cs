using System;
using System.Collections.Generic;
using UnityEngine;

namespace FrogEscape
{
    [Serializable]
    public class RoadObstacle
    {
        public Transform visual;
        public float distance, lateral, width=1.6f, speed;
        public bool passed;
        public int row;
        [NonSerialized] public float initialDistance;
    }
    public class ObstacleSpawner : MonoBehaviour
    {
        public StageController stage;
        public List<RoadObstacle> obstacles = new List<RoadObstacle>();
        public void Configure(DifficultySettings settings)
        {
            foreach(var item in obstacles)
            {
                item.passed=false;
                item.initialDistance=item.distance;
                if (item.visual) item.visual.gameObject.SetActive(item.row%settings.obstacleStride==0);
            }
        }
        void Update()
        {
            if (!stage.Playing || stage.stageIndex!=0) return;
            foreach(var item in obstacles)
            {
                if (!item.visual || !item.visual.gameObject.activeSelf) continue;
                // Keep traffic inside its own row so adjacent rows cannot close the safe lane.
                if (item.speed>0 && !item.passed && item.distance-stage.player.distance<90)
                {
                    item.distance=Mathf.Min(item.initialDistance+12,item.distance+item.speed*Time.deltaTime);
                    item.visual.SetPositionAndRotation(stage.road.Position(item.distance,item.lateral),stage.road.Rotation(item.distance));
                }
                float dz=item.distance-stage.player.distance;
                if (!item.passed && dz < 2.2f && dz > -3)
                {
                    if(Mathf.Abs(item.lateral-stage.player.lateral)<item.width*.5f+.65f)
                    { item.passed=true; stage.playerHealth.TryDamage("장애물 충돌"); }
                }
                if(dz < -4) item.passed=true;
            }
        }
        public float ChooseClearLane(float s,float preferred)
        {
            float best=preferred,bestScore=float.MaxValue;
            float nearest=float.MaxValue;
            foreach(var item in obstacles)
                if(item.visual && item.visual.gameObject.activeSelf && item.distance>s-3 && item.distance<s+60)
                    nearest=Mathf.Min(nearest,item.distance);
            for(int lane=-1;lane<=1;lane++)
            {
                float x=lane*3.4f;bool blocked=false;
                foreach(var item in obstacles)
                    if(item.visual && item.visual.gameObject.activeSelf && item.distance>s-3 && item.distance<=nearest+12 && Mathf.Abs(item.lateral-x)<2.3f) blocked=true;
                if(!blocked && Mathf.Abs(x-preferred)<bestScore) { best=x;bestScore=Mathf.Abs(x-preferred); }
            }
            return best;
        }
    }
}
