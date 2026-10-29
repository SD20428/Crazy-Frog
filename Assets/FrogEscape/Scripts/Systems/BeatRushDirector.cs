using System.Collections.Generic;
using UnityEngine;

namespace FrogEscape
{
    // Road-coordinate solids: rendering and swept contact share the same boxes.
    // No beat-time hit test and no blanket SPACE immunity.
    [DefaultExecutionOrder(100)]
    public sealed class BeatRushDirector : MonoBehaviour
    {
        public StageController stage;
        public int Resolved { get; private set; }
        public int Cleared { get; private set; }
        public int Combo { get; private set; }
        public int PatternCount { get; private set; }
        public int BossHealth => Mathf.Max(0,12-Cleared);
        public int BossPhase => 1+Mathf.Min(2,Cleared/4);
        public bool Presenting => true;
        public bool JumpRequired => false;
        public string Cue { get; private set; }
        public float Countdown => stations.Count==0 ? 0 : Mathf.Max(0,(stations[0].z-stage.player.distance)/stage.player.Speed);
        public static readonly Vector3 RiderHalfSize = new Vector3(.55f,1.1f,1.1f);
        const float Ceiling = 7.8f;
        class Solid
        {
            public Transform mesh;
            public LineRenderer outline;
            public Vector3 center, size, previousCenter, previousSize;
            public float angle, previousAngle;
        }
        class Station
        {
            public float z, phase;
            public int index;
            public bool hit, locked;
            public Vector2 aim;
            public readonly List<Solid> solids = new List<Solid>();
            public LineRenderer route;
        }
        readonly List<Station> stations = new List<Station>();
        readonly List<Solid> pool = new List<Solid>();
        readonly List<LineRenderer> routes = new List<LineRenderer>();
        Material material, red, green;
        Vector3 previousPlayer;
        float clock, spacing;
        int variant;
        float Beat => 60f/Mathf.Max(1,stage.config.musicBpm);

