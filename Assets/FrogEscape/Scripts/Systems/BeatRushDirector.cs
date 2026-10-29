using UnityEngine;

namespace FrogEscape
{
    public enum BeatAttack { Meteor, Crossfire, Shockwave, Freeze, Decoy, Orbit }

    // Pooled runtime choreography. No scene or prefab assets are changed.
    public sealed class BeatRushDirector : MonoBehaviour
    {
        public StageController stage;
        public int Resolved { get; private set; }
        public int Cleared { get; private set; }
        public int Combo { get; private set; }
        public BeatAttack Attack { get; private set; }
        public float SafeCenter { get; private set; }
        public float SafeHalfWidth => .9f;
        public bool JumpRequired => Attack == BeatAttack.Shockwave;
        public bool TargetLocked => Attack != BeatAttack.Decoy || clock >= lockAt;
        public float Countdown => Mathf.Max(0, strikeAt - clock);
        public string Cue { get; private set; }
        public bool Presenting { get; private set; } = true;
        static readonly BeatAttack[] Sewer = { BeatAttack.Crossfire, BeatAttack.Decoy, BeatAttack.Freeze, BeatAttack.Crossfire, BeatAttack.Meteor, BeatAttack.Shockwave };
        static readonly BeatAttack[] Finale = { BeatAttack.Orbit, BeatAttack.Crossfire, BeatAttack.Decoy, BeatAttack.Meteor };
        float Beat => 60f / Mathf.Max(1, stage.config.musicBpm);
        float BeatOffset => stage.config.musicBeatOffset;
        const float RiderRadius = .55f;
        const float ImpactDuration = .22f;
        readonly Transform[] shots = new Transform[3];
        readonly Transform[] markers = new Transform[3];
        readonly Transform[] trails = new Transform[3];
        readonly float[] targets = new float[3];
        Transform safeStrip, shockwave;
        Material dangerMaterial, safeMaterial, shotMaterial;
        float clock, strikeAt, interval, plannedAt, lockAt, radius;
        int targetCount, lastSamples = -1;
        bool resolved;

        void Start()
        {
            dangerMaterial = new Material(stage.config.warningMaterial);
            safeMaterial = new Material(stage.config.successMaterial);
            shotMaterial = new Material(stage.config.warningMaterial);
            shotMaterial.EnableKeyword("_EMISSION");
            shotMaterial.SetColor("_EmissionColor", new Color(1,.12f,.4f)*2);
            for (int i = 0; i < shots.Length; i++)
            {
                shots[i] = Make("Beat projectile " + i, PrimitiveType.Sphere, shotMaterial);
                markers[i] = Make("Impact telegraph " + i, PrimitiveType.Cylinder, dangerMaterial);
                trails[i] = Make("Projectile trail " + i, PrimitiveType.Cube, dangerMaterial);
            }
            safeStrip = Make("Safe response", PrimitiveType.Cylinder, safeMaterial);
            shockwave = Make("Jump shockwave", PrimitiveType.Cube, shotMaterial);
            strikeAt = BeatOffset + Mathf.Ceil((4 - BeatOffset) / Beat) * Beat;
            Plan();
        }

        Transform Make(string label, PrimitiveType primitive, Material material)
        {
            var go = GameObject.CreatePrimitive(primitive);
            go.name = label;
            go.transform.SetParent(transform, false);
            var collider = go.GetComponent<Collider>();
            collider.enabled = false;
            Destroy(collider);
            go.GetComponent<Renderer>().sharedMaterial = material;
            if (primitive == PrimitiveType.Cylinder)
            {
                // A crisp hollow landing reticle keeps the road and rider visible.
                go.GetComponent<Renderer>().enabled = false;
                var ring=go.AddComponent<LineRenderer>();
                ring.useWorldSpace=false; ring.loop=true; ring.positionCount=48;
                ring.sharedMaterial=material; ring.widthMultiplier=.045f;
                for(int i=0;i<48;i++)
                {
                    float angle=i*Mathf.PI*2/48;
                    ring.SetPosition(i,new Vector3(Mathf.Cos(angle)*.5f,0,Mathf.Sin(angle)*.5f));
                }
            }
            return go.transform;
        }

