# Nghi anomaly asset sources

Retrieved 2026-09-03 for non-commercial university coursework.

## Imported models

- `OnlineModels/SchoolTrashcan.glb` — **Trashcan Small**, Quaternius, CC0.
  Source: https://poly.pizza/m/i7HDuYDLkx
- `OnlineModels/CCTV_Camera.glb` — **Security Camera**, J-Toastie, CC BY.
  Source: https://poly.pizza/m/a6J7IDufQP
- `OnlineModels/WindowHead.glb` — **HeadRef**, Christian Venables, CC BY.
  Source: https://poly.pizza/m/9c-7mribNvi

## Transparent 2D bin cutout

- `OnlineModels/SchoolTrashcan_Cardboard.png` is a deliberately pixelated 96×128
  transparent cutout derived from the preview of **Trashcan Small**, Quaternius,
  CC0. The purple preview background was removed and the neutral model was tinted
  green to read as an eerie school-bin silhouette in the 3D corridor.
  Source: https://poly.pizza/m/i7HDuYDLkx

## Existing project model reused

- `OnlineModels/NPCWoman.glb` — **Animated Woman**, Quaternius, CC0.
  Source: https://poly.pizza/m/nIItLV9nxS
  The idle/walk clips support #17. The final Death-animation frame is baked to
  `CorpseDeathPose_*.asset` for #19, replacing the upright/T-pose zombie representation.

- `Assets/Art/Characters/Zombie.obj` and its existing `M_Zombie` material are
  reused for the toilet corpse. This avoids introducing a mismatched cartoon model
  and keeps the anomaly consistent with the project's established horror assets.

The PCCC poster source remains documented separately in `PCCC_Texture_Source.md`.

- T_Nghi_BloodOrganic.png: AI-generated transparent blood-pool decal created with OpenAI image generation for anomaly #19. Used by M_Nghi_Blood on the BloodStain quad.

## Refinement assets (2026-09-04)

- `OnlineModels/SchoolTrashcan_Realistic.png`: generated with the built-in OpenAI image tool. Transparent photographic cutout, not a photograph obtained from a third-party catalogue. Prompt: "Create a photorealistic game sprite on genuinely transparent background: one battered green institutional school rubbish bin, full object visible front three-quarter view, dirty realistic plastic, overflowing crumpled refuse, two tied black trash bags on floor immediately beside bin. Uncanny lifelike photographic detail, neutral dim lighting without cast background, isolated alpha cutout, no text, no logos, no border. Entire group centered with margin, suitable for a camera-facing billboard in a horror corridor."
- `CameraHead.asset` / `CameraMount.asset`: separated housing and bracket from the existing J-Toastie CC BY security camera above; retain that attribution.
- `WaitingStudent_*.asset`: seated pose baked from the existing Quaternius CC0 Animated Woman rig; muted shirt and dark trousers. This remains a low-poly model, not a newly acquired photorealistic student scan.
- `FloodRoar.wav`: original procedural noise/surge audio generated locally for this project; no third-party recording.
- Flood, underwater and billboard shaders are project-authored. Face expression variation deforms individual copies of the existing attributed HeadRef mesh without modifying its source.

## Anomaly 17 replacement (2026-09-04)

- `Rocketbox/Female_Adult_13.fbx` and `f019_*` textures: Microsoft Rocketbox Avatar Library, Female Adult 13, MIT license. A gray vest, black shirt, jeans and boots replace the earlier green-dress visual.
- `Rocketbox/f_walk_neutral.max.fbx` and `f_idle_breathe_01.max.fbx`: matching Rocketbox biped motions, MIT. `Walk.anim` and `Idle.anim` adapt matching bone rotations and vertical body motion to this avatar; horizontal travel remains controlled by the existing NPC NavMeshAgent. Animation cadence follows measured travel speed.
- Source: https://github.com/microsoft/Microsoft-Rocketbox
- License: `Rocketbox/LICENSE.md`. Retrieved 2026-09-04. The original model and texture assets are retained alongside the derived animation clips.
- The Quaternius model remains in the project for the corpse assets; it is no longer the #17 appearance.

## Slow leak revision (2026-09-04)

Anomaly 21 now begins after 18 seconds. One bowl leaks into a local puddle, spreading at 0.028 m/s up to a 4.5 m radius. Depth approaches 0.035 m over 240 seconds and is hard-capped at 0.04 m in runtime code. Ripples are millimetres high, particles are small and sparse, and audio is quiet and audible only nearby. Underwater distortion, muffling and submersion logic were removed. Existing `Underwater` assets are unused by this anomaly.

## Main teacher and hallway seep (2026-09-04)

- `Rocketbox/Teacher/Male_Adult_08.fbx`, `m014_*` textures, `m_walk_neutral.max.fbx`, and `m_idle_breathe_01.max.fbx` come from the Microsoft Rocketbox Avatar Library under MIT. Source: https://github.com/microsoft/Microsoft-Rocketbox. The license is retained in `Rocketbox/LICENSE.md`.
- `TeacherBuild.asset` is derived from that avatar with a modestly fuller waist and rounded abdomen. Clothing remains a light blue collared shirt, dark trousers and ordinary shoes. `Teacher/Idle.anim` and `Teacher/Walk.anim` use the matching male biped rotations, with navigation-controlled translation and speed-matched cadence.
- The normal NPC keeps its original navigation/collider/root references. The robot renderers are disabled; anomaly #17 swaps only the teacher renderers for the existing female appearance and restores them afterward.
- This hallway seep revision supersedes the earlier slow-leak timing above: onset 8 seconds, flow-front travel 0.14 m/s, maximum travel 12 m, default rise 3.5 cm over 150 seconds. The flow routes through an authored restroom exit before widening along the hallway; the advancing edge uses layered spatial noise. Hall geometry limits the wet footprint. There is still no submersion or drowning logic.
