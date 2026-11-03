# Little Market — System Architecture

Current architecture after **Lesson 113 — Mouse-Driven Layout Editing & Operational Clearance**.

Baseline: GitHub `main` commit `1945af5b5be799d6402c231c7a91a9cdd4904d9a` (`Lesson 112 complete - layout navigation safety camera and visual refinement`).

## Layout controls
Layout Mode is now a cursor-driven editing tool:
- Left Mouse: select furniture / confirm placement
- mouse movement: move selected furniture on the floor
- Middle Mouse drag: orbit/tilt layout camera
- mouse wheel: zoom
- WASD: pan
- Left Shift + WASD: faster pan
- R/T: rotate selected furniture
- Right Mouse, Esc or Q: cancel placement
- L: exit Layout Mode

The cursor is visible/unlocked in Layout Mode. The first-person crosshair is hidden and restored on exit.

## LayoutCameraController
Raw mouse movement no longer rotates the camera. Camera orbit only occurs while Middle Mouse is held. `IsOrbiting` is exposed so placement pauses during camera manipulation.

Current committed scene values are preserved unless deliberately changed later:
- starting pitch 15
- minimum pitch 15
- maximum pitch 88
- starting distance 11
- minimum distance 4
- maximum distance 24
- pan speed 7
- boost 2
- yaw sensitivity .12
- pitch sensitivity .10
- zoom sensitivity .02

## FurniturePlacementController
Pointer-based rays use `Camera.ScreenPointToRay(Mouse.current.position.ReadValue())` instead of camera-centre rays.

Current values remain:
- selection ray distance 35
- preview ray distance 30
- grid size .1
- furniture snap distance .35
- furniture snap gap .01
- alignment snap distance .30

Placement ignores raycastable UI. Existing colliders, preview tinting, access outline, snapping and purchase behavior remain.

## FixedNavigationBlocker
New generic component for fixed physical fixtures:
- requires BoxCollider
- requires NavMeshModifierVolume
- derives a Not Walkable modifier volume from the BoxCollider
- horizontal padding .05
- floor overlap .20

Add to:
- CustomerBasketStation
- CheckoutCounter

CustomerBasketStation's collider is 0.95 × 0.45 × 0.72. Because Humanoid agent climb is .75, raw geometry allowed customers to walk over it. The explicit Not Walkable modifier expresses gameplay intent.

## LayoutClearanceZone
New generic semantic operational-space component. It is not a NavMesh obstacle.

It represents walkable space that furniture must not occupy, and can independently block furniture body/access footprints.

CheckoutCounter gets:
- Area Type: SalesFloor
- Center `(0, 0.75, -2.0)`
- Half Extents `(0.8, 0.75, 1.55)`
- Block Furniture Body: true
- Block Furniture Access: true

Current CheckoutCounter:
- root `(0.3,0,-3.4)`
- Y rotation 180
- collider `1.8 × 1.0 × 0.8`
- queue points local Z: -1.2, -2.1, -3.0

The zone protects the customer queue/service lane while keeping it walkable.

## LayoutModeUI
Now owns:
- LayoutModeFrame
- GameUI/Crosshair

It preserves Crosshair state, hides it in Layout Mode, and restores it afterward.

## Existing navigation safety
Lesson 112 remains:
- CustomerNavigationRecovery validates same-floor NavMesh + physical capsule clearance
- FurnitureNavigationBlocker derives Not Walkable volumes from FurnitureDefinition
- Ceiling layer excluded from LayoutCamera and NavMeshSurface
- NavMesh rebuild once on Layout Mode exit

## Roadmap
Lesson 114 — Grid & Footprint Visual Language.
Lesson 115 — Grid/Precision Controls & Modular Asset Standard.
Lesson 116 — Magnetic Socket Snapping.

## Permanent principles
- GitHub `main` is source of truth.
- Inspect scripts/prefabs/scene before every lesson.
- Never guess object names, paths, fields or values.
- Prefer semantic gameplay zones over accidental collider behavior.
- Navigation blockers and layout clearance zones are different concepts.
- Avoid god classes.
- The building provides space; the player decides how the grocery operates inside it.
- Layout Mode works while the store is open.
- Stocked furniture can move.
- Grid and magnetic snapping are independent.
- 1 Unity unit = 1 metre.
- End every lesson with updated architecture and continuation-prompt `.md` files.
