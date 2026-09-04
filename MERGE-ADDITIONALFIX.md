# additionalfix integration review

## Branches and checkpoints

- User branch: `origin/feature/anomalies-batch-nghi`; original tip `ec0c9c8`.
- Saved all pending anomaly work before merging: `9a5ae4f`.
- Friend's incoming `additionalfix` tip: `5d8d6a7` (also includes `6eba1b0`).
- Merge checkpoint: `15ca32c`, with parents `5d8d6a7` and `9a5ae4f`.
- Final destination: local `additionalfix`, tracking remote `origin/additionalfix`.

The merge retains both histories. The temporary integration branch name is not the final destination.

## Preserved from the user's branch

| Feature | Retained behavior/assets |
| --- | --- |
| #11 trash | Realistic camera-facing bin and bags; correct normal/anomaly visibility swap |
| #15 following | Existing patrol first; encounter radius and line of sight; follow at 1.75 m/s |
| #17 appearance | Textured female civilian, bound walk/idle cycles and speed matching; still an appearance swap on the same NPC |
| #19 corpse | Waiting seated pose, generated blood decal, baked meshes and materials |
| #21 water | Water shader, ripple/outflow/audio infrastructure and shallow-depth limit; latest hallway seep tuning described below |
| #28 window faces | Authored anchor, multiple varied faces, window clearance and restoration |
| #34 cameras | Peripheral tracking, quick look-away when watched, gradual return; fixed mounts and animated heads |
| #35 classroom faces | Closed-room placement, varied tracking faces, falling/rolling heads and room collision barriers |
| Other work | Existing anomalies, player/VR setup, tester, sourced/generated artwork, editor setup and asset attribution |

## Preserved from additionalfix

| Feature | Retained behavior/assets |
| --- | --- |
| Elevator fixes | Cabin/trim placement adjustments, overhead clearance, sign/display object and supporting geometry |
| Elevator floor indicator | `ElevatorDisplay` component, blinking floor number and LED font resources |
| School logo | Friend's adjusted logo position |
| Dialogue | Dialog managers, views, nodes, triggers and Opening/Mid/End dialogue assets |
| Cutscenes | Managers, camera/player movement data, triggers, Starter/Mid/End assets |
| Game flow | Start/reset at floor 8, current-floor accessor, ending transition |
| UI and scenes | Sample, MainMenu, Intro and End scenes; wake-up fade in FirstPerson; build-scene list |
| Supporting resources | Font/material/UI imports and XR settings from the merged histories |

## Conflict decisions

`Floor.prefab` was merged by serialized object ID rather than choosing one entire file. The resolved merge applied 21 non-anomaly object additions/edits from the friend and retained the user's latest anomaly subtrees. Every internal nonzero fileID reference resolved afterward.

The friend's branch recreated camera/trash/corpse/window model instances using new IDs. Those were older copies of the same anomaly structures, not new independent features. The newer user implementations take precedence. Camera placements in the recreated copies match the common ancestor, so no distinct camera-placement change was discarded.

`FirstPerson.unity` combines the user's scene/anomaly changes with the friend's wake-up canvas and fade. Both scene and prefab were explicitly inspected after merging.

One empty camera/head pair was already present in the saved user checkpoint (the previously deleted bin-peeking camera). The final cleanup removes that stale slot without restoring an intentionally deleted object. The remaining 14 configured cameras are checked for valid paired head references.

## Latest requested changes after the merge checkpoint

- Normal NPC: male teacher in a blue collared shirt and dark trousers, a fuller waist and modest rounded belly. The existing NPC root, collider, navigation, patrol/follow/chase behaviors remain in use. Male walking/breathing clips drive the new skeleton; cadence follows actual speed. #17 temporarily hides these teacher renderers and restores them afterward.
- #21: shallow water starts after 8 seconds, advances at 0.14 m/s, travels through the restroom opening and spreads into the hall. The front reaches the opening around 37 seconds; by 65 seconds it has spread several metres along the hall. Spatial noise gives the advancing edge irregular fingers. The puddle stays shallow: default 3.5 cm rise, hard cap 4 cm, no drowning/submersion code.
- Ending fix: once floor zero loads End, stop the query/update before code can index floor -1. This preserves and completes the friend's intended ending transition.

## Verification

Unity validation completed successfully. Normal teacher patrol: 1.750027 m/s, knee rotation excursion 56.16 degrees. Female #17 patrol: 1.750055 m/s, knee excursion 77.82 degrees. Both animation clocks advanced; teacher/female visibility restored correctly. Navigation remained on the NavMesh with an active path. Twelve runtime elevator-display components were present.

