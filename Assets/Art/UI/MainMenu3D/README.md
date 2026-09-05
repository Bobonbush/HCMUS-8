# Main menu presentation

The main menu uses the complete `Assets/Prefabs/Floor.prefab` instance, with its authored meshes, materials, lights and visual component settings. Gameplay/anomaly scripts are removed **from the menu instance only**, so their `Awake` methods cannot open the doors or start anomalies. The original prefab is unchanged.

The phone display, transparent PLAY? lettering, title, and hands are serialized in `MainMenu.unity`. Runtime code only binds and animates them, so their complete hierarchy is visible and editable before entering Play Mode. The installer can rebuild that editable hierarchy from the full prefab.

- A newly downloaded, textured 21-bone anatomical hand model replaces the old static OBJ. One mirrored support hand grips the phone; a separate pointer hand keeps its index extended and animates the three real index joints with short, bounded rotations. The pointer does not support or stretch with the phone.
- The booking screen shows a cropped route map, driver arrival status, **Options / Game settings**, and **Cancel booking / Quit game**. PLAY? appears only on the screen-right elevator. HCMUS8 appears in the upper-left corner.
- Mouse movement into the upper screen lowers the phone; movement into the lower screen raises it into the center. Hovering/clicking the phone does not move it away from the pointer.
- The existing `UI/Navigate` action cycles **Play → Settings → Quit**, wrapping in either direction. Keyboard arrows, D-pad and stick bindings work independently of the glance. `UI/Submit` confirms the selected action; mouse hover updates that same selection.
- Options opens through the existing backend, with an animated projection from the phone, cyan aura and the project's existing `PauseBlur` effect. Its settings controls/backend remain unchanged.
- Cancel booking taps the screen, switches the phone off, enters the unmarked elevator and fades out before calling the existing Quit backend.
- The lobby elevator LEDs display floor 11. Play switches the phone off, opens the marked elevator's original inner and outer doors, enters and closes them, descends 11 → 10 → 9 → 8, then flickers/shakes/stops before calling the existing Play backend. That backend already targets the Intro scene; its text and scene logic are unchanged. Shake intensity respects the saved camera-shake setting.

## References and assets

- Phone UI reference: [Grab's illustrated booking flow](https://engineering.grab.com/poi-entrances-venues-door-to-door). The menu uses its map/status/action hierarchy, with less detail for a readable world-space screen. The route is an illustrative reused project asset, not live tracking.
- Phone layout reference: [Apple's iPhone personalization guide](https://support.apple.com/en-ie/guide/iphone/iphefb3daa42/ios), for restrained status information and separated controls.
- Lettering: [Creepster, Google Fonts](https://fonts.google.com/specimen/Creepster), Copyright Font Diner, SIL Open Font License. The unmodified font and full license are in `Fonts/`. PLAY? is rendered as text with a soft underlay and faint depth layer, without a background image.
- Hand model: [Anatomically Accurate Rigged & Animated Hand Model](https://github.com/emmalieker/anatomical-hand-model), © 2026 Emma L. D. Lieker, used under CC BY-NC 4.0 for this academic project. The downloaded GLB and license are in `RealisticHandsSource/`.

## Validation

The presentation targets Unity 6000.3.16f1. Scene checks cover serialized UI references, all eight original door-leaf bindings, restored renderer/light defaults, input binding coverage, the existing Intro destination, and full-phone framing at 16:9, 16:10, 4:3 and 9:16.

No Unity window automation or visual playtest was performed. Play mode review is still needed for final material appearance, hand poses, transition feel, and the settings projection on the target display.