        void Start()
        {
            material=new Material(stage.config.warningMaterial);
            material.color=stage.stageIndex==0?new Color(.22f,.08f,.45f):stage.stageIndex==1?new Color(.08f,.35f,.4f):new Color(.22f,.12f,.16f);
            red=new Material(stage.config.warningMaterial);
            red.color=new Color(1,.025f,.035f);
            red.EnableKeyword("_EMISSION");red.SetColor("_EmissionColor",Color.red*1.5f);
            green=new Material(stage.config.successMaterial);
            green.color=new Color(.1f,1,.65f);
            previousPlayer=stage.player.CoursePosition;
            BeginSection();
        }
        LineRenderer Line(string name,Material mat)
        {
            var go=new GameObject(name);go.transform.SetParent(transform,false);
            var line=go.AddComponent<LineRenderer>();
            line.useWorldSpace=false;line.sharedMaterial=mat;line.widthMultiplier=.09f;
            return line;
        }
        Solid NewSolid()
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name="Course solid";go.transform.SetParent(transform,false);
            var collider=go.GetComponent<Collider>();collider.enabled=false;Destroy(collider);
            go.GetComponent<Renderer>().sharedMaterial=material;
            var solid=new Solid { mesh=go.transform,outline=Line("Red danger box",red) };
            // All twelve box edges, including the depth that must be fully cleared.
            solid.outline.positionCount=16;
            solid.outline.SetPositions(new[]{
                new Vector3(-.5f,-.5f,-.5f),new Vector3(.5f,-.5f,-.5f),new Vector3(.5f,.5f,-.5f),new Vector3(-.5f,.5f,-.5f),new Vector3(-.5f,-.5f,-.5f),
                new Vector3(-.5f,-.5f,.5f),new Vector3(.5f,-.5f,.5f),new Vector3(.5f,.5f,.5f),new Vector3(-.5f,.5f,.5f),new Vector3(-.5f,-.5f,.5f),
                new Vector3(.5f,-.5f,.5f),new Vector3(.5f,-.5f,-.5f),new Vector3(.5f,.5f,-.5f),new Vector3(.5f,.5f,.5f),new Vector3(-.5f,.5f,.5f),new Vector3(-.5f,.5f,-.5f)});
            pool.Add(solid);return solid;
        }
        void BeginSection()
        {
            variant=PatternCount++%3;
            // Four music beats between stations, with enough room for diagonal travel.
            spacing=stage.Settings.speed*Beat*(stage.Session.normalMode?4:5);
            float first=stage.player.distance+spacing+24;
            string[,] cues={
                {"SKY SLALOM / 왼쪽 위 → 오른쪽 아래 → 왼쪽 위","OVER / UNDER / 차단봉 넘고 간판 아래로 하강","DRONE IRIS / 좌우·상하로 움직이는 통로"},
                {"SIPHON / 낮은 배관 → 높은 수문 → 낮은 배관","PRESSURE / 솟는 물기둥 옆으로 · 천장 아래로","CURRENT TUNNEL / 물살에 저항하며 굽은 통로 유지"},
                {"ROTATING BATON / 회전봉 끝을 돌아서 통과","LOCK & RAM / 조준 고정 후 대각선으로 탈출","SCISSORS / 엇갈리는 차단봉 사이 높이 맞추기"}
            };
            Cue=cues[stage.stageIndex,variant];
            int cursor=0;
            for(int i=0;i<3;i++)
            {
                var s=new Station {z=first+i*spacing,index=i,phase=i*Mathf.PI};
                for(int j=0;j<4;j++)s.solids.Add(cursor<pool.Count?pool[cursor++]:NewPooled(ref cursor));
                LineRenderer route;
                if(i<routes.Count)route=routes[i];
                else {route=Line("Suggested opening",green);route.loop=true;route.positionCount=4;routes.Add(route);}
                s.route=route;stations.Add(s);
                Shape(s);
                foreach(var box in s.solids)SavePose(box);
            }
        }
        Solid NewPooled(ref int cursor) { cursor++;return NewSolid(); }
        void Box(Station s,int i,float x,float y,float width,float height,float depth=3,float angle=0)
        {
            var b=s.solids[i];b.center=new Vector3(x,y,s.z);b.size=new Vector3(width,height,depth);b.angle=angle;
        }
        void Gate(Station s,float x,float y,float width=3.8f,float height=3.4f,float depth=3)
        {
            float edge=stage.road.halfWidth,left=x-width*.5f,right=x+width*.5f;
            float bottom=y-height*.5f,top=y+height*.5f;
            Box(s,0,(-edge+left)*.5f,Ceiling*.5f,left+edge,Ceiling,depth);
            Box(s,1,(edge+right)*.5f,Ceiling*.5f,edge-right,Ceiling,depth);
            Box(s,2,x,bottom*.5f,width,Mathf.Max(.02f,bottom),depth);
            Box(s,3,x,(top+Ceiling)*.5f,width,Mathf.Max(.02f,Ceiling-top),depth);
            s.route.gameObject.SetActive(true);
            s.route.transform.SetPositionAndRotation(stage.road.Position(s.z,x,y),stage.road.Rotation(s.z));
            s.route.SetPositions(new[]{new Vector3(-width*.5f,-height*.5f,-depth*.5f-.1f),new Vector3(width*.5f,-height*.5f,-depth*.5f-.1f),new Vector3(width*.5f,height*.5f,-depth*.5f-.1f),new Vector3(-width*.5f,height*.5f,-depth*.5f-.1f)});
        }
        void Shape(Station s)
        {
            foreach(var b in s.solids)b.size=Vector3.zero;
            s.route.gameObject.SetActive(false);
            float sign=s.index%2==0?-1:1;
            float pulse=Mathf.Sin(clock/Beat*Mathf.PI*.25f+s.phase);
            if(stage.stageIndex==0)
            {
                if(variant==0)Gate(s,sign*2,s.index%2==0?4.8f:1.9f);
                else if(variant==1)
                {
                    // Full-width floor/ceiling prevents bypassing the vertical task at the road edge.
                    Gate(s,-sign*1.8f,s.index%2==0?4.9f:1.8f,4.2f,3.5f,8);
                }
                else Gate(s,pulse*1.8f,3.7f+Mathf.Cos(clock/Beat*Mathf.PI*.25f+s.phase)*1.5f,4.2f,3.8f,6);
            }
            else if(stage.stageIndex==1)
            {
                if(variant==0)Gate(s,sign*1.5f,s.index%2==0?1.8f:4.9f,4,3.5f,10);
                else if(variant==1)
                {
                    float waterHeight=3.8f+1.5f*pulse;
                    Box(s,0,sign*2,waterHeight*.5f,3.5f,waterHeight,12);
                    Box(s,1,0,6.7f,11,2.2f,12);
                    // Vertical jet and low ceiling leave a lateral escape, not a jump shortcut.
                }
                else Gate(s,sign*1.6f,3.2f+sign*.8f,4.3f,3.8f,20);
            }
            else
            {
                if(variant==0)
                {
                    Box(s,0,0,3.8f,8,.65f,5,clock/Beat*16+s.index*65);
                    Box(s,1,0,.45f,11,.9f,5);
                    Box(s,2,0,7.4f,11,.8f,5);
                }
                else if(variant==1)
                {
                    float dz=s.z-stage.player.distance;
                    if(!s.locked)
                    {
                        s.aim=new Vector2(stage.player.lateral,stage.player.CoursePosition.y);
                        if(dz<stage.Settings.speed*1.7f)s.locked=true;
                    }
                    if(s.locked)s.z-=stage.Settings.speed*.55f*Time.deltaTime;
                    Box(s,0,s.aim.x,s.aim.y,2.8f,2.8f,6);
                    // An alternating ceiling/floor escort prevents repeating only a sideways dodge.
                    Box(s,1,0,s.index%2==0?6.6f:.65f,11,s.index%2==0?2.4f:1.3f,6);
                }
                else
                {
                    float gap=3.8f+pulse*1.7f;
                    Gate(s,sign*1.7f,gap,4.2f,3.6f,9);
                }
            }
        }
        static void SavePose(Solid b) { b.previousCenter=b.center;b.previousSize=b.size;b.previousAngle=b.angle; }
        // Slab sweep in obstacle-local space. Includes the rider's actual height and body width.
        public static bool SweptBox(Vector3 from,Vector3 to,Vector3 center,Vector3 size,float angle)
        {
            var inverse=Quaternion.Euler(0,0,-angle);
            from=inverse*(from-center);to=inverse*(to-center);
            float radians=angle*Mathf.Deg2Rad,c=Mathf.Abs(Mathf.Cos(radians)),s=Mathf.Abs(Mathf.Sin(radians));
            Vector3 extent=size*.5f+new Vector3(c*RiderHalfSize.x+s*RiderHalfSize.y,s*RiderHalfSize.x+c*RiderHalfSize.y,RiderHalfSize.z);
            float enter=0,exit=1;
            for(int axis=0;axis<3;axis++)
            {
                float delta=to[axis]-from[axis];
                if(Mathf.Abs(delta)<.00001f) { if(Mathf.Abs(from[axis])>extent[axis])return false;continue; }
                float a=(-extent[axis]-from[axis])/delta,b=(extent[axis]-from[axis])/delta;
                enter=Mathf.Max(enter,Mathf.Min(a,b));exit=Mathf.Min(exit,Mathf.Max(a,b));
                if(enter>exit)return false;
            }
            return true;
        }
        bool Contact(Solid b,Vector3 player)
        {
            if(b.size.sqrMagnitude<.01f)return false;
            // Subdivide moving/rotating geometry; sweep each relative-motion interval.
            int steps=Mathf.Clamp(Mathf.CeilToInt(Mathf.Abs(b.angle-b.previousAngle)/2)+1,1,64);
            for(int i=0;i<steps;i++)
            {
                float a=(float)i/steps,z=(float)(i+1)/steps;
                Vector3 ca=Vector3.Lerp(b.previousCenter,b.center,a),cz=Vector3.Lerp(b.previousCenter,b.center,z);
                Vector3 from=Vector3.Lerp(previousPlayer,player,a)-ca, to=Vector3.Lerp(previousPlayer,player,z)-cz;
                Vector3 size=Vector3.Max(b.previousSize,b.size);
                if(SweptBox(from,to,Vector3.zero,size,Mathf.Lerp(b.previousAngle,b.angle,(a+z)*.5f)))return true;
            }
            return false;
        }
        void Update()
        {
            if(!stage.Playing)
            {
                if(stage.Session.mode!=GameMode.Paused)
                {
                    foreach(var b in pool){b.mesh.gameObject.SetActive(false);b.outline.gameObject.SetActive(false);}
                    foreach(var route in routes)route.gameObject.SetActive(false);
                }
                return;
            }
            clock+=Time.deltaTime;
            if(stage.stageIndex==1 && variant==2)
            {
                stage.player.lateral=Mathf.Clamp(stage.player.lateral+Mathf.Sin(clock/Beat*.5f)*1.7f*Time.deltaTime,-4.8f,4.8f);
                stage.player.Place();
            }
            Vector3 player=stage.player.CoursePosition;
            for(int n=stations.Count-1;n>=0;n--)
            {
                var station=stations[n];Shape(station);
                foreach(var b in station.solids)
                {
                    bool active=b.size.sqrMagnitude>.01f;
                    b.mesh.gameObject.SetActive(active);b.outline.gameObject.SetActive(active);
                    if(active)
                    {
                        Quaternion rot=stage.road.Rotation(b.center.z)*Quaternion.Euler(0,0,b.angle);
                        Vector3 pos=stage.road.Position(b.center.z,b.center.x,b.center.y);
                        b.mesh.SetPositionAndRotation(pos,rot);b.mesh.localScale=b.size;
                        b.outline.transform.SetPositionAndRotation(pos,rot);b.outline.transform.localScale=b.size*1.008f;
                        if(Contact(b,player) && !station.hit)
                        {
                            station.hit=true;Combo=0;
                            stage.playerHealth.TryDamage("입체 장애물 충돌 · 높이와 좌우를 함께 조절하세요",false);
                        }
                    }
                    SavePose(b);
                }
                float depth=0;foreach(var b in station.solids)depth=Mathf.Max(depth,b.size.z);
                if(player.z>station.z+depth*.5f+RiderHalfSize.z)
                {
                    Resolved++;
                    if(!station.hit){Cleared++;Combo++;stage.ShowFeedback("COURSE CLEAR! "+Combo+" COMBO",.7f);}
                    foreach(var b in station.solids){b.mesh.gameObject.SetActive(false);b.outline.gameObject.SetActive(false);}
                    station.route.gameObject.SetActive(false);stations.RemoveAt(n);
                }
            }
            previousPlayer=player;
            if(stage.stageIndex==2)
            {
                stage.police.transform.SetPositionAndRotation(stage.road.Position(player.z+65,Mathf.Sin(clock)*2,.15f),stage.road.Rotation(player.z+65)*Quaternion.Euler(0,180,0));
                if(BossHealth==0 && stage.Playing){stage.Session.Load(stage.config.endingScene);return;}
            }
            if(stations.Count==0)BeginSection();
        }
        void OnDestroy()
        {
            if(material)Destroy(material);if(red)Destroy(red);if(green)Destroy(green);
        }
    }
}