Prefab checks passed for registered anomaly references, visibility restoration, trash swap, window anchor/face count, water shader/references, closed-room face containment, and model previews. Water reached the exit by 40 seconds and advanced about 4 m beyond it by 65 seconds; depth remained capped at 3.5 cm at 600 seconds. The serialized prefab also has no dangling nonzero internal fileID references.

Reports are saved under `Documentation/Validation/`. Visual previews are `Documentation/Previews/Teacher.png` and `Documentation/Previews/HallLeak65seconds.png`. Full opening-to-ending gameplay, standalone/VR builds, and subjective in-headset visual review were not run. The ending guard was reviewed in code; the existing dialogue/cutscene/UI files were preserved, not rewritten.

The file audit below refers specifically to merge checkpoint `15ca32c`, before the requested teacher/hallway improvements listed above. Unity reserialization makes the final prefab text diff large; the object/reference checks verify its combined contents.

## Exact file preservation at the merge checkpoint

140 user-changed files and 76 friend-changed files were byte-for-byte retained at the merge checkpoint. The only combined files from either change set were `Assets/Prefabs/Floor.prefab` and `Assets/Scenes/FirstPerson.unity`. No changed file from either branch was missing.

### User branch — exact retained files

- `Assets/Art/Characters/Ghost3D/M_Ghost_A.mat`
- `Assets/Art/Characters/Ghost3D/M_Ghost_B.mat`
- `Assets/Art/NghiAnomalies/ASSET_SOURCES.md`
- `Assets/Art/NghiAnomalies/CameraHead.asset`
- `Assets/Art/NghiAnomalies/CameraHead.asset.meta`
- `Assets/Art/NghiAnomalies/CameraMount.asset`
- `Assets/Art/NghiAnomalies/CameraMount.asset.meta`
- `Assets/Art/NghiAnomalies/CorpseDeathPose_Formad_Head.asset`
- `Assets/Art/NghiAnomalies/CorpseDeathPose_Formad_Head.asset.meta`
- `Assets/Art/NghiAnomalies/CorpseDeathPose_Formal_Body.asset`
- `Assets/Art/NghiAnomalies/CorpseDeathPose_Formal_Body.asset.meta`
- `Assets/Art/NghiAnomalies/CorpseDeathPose_Formal_Feet.asset`
- `Assets/Art/NghiAnomalies/CorpseDeathPose_Formal_Feet.asset.meta`
- `Assets/Art/NghiAnomalies/CorpseDeathPose_Formal_Legs.asset`
- `Assets/Art/NghiAnomalies/CorpseDeathPose_Formal_Legs.asset.meta`
- `Assets/Art/NghiAnomalies/CorpseRelaxedPose_Formad_Head.asset`
- `Assets/Art/NghiAnomalies/CorpseRelaxedPose_Formad_Head.asset.meta`
- `Assets/Art/NghiAnomalies/CorpseRelaxedPose_Formal_Body.asset`
- `Assets/Art/NghiAnomalies/CorpseRelaxedPose_Formal_Body.asset.meta`
- `Assets/Art/NghiAnomalies/CorpseRelaxedPose_Formal_Feet.asset`
- `Assets/Art/NghiAnomalies/CorpseRelaxedPose_Formal_Feet.asset.meta`
- `Assets/Art/NghiAnomalies/CorpseRelaxedPose_Formal_Legs.asset`
- `Assets/Art/NghiAnomalies/CorpseRelaxedPose_Formal_Legs.asset.meta`
- `Assets/Art/NghiAnomalies/FloodGrid.asset`
- `Assets/Art/NghiAnomalies/FloodGrid.asset.meta`
- `Assets/Art/NghiAnomalies/FloodRoar.wav`
- `Assets/Art/NghiAnomalies/FloodRoar.wav.meta`
- `Assets/Art/NghiAnomalies/M_Nghi_Blood.mat`
- `Assets/Art/NghiAnomalies/M_Nghi_CardboardPhoto.mat`
- `Assets/Art/NghiAnomalies/M_Nghi_FloodWaves.mat`
- `Assets/Art/NghiAnomalies/M_Nghi_FloodWaves.mat.meta`
- `Assets/Art/NghiAnomalies/M_Nghi_Foam.mat`
- `Assets/Art/NghiAnomalies/M_Nghi_Foam.mat.meta`
- `Assets/Art/NghiAnomalies/M_Nghi_RealisticBin.mat`
- `Assets/Art/NghiAnomalies/M_Nghi_RealisticBin.mat.meta`
- `Assets/Art/NghiAnomalies/M_Nghi_StudentShirt.mat`
- `Assets/Art/NghiAnomalies/M_Nghi_StudentShirt.mat.meta`
- `Assets/Art/NghiAnomalies/M_Nghi_StudentSkin.mat`
- `Assets/Art/NghiAnomalies/M_Nghi_StudentSkin.mat.meta`
- `Assets/Art/NghiAnomalies/M_Nghi_StudentUniform.mat`
- `Assets/Art/NghiAnomalies/M_Nghi_StudentUniform.mat.meta`
- `Assets/Art/NghiAnomalies/M_Nghi_Underwater.mat`
- `Assets/Art/NghiAnomalies/M_Nghi_Underwater.mat.meta`
- `Assets/Art/NghiAnomalies/M_Nghi_WaitingStool.mat`
- `Assets/Art/NghiAnomalies/M_Nghi_WaitingStool.mat.meta`
- `Assets/Art/NghiAnomalies/M_Nghi_Water.mat`
- `Assets/Art/NghiAnomalies/NPCWoman_Idle.anim`
- `Assets/Art/NghiAnomalies/NPCWoman_Idle.anim.meta`
- `Assets/Art/NghiAnomalies/NPCWoman_Walk.anim`
- `Assets/Art/NghiAnomalies/NPCWoman_Walk.anim.meta`
- `Assets/Art/NghiAnomalies/OnlineModels/NPCWoman.glb`
- `Assets/Art/NghiAnomalies/OnlineModels/NPCWoman.glb.meta`
- `Assets/Art/NghiAnomalies/OnlineModels/SchoolTrashcan_Cardboard.png.meta`
- `Assets/Art/NghiAnomalies/OnlineModels/SchoolTrashcan_Realistic.png`
- `Assets/Art/NghiAnomalies/OnlineModels/SchoolTrashcan_Realistic.png.meta`
- `Assets/Art/NghiAnomalies/Rocketbox.meta`
- `Assets/Art/NghiAnomalies/Rocketbox/Female_Adult_13.fbx`
- `Assets/Art/NghiAnomalies/Rocketbox/Female_Adult_13.fbx.meta`
- `Assets/Art/NghiAnomalies/Rocketbox/Idle.anim`
- `Assets/Art/NghiAnomalies/Rocketbox/Idle.anim.meta`
- `Assets/Art/NghiAnomalies/Rocketbox/LICENSE.md`
- `Assets/Art/NghiAnomalies/Rocketbox/LICENSE.md.meta`
- `Assets/Art/NghiAnomalies/Rocketbox/Walk.anim`
- `Assets/Art/NghiAnomalies/Rocketbox/Walk.anim.meta`
- `Assets/Art/NghiAnomalies/Rocketbox/body.mat`
- `Assets/Art/NghiAnomalies/Rocketbox/body.mat.meta`
- `Assets/Art/NghiAnomalies/Rocketbox/f019_body_color.tga`
- `Assets/Art/NghiAnomalies/Rocketbox/f019_body_color.tga.meta`
- `Assets/Art/NghiAnomalies/Rocketbox/f019_body_normal.tga`
- `Assets/Art/NghiAnomalies/Rocketbox/f019_body_normal.tga.meta`
- `Assets/Art/NghiAnomalies/Rocketbox/f019_head_color.tga`
- `Assets/Art/NghiAnomalies/Rocketbox/f019_head_color.tga.meta`
- `Assets/Art/NghiAnomalies/Rocketbox/f019_head_normal.tga`
- `Assets/Art/NghiAnomalies/Rocketbox/f019_head_normal.tga.meta`
- `Assets/Art/NghiAnomalies/Rocketbox/f019_opacity_color.tga`
- `Assets/Art/NghiAnomalies/Rocketbox/f019_opacity_color.tga.meta`
- `Assets/Art/NghiAnomalies/Rocketbox/f_idle_breathe_01.max.fbx`
- `Assets/Art/NghiAnomalies/Rocketbox/f_idle_breathe_01.max.fbx.meta`
- `Assets/Art/NghiAnomalies/Rocketbox/f_walk_neutral.max.fbx`
- `Assets/Art/NghiAnomalies/Rocketbox/f_walk_neutral.max.fbx.meta`
- `Assets/Art/NghiAnomalies/Rocketbox/head.mat`
- `Assets/Art/NghiAnomalies/Rocketbox/head.mat.meta`
- `Assets/Art/NghiAnomalies/Rocketbox/opacity.mat`
- `Assets/Art/NghiAnomalies/Rocketbox/opacity.mat.meta`
- `Assets/Art/NghiAnomalies/Shaders.meta`
- `Assets/Art/NghiAnomalies/Shaders/BillboardCutout.shader`
- `Assets/Art/NghiAnomalies/Shaders/BillboardCutout.shader.meta`
- `Assets/Art/NghiAnomalies/Shaders/Flood.shader`
- `Assets/Art/NghiAnomalies/Shaders/Flood.shader.meta`
- `Assets/Art/NghiAnomalies/Shaders/Splash.shader`
- `Assets/Art/NghiAnomalies/Shaders/Splash.shader.meta`
- `Assets/Art/NghiAnomalies/Shaders/Underwater.shader`
- `Assets/Art/NghiAnomalies/Shaders/Underwater.shader.meta`
- `Assets/Art/NghiAnomalies/T_Nghi_BloodOrganic.png`
- `Assets/Art/NghiAnomalies/T_Nghi_BloodOrganic.png.meta`
- `Assets/Art/NghiAnomalies/WaitingStudent_Formad_Head.asset`
- `Assets/Art/NghiAnomalies/WaitingStudent_Formad_Head.asset.meta`
- `Assets/Art/NghiAnomalies/WaitingStudent_Formal_Body.asset`
- `Assets/Art/NghiAnomalies/WaitingStudent_Formal_Body.asset.meta`
- `Assets/Art/NghiAnomalies/WaitingStudent_Formal_Feet.asset`
- `Assets/Art/NghiAnomalies/WaitingStudent_Formal_Feet.asset.meta`
- `Assets/Art/NghiAnomalies/WaitingStudent_Formal_Legs.asset`
- `Assets/Art/NghiAnomalies/WaitingStudent_Formal_Legs.asset.meta`
- `Assets/Editor/AnomolySetupNghi.cs`
- `Assets/Editor/NghiAnomalyRefinement.cs`
- `Assets/Editor/NghiAnomalyRefinement.cs.meta`
- `Assets/Editor/NghiCameraPlacement.cs`
- `Assets/Editor/NghiCameraPlacement.cs.meta`
- `Assets/Editor/NghiExtensionSetup.cs`
- `Assets/Editor/NghiExtensionSetup.cs.meta`
- `Assets/Editor/NghiFacePlacement.cs`
- `Assets/Editor/NghiFacePlacement.cs.meta`
- `Assets/Editor/NghiMotionProbe.cs`
- `Assets/Editor/NghiMotionProbe.cs.meta`
- `Assets/Editor/NghiPosterPlacement.cs`
- `Assets/Editor/NghiPosterPlacement.cs.meta`
- `Assets/Editor/NghiReadOnlyAudit.cs`
- `Assets/Editor/NghiReadOnlyAudit.cs.meta`
- `Assets/Editor/NghiRefinementValidation.cs`
- `Assets/Editor/NghiRefinementValidation.cs.meta`
- `Assets/Editor/NghiRocketboxImport.cs`
- `Assets/Editor/NghiRocketboxImport.cs.meta`
- `Assets/Editor/NghiSubtleAnomalies.cs`
- `Assets/Editor/NghiSubtleAnomalies.cs.meta`
- `Assets/Editor/NghiWalkingPlayCheck.cs`
- `Assets/Editor/NghiWalkingPlayCheck.cs.meta`
- `Assets/Editor/Tests/NghiAnomalyTests.cs`
- `Assets/Scripts/Game/Anomolies/AnomalyBillboard.cs`
- `Assets/Scripts/Game/Anomolies/AnomalyBillboard.cs.meta`
- `Assets/Scripts/Game/Anomolies/AnomalyFaceVariation.cs`
- `Assets/Scripts/Game/Anomolies/AnomalyFaceVariation.cs.meta`
- `Assets/Scripts/Game/Anomolies/Anomoly15.cs`
- `Assets/Scripts/Game/Anomolies/Anomoly17.cs`
- `Assets/Scripts/Game/Anomolies/Anomoly17.cs.meta`
- `Assets/Scripts/Game/Anomolies/Anomoly21.cs`
- `Assets/Scripts/Game/Anomolies/Anomoly28.cs`
- `Assets/Scripts/Game/Anomolies/Anomoly34.cs`
- `Assets/Scripts/Game/Anomolies/Anomoly35.cs`
- `Assets/Scripts/Game/Anomolies/Anomoly35.cs.meta`
- `Assets/Scripts/Game/Mechanic/AnomolyTester.cs`

