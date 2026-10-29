using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace FrogEscape
{
    // Cosmetic child geometry only. Never modifies gameplay transforms or colliders.
    public sealed class DetailArtPass : MonoBehaviour
    {
        public StageController stage;
        readonly List<Material> materials=new List<Material>();
        readonly List<Mesh> meshes=new List<Mesh>();
        readonly List<Transform> roadside=new List<Transform>();
        Material metal, dark, ivory, orange, teal, violet;
        void Start()
        {
            metal=Mat(new Color(.38f,.48f,.57f),.75f,.65f);
            dark=Mat(new Color(.035f,.055f,.085f),.25f,.3f);
            ivory=Mat(new Color(.89f,.91f,.8f),.1f,.4f);
            orange=Mat(new Color(1,.47f,.13f),.25f,.4f);
            teal=Mat(new Color(.15f,.95f,.8f),.25f,.5f,true);
            violet=Mat(new Color(.51f,.24f,.72f),.3f,.5f);
            Bike(); Frog(); Police();
            Missile(stage.missile.model);
            if(stage.cinematic.endingMissile) Missile(stage.cinematic.endingMissile.Find("Model"));
            for(int i=0;i<12;i++)
            {
                var root=Group(transform,"Roadside detail module "+i);
                if(stage.stageIndex==1) Sewer(root,i);
                else if(stage.stageIndex>=2) Outskirts(root,i);
                else City(root,i);
                Combine(root);roadside.Add(root);
            }
        }
        Material Mat(Color color,float metallic,float smooth,bool glow=false)
        {
            var m=new Material(stage.config.successMaterial);m.color=color;
            m.SetFloat("_Metallic",metallic);m.SetFloat("_Smoothness",smooth);
            if(glow){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",color*.8f);}
            else {m.DisableKeyword("_EMISSION");m.SetColor("_EmissionColor",Color.black);}
            materials.Add(m);return m;
        }
        Transform Group(Transform parent,string name)
        {var t=new GameObject(name).transform;t.SetParent(parent,false);return t;}
        Transform Part(Transform parent,string name,PrimitiveType shape,Vector3 p,Vector3 size,Material material,Vector3 angles=default)
        {
            var go=GameObject.CreatePrimitive(shape);go.name=name;go.transform.SetParent(parent,false);
            go.transform.localPosition=p;go.transform.localScale=size;go.transform.localRotation=Quaternion.Euler(angles);
            var c=go.GetComponent<Collider>();c.enabled=false;Destroy(c);
            go.GetComponent<Renderer>().sharedMaterial=material;return go.transform;
        }
        void Box(Transform t,string n,Vector3 p,Vector3 size,Material m,Vector3 angles=default)
        {Part(t,n,PrimitiveType.Cube,p,size,m,angles);}
        void Bolt(Transform t,Vector3 p,float scale=.055f)
        {Part(t,"Fastener",PrimitiveType.Sphere,p,Vector3.one*scale,metal);}
        void Ring(Transform t,string name,Vector3 center,float radius,float thickness,Material m,Vector3 angles=default)
        {
            Quaternion q=Quaternion.Euler(angles);
            for(int i=0;i<16;i++)
            {
                float a=i*Mathf.PI*2/16;
                Vector3 p=center+q*new Vector3(Mathf.Cos(a)*radius,Mathf.Sin(a)*radius,0);
                var part=Part(t,name,PrimitiveType.Cube,p,new Vector3(thickness,radius*.41f,thickness),m);
                part.localRotation=q*Quaternion.Euler(0,0,a*Mathf.Rad2Deg);
            }
        }
        void Bike()
        {
            var t=Group(stage.player.visual,"Precision bike trim");
            Box(t,"Dashboard",new Vector3(0,1.56f,.64f),new Vector3(.38f,.08f,.24f),dark,new Vector3(-22,0,0));
            Box(t,"Speedometer",new Vector3(0,1.607f,.64f),new Vector3(.26f,.015f,.13f),teal,new Vector3(-22,0,0));
            for(int side=-1;side<=1;side+=2)
            {
                Box(t,"Brake lever",new Vector3(side*.6f,1.59f,.96f),new Vector3(.29f,.035f,.04f),metal);
                Box(t,"Radiator shroud",new Vector3(side*.57f,.7f,.34f),new Vector3(.055f,.32f,.64f),dark);
                for(int j=0;j<5;j++)Box(t,"Cooling slot",new Vector3(side*.607f,.58f+j*.055f,.34f),new Vector3(.025f,.018f,.52f),metal);
                for(int z=-1;z<=1;z+=2)
                {
                    Ring(t,"Brake disc",new Vector3(side*.29f,.57f,z*1.1f),.26f,.035f,metal,new Vector3(0,90,0));
                    Box(t,"Brake caliper",new Vector3(side*.32f,.68f,z*1.1f+.2f),new Vector3(.09f,.2f,.12f),orange);
                }
                Box(t,"Rear grab rail",new Vector3(side*.44f,1.3f,-1),new Vector3(.05f,.07f,.52f),metal);
                Bolt(t,new Vector3(side*.59f,.9f,.78f));Bolt(t,new Vector3(side*.59f,.9f,-.3f));
            }
            Box(t,"Rear plate",new Vector3(0,.7f,-1.37f),new Vector3(.4f,.22f,.035f),ivory);
            for(int i=0;i<4;i++)Box(t,"Plate digits",new Vector3(-.12f+i*.08f,.7f,-1.395f),new Vector3(.035f,.1f,.012f),dark);
            Combine(t);
        }
        void Frog()
        {
            var t=Group(stage.player.rider,"Rider fine details");
            for(int side=-1;side<=1;side+=2)
            {
                Box(t,"Goggle temple",new Vector3(side*.59f,1.46f,.3f),new Vector3(.075f,.12f,.48f),dark);
                Bolt(t,new Vector3(side*.62f,1.46f,.51f),.08f);
                for(int j=0;j<3;j++)Part(t,"Cheek freckles",PrimitiveType.Sphere,new Vector3(side*(.34f+j*.07f),1.05f,.6f-j*.04f),new Vector3(.045f,.035f,.025f),orange);
                Part(t,"Glove",PrimitiveType.Sphere,new Vector3(side*.57f,.13f,.79f),new Vector3(.28f,.21f,.26f),dark);
                for(int j=0;j<3;j++)Box(t,"Glove stitching",new Vector3(side*.57f+(j-1)*.055f,.22f,.8f),new Vector3(.018f,.02f,.13f),ivory);
                Box(t,"Boot sole",new Vector3(side*.58f,-.67f,.05f),new Vector3(.37f,.055f,.69f),dark);
                for(int j=0;j<3;j++)Box(t,"Boot buckle",new Vector3(side*.58f,-.43f,j*.12f-.08f),new Vector3(.27f,.035f,.04f),metal);
                Box(t,"Pack buckle",new Vector3(side*.2f,.28f,-.675f),new Vector3(.13f,.16f,.035f),metal);
            }
            for(int i=0;i<7;i++)Box(t,"Pack zipper",new Vector3(0,.02f+i*.09f,-.62f),new Vector3(.045f,.035f,.03f),ivory);
            Part(t,"Backpack patch",PrimitiveType.Sphere,new Vector3(0,.53f,-.615f),new Vector3(.22f,.22f,.035f),orange);
            Combine(t);
        }
        void Police()
        {
            var t=Group(stage.police.transform,"Interceptor precision trim");
            for(int side=-1;side<=1;side+=2)
            {
                Box(t,"Push bar upright",new Vector3(side*.8f,.76f,2.5f),new Vector3(.09f,.7f,.1f),dark);
                Box(t,"Door handle",new Vector3(side*1.19f,1.05f,-.4f),new Vector3(.055f,.08f,.27f),metal);
                Box(t,"Window divider",new Vector3(side*.99f,1.4f,-.3f),new Vector3(.04f,.57f,.08f),dark);
                for(int z=-1;z<=1;z+=2)
                    for(int j=0;j<5;j++)
                    {float a=j*Mathf.PI*2/5;Bolt(t,new Vector3(side*1.32f,.5f+Mathf.Sin(a)*.14f,z*1.4f+Mathf.Cos(a)*.14f),.055f);}
                Box(t,"Rear lamp surround",new Vector3(side*.78f,.87f,-2.28f),new Vector3(.55f,.25f,.08f),dark);
                Box(t,"Rear lamp",new Vector3(side*.78f,.87f,-2.33f),new Vector3(.45f,.13f,.035f),orange);
            }
            Box(t,"Push bar",new Vector3(0,.72f,2.54f),new Vector3(2.1f,.08f,.08f),metal);
            for(int j=0;j<5;j++)Box(t,"Hood intake",new Vector3((j-2)*.17f,1.095f,1.55f),new Vector3(.08f,.035f,.5f),dark);
            Box(t,"Roof identification stripe",new Vector3(0,1.795f,.2f),new Vector3(.12f,.02f,.7f),ivory);
            Combine(t);
        }
        void Missile(Transform model)
        {
            if(!model)return;var t=Group(model,"Guidance and engine details");
            Ring(t,"Engine collar",new Vector3(0,0,-1.6f),.43f,.07f,metal);
            Ring(t,"Thruster core",new Vector3(0,0,-1.66f),.27f,.05f,teal);
            for(int i=0;i<4;i++)
            {
                float angle=i*90;var rot=Quaternion.Euler(0,0,angle);
                var fin=Part(t,"Stabilizer trim",PrimitiveType.Cube,rot*new Vector3(.48f,0,-1.15f),new Vector3(.48f,.055f,.55f),dark);
                fin.localRotation=rot;
                Bolt(t,rot*new Vector3(.34f,0,.75f),.06f);
            }
            Part(t,"Guidance lens",PrimitiveType.Sphere,new Vector3(0,.12f,1.62f),new Vector3(.2f,.2f,.1f),teal);
            for(int j=0;j<3;j++)Box(t,"Telemetry band",new Vector3(0,.36f,-.3f+j*.16f),new Vector3(.38f,.025f,.045f),orange);
            Combine(t);
        }
        void City(Transform t,int i)
        {
            Box(t,"Kiosk pedestal",new Vector3(0,1.4f,0),new Vector3(1.35f,2.8f,.8f),dark);
            Box(t,"Ad frame",new Vector3(0,2.5f,-.48f),new Vector3(1.65f,2.15f,.16f),metal);
            Box(t,"Ad face",new Vector3(0,2.5f,-.58f),new Vector3(1.43f,1.94f,.05f),i%2==0?violet:teal);
            for(int j=0;j<4;j++)Box(t,"Graphic equalizer",new Vector3(-.48f+j*.32f,2.4f,-.62f),new Vector3(.13f,.4f+j*.22f,.03f),ivory);
            Box(t,"Service cabinet",new Vector3(1.35f,.65f,1.3f),new Vector3(.65f,1.3f,.8f),metal);
            for(int j=0;j<5;j++)Box(t,"Cabinet louvre",new Vector3(1.35f,.4f+j*.12f,.88f),new Vector3(.46f,.035f,.03f),dark);
            for(int j=0;j<3;j++)Box(t,"Cable bundle",new Vector3(1.2f+j*.09f,.12f,2.2f),new Vector3(.045f,.045f,1.2f),dark);
        }
        void Sewer(Transform t,int i)
        {
            Box(t,"Maintenance panel",new Vector3(0,1.55f,0),new Vector3(1.2f,1.8f,.22f),metal);
            Box(t,"Inset panel",new Vector3(0,1.55f,-.14f),new Vector3(1.03f,1.6f,.06f),dark);
            Ring(t,"Pressure gauge bezel",new Vector3(-.22f,1.9f,-.2f),.23f,.035f,ivory);
            Box(t,"Gauge needle",new Vector3(-.22f,1.9f,-.23f),new Vector3(.025f,.31f,.025f),orange,new Vector3(0,0,-35));
            for(int j=0;j<3;j++)Part(t,"Status lamp",PrimitiveType.Sphere,new Vector3(.33f,1.4f+j*.22f,-.22f),Vector3.one*.1f,j==i%3?teal:orange);
            for(int j=0;j<5;j++)Box(t,"Vent slit",new Vector3(-.18f,.95f+j*.1f,-.2f),new Vector3(.46f,.035f,.04f),metal);
            for(int side=-1;side<=1;side+=2)for(int y=0;y<2;y++)Bolt(t,new Vector3(side*.49f, .84f+y*1.42f,-.23f),.07f);
            Ring(t,"Valve wheel",new Vector3(1.1f,1.5f,0),.35f,.07f,orange);
            for(int j=0;j<3;j++)Box(t,"Valve spoke",new Vector3(1.1f,1.5f,0),new Vector3(.6f,.04f,.05f),metal,new Vector3(0,0,j*60));
        }
        void Outskirts(Transform t,int i)
        {
            Box(t,"Crash barrier base",new Vector3(0,.4f,0),new Vector3(2.1f,.8f,1),metal);
            for(int j=0;j<5;j++)Box(t,"Hazard stripe",new Vector3(-.8f+j*.4f,.45f,-.52f),new Vector3(.18f,.6f,.03f),orange,new Vector3(0,0,-22));
            Box(t,"Beacon mast",new Vector3(0,2.2f,.35f),new Vector3(.12f,3.6f,.12f),dark);
            Box(t,"Solar frame",new Vector3(0,3.9f,.35f),new Vector3(1.6f,.08f,1),metal,new Vector3(18,0,0));
            for(int j=0;j<4;j++)Box(t,"Solar cells",new Vector3(-.57f+j*.38f,3.96f,.35f),new Vector3(.31f,.025f,.85f),violet,new Vector3(18,0,0));
            Part(t,"Route beacon",PrimitiveType.Sphere,new Vector3(0,3.3f,-.1f),new Vector3(.24f,.24f,.24f),teal);
            for(int j=0;j<3;j++)Part(t,"Ground rubble",PrimitiveType.Cube,new Vector3(1.6f+j*.32f,.1f,1),new Vector3(.3f,.2f,.4f),dark,new Vector3(j*11,j*35,12));
        }
        void Combine(Transform root)
        {
            var groups=new Dictionary<Material,List<CombineInstance>>();
            var parts=root.GetComponentsInChildren<MeshFilter>(true);
            foreach(var part in parts)
            {
                var r=part.GetComponent<MeshRenderer>();var m=r.sharedMaterial;
                if(!groups.TryGetValue(m,out var list)){list=new List<CombineInstance>();groups[m]=list;}
                list.Add(new CombineInstance{mesh=part.sharedMesh,transform=root.worldToLocalMatrix*part.transform.localToWorldMatrix});
                r.enabled=false;Destroy(part.gameObject);
            }
            foreach(var pair in groups)
            {
                var mesh=new Mesh{name="Combined detail geometry",indexFormat=IndexFormat.UInt32};
                mesh.CombineMeshes(pair.Value.ToArray(),true,true);meshes.Add(mesh);
                var child=Group(root,"Detail surface");child.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;
                var r=child.gameObject.AddComponent<MeshRenderer>();r.sharedMaterial=pair.Key;
                r.shadowCastingMode=ShadowCastingMode.Off;
            }
        }
        void LateUpdate()
        {
            float origin=Mathf.Floor(stage.player.distance/32)*32;
            for(int i=0;i<roadside.Count;i++)
            {
                var t=roadside[i];bool sewer=stage.stageIndex==1;
                t.gameObject.SetActive(!sewer || stage.Session.mode!=GameMode.Cinematic);
                float side=i%2==0?-1:1;
                t.SetPositionAndRotation(stage.road.Position(origin+(i/2-1)*32,side*(sewer?7.5f:7.8f)),stage.road.Rotation(origin+(i/2-1)*32));
            }
        }
        void OnDestroy(){foreach(var m in materials)if(m)Destroy(m);foreach(var mesh in meshes)if(mesh)Destroy(mesh);}
    }
}