        void Plan()
        {
            // Six response types repeat, with mirrored launch directions on the next phrase.
            Attack = stage.stageIndex == 1 ? Sewer[Resolved % Sewer.Length] :
                stage.stageIndex == 2 ? Finale[Resolved % Finale.Length] : (BeatAttack)(Resolved % 6);
            interval = Beat * (stage.Session.normalMode ? 4 : 5);
            plannedAt = clock;
            lockAt = Mathf.Min(clock + Beat, strikeAt - Beat * 2);
            radius = .75f;
            float playerX = stage.player.lateral;
            targetCount = 1;
            switch (Attack)
            {
                case BeatAttack.Meteor:
                    radius = 1.35f;
                    targets[0] = playerX;
                    Cue = "METEOR / 머리 위 폭격! 원 밖으로!";
                    break;
                case BeatAttack.Crossfire:
                    targetCount = 3;
                    targets[0] = -3.6f; targets[1] = 0; targets[2] = 3.6f;
                    Cue = "CROSSFIRE / 대각선 탄막! 탄착점 사이로!";
                    break;
                case BeatAttack.Shockwave:
                    targetCount = 0;
                    Cue = "BASS DROP / 전방위 충격파! 박자에 SPACE!";
                    break;
                case BeatAttack.Freeze:
                    targetCount = 2;
                    targets[0] = playerX - 2.1f; targets[1] = playerX + 2.1f;
                    Cue = "FREEZE / 양옆 협공! 지금 위치를 지켜!";
                    break;
                case BeatAttack.Decoy:
                    radius = 1.15f;
                    targets[0] = playerX;
                    Cue = "FAKE LOCK / 조준 중… 확정되면 벗어나!";
                    break;
                default:
                    targetCount = 2;
                    float center = (Resolved / 6 % 2 == 0 ? 1 : -1) * 1.8f;
                    targets[0] = center - 2.2f; targets[1] = center + 2.2f;
                    Cue = "ORBIT / 나선 미사일! 두 궤도 사이로!";
                    break;
            }
            FindSafeResponse();
            if (stage.stageIndex == 1) Cue = "SEWER RAID / " + Cue;
            if (stage.stageIndex == 2) Cue = "FINAL REMIX / " + Cue;
            resolved = false;
        }

        // Same landing geometry drives both telegraphs and damage adjudication.
        public bool Threatens(float x)
        {
            if (JumpRequired) return true;
            for (int i = 0; i < targetCount; i++)
                if (Mathf.Abs(x - targets[i]) < radius + RiderRadius) return true;
            return false;
        }

        void FindSafeResponse()
        {
            SafeCenter = stage.player.lateral;
            if (JumpRequired) return;
            float best = float.NegativeInfinity;
            float limit = stage.road.halfWidth - .7f;
            for (float x = -limit; x <= limit; x += .1f)
            {
                float clearance = 10;
                for (int i = 0; i < targetCount; i++) clearance = Mathf.Min(clearance, Mathf.Abs(x - targets[i]) - radius - RiderRadius);
                if (clearance < .2f) continue;
                float score = Mathf.Min(clearance, .7f) - Mathf.Abs(x - stage.player.lateral) * .2f;
                if (score > best) { best = score; SafeCenter = x; }
            }
        }

        void Update()
        {
            if (!stage.Playing)
            {
                // Keep the frozen warning visible during pause, hide it on failure/cinematics.
                if (stage.Session.mode != GameMode.Paused) HideVisuals();
                return;
            }
            var music = stage.audioSystem.music;
            if (music && music.clip && music.isPlaying)
            {
                int sample = music.timeSamples;
                if (lastSamples >= 0)
                {
                    int delta = sample - lastSamples;
                    if (delta < 0) delta += music.clip.samples;
                    clock += (float)delta / music.clip.frequency;
                }
                else clock = (float)sample / music.clip.frequency;
                lastSamples = sample;
            }
            else { clock += Time.deltaTime; lastSamples = -1; }

            if (stage.stageIndex == 2)
            {
                // Reserve the entire boss sequence, plus recovery time before its spawn.
                // This lets any optional dodge recharge before the mandatory missile response.
                bool room = stage.missile.state == MissileState.Despawn &&
                    stage.missile.nextSpawn - stage.elapsed > interval + stage.Settings.dodgeCooldown + .5f;
                if (!room) { Presenting = false; HideVisuals(); return; }
                if (!Presenting)
                {
                    Presenting = true;
                    strikeAt = BeatOffset + Mathf.Ceil((clock + interval - BeatOffset) / Beat) * Beat;
                    Plan();
                }
            }

            if (Attack == BeatAttack.Decoy && !resolved)
            {
                if (!TargetLocked) { targets[0] = stage.player.lateral; FindSafeResponse(); }
                Cue = TargetLocked ? "LOCKED! / 조준 확정! 지금 벗어나!" : "FAKE LOCK / 조준 중… 확정되면 벗어나!";
            }
            if (!resolved && clock >= strikeAt)
            {
                resolved = true;
                Resolved++;
                if (stage.player.dodge.Active || !Threatens(stage.player.lateral))
                {
                    Cleared++; Combo++;
                    stage.ShowFeedback("BREAK THE BEAT!  " + Combo + " COMBO", .65f);
                }
                else
                {
                    Combo = 0;
                    stage.playerHealth.TryDamage(JumpRequired ? "충격파! SPACE로 뛰어넘기" : "탄착점 피격!");
                }
            }
            if (resolved && clock >= strikeAt + ImpactDuration)
            {
                strikeAt += interval;
                if (strikeAt <= clock) strikeAt = BeatOffset + (Mathf.Floor((clock - BeatOffset) / Beat) + 4) * Beat;
                Plan();
            }
            Draw();
        }

