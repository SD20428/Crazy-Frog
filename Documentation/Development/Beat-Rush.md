# Beat Rush: attack remix

Stage 1 replaces the traffic rows at runtime with six attacks. Scene and prefab files,
vehicle controls, stages 2/3 and MCP settings are unchanged. A/D steers, SPACE dodges.

| Attack | Threat | Response |
| --- | --- | --- |
| Meteor | A falling projectile targets the position at warning start | Leave the marked blast circle |
| Crossfire | Three diagonal shots enter from outside the road | Stand between the marked landing points |
| Shockwave | A low wave spans the whole road | Time SPACE; lateral movement cannot escape it |
| Freeze | Two shots bracket the current position | Hold position while they pass on either side |
| Decoy | A pink aiming circle follows briefly, then locks orange | Leave after lock; no last-second retargeting |
| Orbit | Two missiles spiral into separated landing points | Move between their landing zones |

All six can also use the existing dodge when it is ready. Each attack is adjudicated once
at impact using its drawn landing radius plus the rider radius. Flights are choreographed
visual trajectories, not Rigidbody collisions; they cannot hit early during their warning.
The green marker suggests one safe response but is not the only safe position.
Pooled runtime objects and materials are discarded with the stage. No asset writes at runtime.

NORMAL attacks every four beats (about 1.74s), EASY every five (about 2.17s), following
at least four seconds of introduction. Mandatory shockwaves are separated by six attacks,
so the original 2.2s dodge cooldown is sufficient. Attack time follows music samples;
pausing freezes music, warnings and the countdown. Later cycles mirror launch sides.

Audio: Assets/FrogEscape/Audio/CrazyFrog_AxelF.mp3, supplied by the user, 171.833s,
44.1kHz stereo. Original root MP3 and Original_Pulse.wav remain untouched. GameConfig
uses 138 BPM and an estimated 0.20s beat offset from PCM onset analysis, not a manually
mapped music chart. Volume is 0.42 normally, 0.28 under stage 2/3 attack warnings.

## Actual verification

Unity 6000.3.23f1 compilation succeeded. A temporary runtime probe called BikeController.Step
with bounded steering and the existing dodge cooldown. NORMAL cleared all six attacks twice
(12/12), then intentionally stayed in a meteor landing circle: one hit, health 3 -> 2.
Actual music was CrazyFrog_AxelF. Live frames advanced; the probe paused afterwards, then
Play mode was stopped. Console ground truth: zero errors and zero warnings. The Pipeline
history still includes an older 'already in play mode' warning from music integration.

Evidence: ValidationResults/AttackRemix-20261028T165223672Z/live-probe.json and capture-assets/.
This establishes baseline reachability and a positive damage check, not human difficulty
balance. EASY, full-game regression, every attack's negative case and fine listening alignment
remain unverified. No new automation system, commit or push was performed.

## Stage 2 and 3 extension

Stage 2 now runs a sewer-specific sequence: Crossfire, Decoy, Freeze, Crossfire, Meteor,
Shockwave. The old PoliceAttackController scheduler is disabled only on the runtime stage
instance; police pursuit and the serialized scene remain intact. This prevents simultaneous
old gunfire and beat attacks.

Stage 3 runs Orbit, Crossfire, Decoy and Meteor between boss sequences. It reserves the whole
missile Spawn/Pursue/LockOn/Warning/Dash/Hit/Miss sequence and a pre-spawn cooldown buffer.
Cancelled telegraphs do not count as hits or clears. On resumption, a new full warning is
scheduled on the music beat grid. Existing missile waves, dodge windows and ending are retained.
