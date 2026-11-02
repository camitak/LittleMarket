# Little Market — System Architecture

Current architecture after **Lesson 112 — Layout Reliability, Navigation Safety & Camera Usability**.

## Baseline
Designed from GitHub `main` commit `7c6adba598527a99b93cca37eb80f8ef1c7f2656` (`Lesson 111 complete - reliable layout presentation and dedicated camera`).

GitHub `main` is the source of truth. At the start of Lesson 112, the committed `SYSTEM_ARCHITECTURE.md` still described Lesson 110, so replace it with this file after completing the lesson.

## Invisible-customer root cause
The remaining bug was spatial recovery, not primarily customer rendering. Lesson 111 used `NavMesh.SamplePosition` near the customer's old position and immediately warped to the returned NavMesh point. A NavMesh point is not guaranteed to be physically clear or on the intended floor.

Lesson 112 adds `CustomerNavigationRecovery`, which searches outward from the position captured on Layout Mode entry and accepts only candidates that:
- match the customer's NavMesh agent type/area mask
- remain within a small vertical tolerance
- have physical capsule clearance
- are not inside furniture/walls
- are selected from the nearest successful search ring

Fallback recovery near entrance/exit remains only for cases where the original area has no safe candidate.

Prototype values: 4 m max radius and 0.5 m vertical tolerance.

## Furniture navigation
New `FurnitureNavigationBlocker` requires `FurnitureIdentity` and `NavMeshModifierVolume`.

It derives a Not Walkable modifier volume from `FurnitureDefinition.BodyCenter` and `BodyHalfExtents`, with 0.05 m horizontal padding and 0.20 m floor overlap. The body footprint becomes non-walkable while the access footprint stays walkable.

Add it to:
- PF_Shelf
- PF_RefrigeratedDisplay
- PF_StorageRack_Ambient
- PF_StorageRack_Chilled

## Ceiling layer
Custom layer 6: `Ceiling`.

Exact ceiling objects:
- StoreBuilding/Base/Ceiling_Base
- StoreBuilding/Expansion_Sales_01/UnlockedGeometry/Ceiling_SalesExpansion_01
- StoreBuilding/Expansion_SalesRear_01/UnlockedGeometry/Ceiling_SalesRearExpansion_01
- StoreBuilding/Expansion_Backroom_01/UnlockedGeometry/Ceiling_BackroomExpansion_01

`LayoutCamera` excludes `Ceiling` from its Culling Mask. `Navigation/NavMeshSurface` excludes `Ceiling` from its Layer Mask. Player/Main Camera still includes it.

This hides roofs only in Layout Mode and prevents ceiling colliders from becoming navigation build sources.

## Layout camera
LayoutCameraController values:
- start pitch 60
- min pitch 15
- max pitch 88
- start distance 11 m
- min distance 4 m
- max distance 24 m
- pan 7 m/s
- Shift boost 2x
- yaw sensitivity .12
- pitch sensitivity .10
- wheel zoom sensitivity .02

Controls remain WASD, mouse look, wheel zoom, Shift boost, L exit.

## Furniture selection
Lesson 111 kept the old first-person 4 m grab ray even though the layout camera starts roughly 10 m away.

Lesson 112 replaces it with:
- selectionRayDistance = 35 m
- previewRayDistance = 30 m

G continues to select/move furniture under the centre reticle.

## Layout border
`GameUI/LayoutModeFrame` becomes an inset neon frame.

Inset: 16 px.

Core bars:
- FrameTop/Bottom/Left/Right
- thickness 5 px
- color #A7F8FFFF

Glow bars:
- GlowTop/Bottom/Left/Right
- thickness 18 px
- color #31DFFF55
- rendered behind the core
- Raycast Target off

## Customer presentation
`CustomerLayoutPresentation` remains presentation-only. It uses `Renderer.forceRenderingOff` during Layout Mode and restores prior values afterward. Customer colliders remain disabled while hidden so they do not block placement.

Navigation safety and presentation remain separate responsibilities.

## Layout Mode lifecycle
Enter: suspend clock/customers, hide customer presentation, disable normal player controls/camera, enable layout camera/UI.

During: move/buy stocked furniture freely; mark navigation dirty; no per-object NavMesh rebuild.

Exit: rebuild NavMesh once if dirty, safely recover customers, reissue activity, restore customer presentation, resume simulation, restore player camera/controls.

## Roadmap
Lesson 113 — Grid & Footprint Visual Language.
Lesson 114 — Grid/Precision Controls & Modular Asset Standard.
Lesson 115 — Magnetic Socket Snapping.

Add refinement lessons if placement still does not feel good.

## Permanent principles
- Inspect latest GitHub before every lesson.
- Never guess object names, paths, fields or values.
- Keep responsibilities separated; avoid god classes.
- Do not fix architecture bugs with presentation workarounds.
- The building provides space; the player decides how the grocery operates inside it.
- Layout Mode works while the store is open.
- Stocked furniture may move.
- Grid and magnetic snapping are independent.
- 1 Unity unit = 1 metre.
- End every lesson with an updated architecture `.md` and continuation-prompt `.md`.