### additionalfix — exact retained files

- `Assets/Art/Characters/Ghost3D/M_GhostEyes.mat`
- `Assets/Art/Characters/Ghost3D/M_Ghost_A.mat`
- `Assets/Art/Characters/Ghost3D/M_Ghost_B.mat`
- `Assets/Art/Normal.meta`
- `Assets/Art/Normal/McCain LED Pedestrian Signals.fbx`
- `Assets/Art/Normal/McCain LED Pedestrian Signals.fbx.meta`
- `Assets/Art/UI/temp_setting_background.png.meta`
- `Assets/CutScene.meta`
- `Assets/CutScene/End.asset`
- `Assets/CutScene/End.asset.meta`
- `Assets/CutScene/Mid.asset`
- `Assets/CutScene/Mid.asset.meta`
- `Assets/CutScene/Starter.asset`
- `Assets/CutScene/Starter.asset.meta`
- `Assets/Dialog.meta`
- `Assets/Dialog/End.asset`
- `Assets/Dialog/End.asset.meta`
- `Assets/Dialog/Mid.asset`
- `Assets/Dialog/Mid.asset.meta`
- `Assets/Dialog/Opening.asset`
- `Assets/Dialog/Opening.asset.meta`
- `Assets/Font/led-counter-7.meta`
- `Assets/Font/led-counter-7/led_counter-7 SDF.asset`
- `Assets/Font/led-counter-7/led_counter-7 SDF.asset.meta`
- `Assets/Font/led-counter-7/led_counter-7.ttf`
- `Assets/Font/led-counter-7/led_counter-7.ttf.meta`
- `Assets/Font/led-counter-7/led_counter-7_italic SDF.asset`
- `Assets/Font/led-counter-7/led_counter-7_italic SDF.asset.meta`
- `Assets/Font/led-counter-7/led_counter-7_italic.ttf`
- `Assets/Font/led-counter-7/led_counter-7_italic.ttf.meta`
- `Assets/Font/led-counter-7/led_counter-7_screen.gif`
- `Assets/Font/led-counter-7/led_counter-7_screen.gif.meta`
- `Assets/Font/led-counter-7/readme.txt`
- `Assets/Font/led-counter-7/readme.txt.meta`
- `Assets/Font/paint-font/Paint-JpZDE SDF.asset`
- `Assets/Prefabs/UI/Cube.prefab`
- `Assets/Prefabs/UI/Cube.prefab.meta`
- `Assets/Scenes/UI/End.unity`
- `Assets/Scenes/UI/End.unity.meta`
- `Assets/Scenes/UI/Intro.unity`
- `Assets/Scenes/UI/Intro.unity.meta`
- `Assets/Scenes/UI/MainMenu.unity`
- `Assets/Scenes/UI/MainMenu.unity.meta`
- `Assets/Scenes/UI/Sample.unity`
- `Assets/Scenes/UI/Sample.unity.meta`
- `Assets/Scripts/CutScene.meta`
- `Assets/Scripts/CutScene/CutHitBox.cs`
- `Assets/Scripts/CutScene/CutHitBox.cs.meta`
- `Assets/Scripts/CutScene/CutSceneInfo.cs`
- `Assets/Scripts/CutScene/CutSceneInfo.cs.meta`
- `Assets/Scripts/CutScene/CutSceneManager.cs`
- `Assets/Scripts/CutScene/CutSceneManager.cs.meta`
- `Assets/Scripts/CutScene/CutTrigger.cs`
- `Assets/Scripts/CutScene/CutTrigger.cs.meta`
- `Assets/Scripts/Dialog.meta`
- `Assets/Scripts/Dialog/CutSceneDialogManager.cs`
- `Assets/Scripts/Dialog/CutSceneDialogManager.cs.meta`
- `Assets/Scripts/Dialog/DialogManager.cs`
- `Assets/Scripts/Dialog/DialogManager.cs.meta`
- `Assets/Scripts/Dialog/DialogNode.cs`
- `Assets/Scripts/Dialog/DialogNode.cs.meta`
- `Assets/Scripts/Dialog/DialogTester.cs`
- `Assets/Scripts/Dialog/DialogTester.cs.meta`
- `Assets/Scripts/Dialog/DialogTrigger.cs`
- `Assets/Scripts/Dialog/DialogTrigger.cs.meta`
- `Assets/Scripts/Dialog/DialogView.cs`
- `Assets/Scripts/Dialog/DialogView.cs.meta`
- `Assets/Scripts/Effect.meta`
- `Assets/Scripts/Effect/WakeUpEffec.cs`
- `Assets/Scripts/Effect/WakeUpEffec.cs.meta`
- `Assets/Scripts/Game/GameManager.cs`
- `Assets/Scripts/Interaction/ElevatorDisplay.cs`
- `Assets/Scripts/Interaction/ElevatorDisplay.cs.meta`
- `Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset`
- `Assets/XR/Settings/OpenXR Package Settings.asset`
- `ProjectSettings/EditorBuildSettings.asset`
