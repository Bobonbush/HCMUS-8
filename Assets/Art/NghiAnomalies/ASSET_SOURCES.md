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
