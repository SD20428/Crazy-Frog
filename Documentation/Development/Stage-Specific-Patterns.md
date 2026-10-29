# Movement course revision (2026-10-30 KST)

Supersedes the previous instant beat/lateral-position attack system and its old probe results.
The supplied Google Drive folder could not be opened by the reference tool; this is an
implementation of the requested combined movement, not a claimed recreation of that video.

## Controls and contact

A/D or Left/Right: lateral steering. W/S or Up/Down: lift/dive, with acceleration and
a 0..4.8 road-relative altitude limit. Releasing vertical input settles into a hover.
SPACE: existing short hop, now included in the collision body's actual height.
It does not grant immunity to course solids. Ordinary post-hit health invulnerability remains.
The camera follows altitude. Existing outdoor overhead signs are lifted at runtime for clearance.

Every red outlined solid is a real analytic road-coordinate collision volume.
Sweeps cover the space travelled between frames, including obstacle motion and sampled rotation.
The body is a conservative 1.1 x 2.2 x 2.2 box; rotating solids use its projected extents.
Green frames outline openings, not invulnerability zones. Deep openings require holding a
position until the whole body has left. Collision marks that station failed even during health
invulnerability, so immunity cannot farm boss progress. Geometry is pooled (12 solids).

## Stage differences

- City: alternating high-left / low-right apertures; climb over full-width barriers then
  dive below signs; moving apertures travelling horizontally and vertically.
- Sewer: long low / high siphon gates; changing-height water columns with a low ceiling
  that forces a sideways route; deep offset tunnels with lateral current to countersteer.
- Police boss: actual rotating bars; position-tracking ram boxes that lock with 1.7 seconds
  of nominal approach remaining and then travel toward the rider, with floor/ceiling escorts;
  vertically oscillating scissor openings with alternating lateral offsets.
  Twelve clean stations deplete armor and request the existing Ending scene.

Axel F and its existing audio setup remain. Station spacing derives from the configured BPM:
four beats at NORMAL nominal speed, five at EASY. Moving geometry uses the same tempo.
This is tempo-based course spacing, not a claim of sample-accurate contact with the recording:
slowing after damage and approaching ram motion change the time of actual contact.

## Actual verification

- Unity 6000.3.23f1 compiled the gameplay revision without errors.
- Nine swept-volume boundary cases passed in the running Editor: direct contact, lateral
  clearance, vertical clearance, body-height overlap, fast crossing, no premature contact,
  contact inside a deep obstacle, rotated-bar contact and rotated-bar clearance.
- NORMAL live movement probe, real Step(horizontal, vertical, deltaTime), no waypoint teleport:
  city 9/9 clean with 0 damage; sewer 9/9 clean with 0 damage; boss 7/9 clean with 2 damage.
  All three variants per stage were exercised. Player altitude and lateral position both varied.
  The probe used 2x simulation time and temporary health=100 to record collisions without ending
  the run. This is not a human balance test or a claim of a flawless boss run.
- Deliberate solid overlap with SPACE active caused one damage event and a failed station.
- Live frames advanced. Current Unity Console ground truth had 0 errors and 0 warnings;
  the Pipeline history still contained 2000 old errors, which is not a current clean history.
- Evidence: ValidationResults/MovementCourse-20261029T163152661Z-c6cebf/runtime-results.json.
  Captures are diagnostic, including a paused probe screen; not all show final framing.
- EASY full run, new full boss-to-ending flow, music phase alignment, and human difficulty
  balance remain unverified. The old boss-ending probe is not evidence for this revision.
