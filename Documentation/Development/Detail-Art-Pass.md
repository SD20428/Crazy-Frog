# Runtime detail art pass

DetailArtPass adds cosmetic child meshes to the rider, bike, police vehicle, chase missile
and cinematic missile in all five scenes. StageController attaches it alongside NeonArtDirection.
Original meshes, prefabs, colliders, attack rules and Timeline transforms are untouched.

Rider: goggles hardware, cheek spots, gloves/stitching, boot buckles/soles, backpack zipper/patch.
Bike: dashboard, brake levers/discs/calipers, radiator louvers, fasteners, rear grab rails/plate.
Police: bumper guard, door handles, window dividers, wheel bolts, hood intakes and rear lights.
Missiles: engine collars, thruster ring, stabilizer trim, telemetry bands and sensor lens.

Twelve recycled roadside modules provide city advertisements and utility cabinets, sewer
instrument panels/pressure gauges/valves, or finale crash barriers/solar-powered route beacons.
Geometry is merged by material within each group; owned materials and meshes are destroyed
with the stage. New parts have no active colliders. Rider parts follow Rider, vehicle parts
follow Visual, and missile parts follow Model, including cinematic reparenting.

Validation: compilation succeeded; all three stages were loaded and photographed; rider,
police and missile close-ups were inspected. Ending scene reported all four detail roots.
The first run after compilation had a missing GameManager static reference despite an existing
manager object, producing errors in GameAudio/PoliceAI/RiderPresentation. A fresh PlayMode run
restored the session and all later scene checks had zero current Console errors/warnings.
The original cause was not established; historical Pipeline errors remain as evidence.

This is an additive stylized geometry pass, not replacement high-resolution character art.
Long-duration gameplay, low-end hardware profiling and complete cinematic clipping checks
remain unverified. No commit or push was performed.