        void HideVisuals()
        {
            foreach (var part in shots) if (part) part.gameObject.SetActive(false);
            foreach (var part in markers) if (part) part.gameObject.SetActive(false);
            foreach (var part in trails) if (part) part.gameObject.SetActive(false);
            if (safeStrip) safeStrip.gameObject.SetActive(false);
            if (shockwave) shockwave.gameObject.SetActive(false);
        }

        void Draw()
        {
            HideVisuals();
            float progress = Mathf.Clamp01((clock - plannedAt) / Mathf.Max(.01f, strikeAt - plannedAt));
            float travel = Mathf.Clamp01(1 - Countdown / (Beat * 2));
            float impact = Mathf.Clamp01((clock - strikeAt) / ImpactDuration);
            float distance = stage.player.distance;
            dangerMaterial.color = resolved ? new Color(1,.15f,.25f) : TargetLocked ? new Color(1,.6f,.08f) : new Color(1,.2f,.8f);
            shotMaterial.color = new Color(1,.24f,.58f);
            safeMaterial.color = new Color(.15f,1,.65f);
            if (JumpRequired)
            {
                Put(shockwave, distance + (1-travel)*45, 0, .45f,
                    new Vector3(stage.road.halfWidth*2,.7f,1.1f));
                return;
            }
            Put(safeStrip, distance + 1, SafeCenter, .06f, new Vector3(1.1f,.025f,2.5f));
            for (int i = 0; i < targetCount; i++)
            {
                float x = targets[i];
                // Ground circles remain fixed laterally after lock; only the approaching shot moves.
                Put(markers[i], distance, x, .08f, new Vector3(radius*2,.035f,radius*2));
                Vector3 end = stage.road.Position(distance,x,.6f);
                Vector3 point;
                if (Attack == BeatAttack.Meteor || Attack == BeatAttack.Decoy)
                    point = stage.road.Position(distance+(1-travel)*12,x, .6f+(1-travel*travel)*22);
                else if (Attack == BeatAttack.Orbit)
                {
                    float sign = i == 0 ? -1 : 1;
                    float angle = progress*Mathf.PI*4;
                    point = stage.road.Position(distance+(1-travel)*35,
                        x+Mathf.Sin(angle)*3*(1-travel), .6f+(1-travel)*(4+Mathf.Cos(angle)*3));
                }
                else
                {
                    float side = (i+Resolved/6)%2 == 0 ? -1 : 1;
                    point = stage.road.Position(distance+(1-travel)*30,
                        Mathf.Lerp(side*11,x,travel), .6f+(1-travel)*3);
                }
                shots[i].gameObject.SetActive(true);
                shots[i].position = point;
                shots[i].localScale = Vector3.one*(resolved ? radius*2*(1+impact) : .8f);
                // Visible trajectory communicates where the attack will land, not just a HUD label.
                Vector3 vector = end-point;
                if (vector.sqrMagnitude > .02f)
                {
                    trails[i].gameObject.SetActive(true);
                    trails[i].position = (point+end)*.5f;
                    trails[i].rotation = Quaternion.LookRotation(vector);
                    trails[i].localScale = new Vector3(.045f,.045f,vector.magnitude);
                }
            }
        }

        void Put(Transform target, float distance, float x, float height, Vector3 scale)
        {
            target.gameObject.SetActive(true);
            target.SetPositionAndRotation(stage.road.Position(distance,x,height),stage.road.Rotation(distance));
            target.localScale = scale;
        }

        void OnDestroy()
        {
            if (dangerMaterial) Destroy(dangerMaterial);
            if (safeMaterial) Destroy(safeMaterial);
            if (shotMaterial) Destroy(shotMaterial);
        }
    }
}
