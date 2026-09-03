# VR Testing Guide

This document explains how to test the game's VR support **without a VR headset**, and what to enable once you have a real one.

## What the VR support provides

- **`VRRig`** (on the `Player` object in the scene, `Mode = Auto`): auto-detects a headset (HMD). When one is present it automatically:
  - switches the camera to tracked mode (adds a `TrackedPoseDriver`, camera parented under a `VRTrackingOffset` node);
  - makes the body follow the head (room-scale body-follow);
  - adds the VR controller bindings at runtime and temporarily mutes the keyboard bindings;
  - on headset loss (in Auto mode) restores the camera and keyboard exactly as before.
- **OpenXR** is embedded in `Packages/com.unity.xr.openxr` (with XR Interaction Toolkit 3.6.0). Nothing extra to install.
- **Initialize XR on Startup = OFF** (opt-in): normal desktop play is NOT forced into VR. VR only starts when you explicitly launch it (mock) or plug in a configured headset.

---

## Method 1 — Mock VR (stereo, NO headset) — verify the pipeline

Use this for a quick check: OpenXR initializes, a virtual HMD appears, and VRRig switches into VR. **You cannot interact** (the Mock Runtime has no motion sensors).

1. Press **Play**.
2. Menu **`HCMUS/VR/Start Mock VR (stereo)`**.
3. VRRig switches into VR mode (camera gets a TrackedPoseDriver, VR controller bindings are added).
4. When done, **`HCMUS/VR/Stop Mock VR`** to shut it down.

> This menu lives in `Assets/Scripts/Editor/MockVRMenu.cs`. It disables the XR Device Simulator first so two head-pose sources don't fight.

**Expected result** (verified): loader = OpenXRLoader, a `Head Tracking - OpenXR` device appears, `controller.vrMode = true`, 4 VR bindings added, 10 keyboard bindings muted; Stop restores everything cleanly.

---

## Method 2 — XR Device Simulator (simulate head + controllers with mouse/keyboard)

This is the **closest you can get to "feeling" VR without a headset** — you drive the simulated head and two controllers with the mouse and keyboard.

1. In the scene, find the **`XR Device Simulator`** object (**inactive** by default) → **enable it (set active)**.
2. Press **Play**.
3. Controls:
   - Hold **right mouse + move mouse**: rotate the head (look around).
   - **W/A/S/D**: move the head/body.
   - Hold **T** (or **Y**): switch to controlling the left / right controller, then use the mouse/keys to move that hand.
   - The on-screen panel lists every key.
4. The camera rotates with the simulated "head" — this is where you see VRRig actually running.

> Remember to **disable** the XR Device Simulator again after testing so desktop play works normally.

---

## Method 3 — Real headset (Meta Quest, Vive, ...)

The core (head tracking + stereo rendering) works right away. **But the controllers won't respond** until you enable the interaction profile for your headset. Steps:

1. **Project Settings → XR Plug-in Management → OpenXR** (PC/Standalone tab):
   - **Tick the interaction profile that matches your headset** (e.g. Meta Quest → *Oculus Touch Controller Profile* or *Meta Quest Touch Pro Controller Profile*; HTC Vive → *HTC Vive Controller Profile*, etc.). These profiles are already installed — just tick them.
   - **Untick `Mock Runtime`** (it is a testing feature and can block the real runtime).
2. **XR Plug-in Management → tick `Initialize XR on Startup`** (currently off for desktop play). Or leave it off and start XR from code when entering VR mode.
3. Plug in the headset, Build & Run (or Play in the editor if the headset's OpenXR runtime is installed on the machine).

After those two config steps, the headset works: VRRig detects the HMD and switches into VR automatically.

---

## VR control map (while in VR mode)

| Action | Input |
|---|---|
| Move | **Left** thumbstick |
| Sprint | **Click** the left thumbstick |
| Jump | **Primary button (A/X)** on the right controller |
| Interact | **Trigger** on the right controller |
| Snap turn | **Right** thumbstick |

---

## Troubleshooting

- **Started Mock VR but nothing changed**: make sure you are in **Play mode** before using the menu; check the Console for `MockVR: stereo session started`.
- **Camera stutters / two head-pose sources**: only one of the two may run at a time — Mock VR **or** the XR Device Simulator, never both (the Mock VR menu disables the Simulator for you).
- **Desktop play gets forced into VR**: check that `Initialize XR on Startup` is **OFF**.
- **Controllers unresponsive on a real headset**: the interaction profile for your headset isn't ticked (see Method 3, step 1).

---

*The VR work is in Khoa's lane (movement/VR/anomalies/audio). Main scripts: `Assets/Scripts/Player/VRRig.cs`, `Assets/Scripts/Editor/MockVRMenu.cs`.*
