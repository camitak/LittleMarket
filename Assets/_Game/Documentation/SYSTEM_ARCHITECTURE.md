# Little Market — System Architecture

Current architecture after **Lesson 114 — Reliable Fixed-Fixture Navigation & Automatic Layout Overview**.

## Baseline
Lesson 114 is designed from GitHub `main` commit:

`5a1b9cb9639684e9d73d568ad60a220b35164a50`

Commit message:

`Lesson 113 complete - mouse driven layout and operational clearance`

GitHub `main` remains the source of truth.

## Why Lesson 114 exists
Lesson 113 improved Layout Mode interaction and introduced semantic operational clearance, but two foundation issues remained:

1. customers could still walk over `CustomerBasketStation`
2. Layout Mode opened around the player's current position rather than presenting the store as an editing workspace

Lesson 114 fixes those before adding the visual grid/footprint layer.

Fine movement/orbit smoothing is deliberately deferred until the visual placement language is present, because input feel should be tuned against the near-final editing feedback rather than repeatedly retuned while the interface is still changing.

## FixedNavigationBlocker
`FixedNavigationBlocker` no longer uses `NavMeshModifierVolume`.

The Lesson-113 volume was correctly configured, but its top boundary ended at the basket-rack collider's top surface. Because NavMesh generation is voxelized, the top surface could still become walkable.

For a fixed physical fixture, the reliable semantic rule is to mark the fixture's own collected geometry as `Not Walkable`.

`FixedNavigationBlocker` now requires:
- `BoxCollider`
- `NavMeshModifier`

It configures:
- Override Area = true
- Area = Not Walkable
- Ignore From Build = false

The NavMeshModifier applies the area override to the fixture geometry used by `Navigation/NavMeshSurface`.

Use `FixedNavigationBlocker` on:
- `CustomerBasketStation`
- `CheckoutCounter`

Remove the old `NavMeshModifierVolume` components from those two fixed fixtures.

`FurnitureNavigationBlocker` remains unchanged and continues using `NavMeshModifierVolume`, because movable furniture needs a semantic body footprint that can be larger/more regular than its individual physics colliders.

## CustomerBasketStation
Current scene facts:
- root position `(4.2, 0, -3.173)`
- BoxCollider center `(0, 0.2, 0)`
- BoxCollider size `(0.95, 0.65, 0.72)`
- PickupPoint local position `(0.9, 0, 0.3)`

The physical rack geometry is now baked as Not Walkable.

The PickupPoint remains outside the blocked rack body and therefore remains reachable for customers who need a basket.

Customers who do not need a basket route around the rack.

## StoreBuildArea overview bounds
`StoreBuildArea` gains:

`TryGetAvailableBounds(StoreAreaType requiredArea, out Bounds bounds)`

The method combines the world-space floor bounds of all currently available `StoreBuildZone` entries matching the requested area.

For `SalesFloor`, this automatically includes:
- starter `SalesFloorArea`
- unlocked `Expansion_Sales_01`
- unlocked `Expansion_SalesRear_01`

Locked expansions are excluded because `StoreBuildZone.IsAvailable` is false.

This keeps camera framing data-driven and expansion-aware.

## Layout camera overview
`LayoutCameraController` now supports:

`ActivateForBounds(Bounds targetBounds)`

Layout Mode no longer starts from the player's current transform when valid sales-floor bounds are available.

On entry it:
1. centers the layout rig on the available sales-floor bounds
2. uses a consistent overview yaw
3. uses an overview pitch
4. calculates the required camera distance from the target bounds and camera FOV
5. clamps that distance to the allowed zoom range

The framing calculation uses the smaller of horizontal and vertical camera FOV, so the floor remains framed across different aspect ratios.

Prototype values:
- Overview Pitch = 62°
- Overview Yaw = 0°
- Overview Padding = 1.08
- Fallback Distance = 18 m
- Minimum Pitch = 15°
- Maximum Pitch = 88°
- Minimum Distance = 4 m
- Maximum Distance = 32 m
- Pan Speed = 7 m/s
- Boost Multiplier = 2
- Yaw Sensitivity = 0.12
- Pitch Sensitivity = 0.10
- Scroll Zoom Sensitivity = 0.035

Zoom responsiveness is increased provisionally. Full smoothing/speed tuning is deferred.

## StoreLayoutModeController
New Store reference:

`StoreBuildArea buildArea`

On Layout Mode entry:
- first ask `buildArea.TryGetAvailableBounds(StoreAreaType.SalesFloor, ...)`
- when successful, call `layoutCameraController.ActivateForBounds(...)`
- otherwise fall back to `ActivateFrom(playerController.transform)`

Exact scene assignment:
`StoreSystems -> Store Layout Mode Controller -> Build Area = StoreSystems / StoreBuildArea`

All existing simulation suspension/recovery behavior remains.

## Existing Layout Mode controls
- Left Mouse: select furniture / confirm
- mouse movement: move selected furniture
- Middle Mouse drag: orbit/tilt
- mouse wheel: zoom
- WASD: pan
- Shift + WASD: faster pan
- R/T: rotate
- Right Mouse / Esc / Q: cancel
- L: exit

Cursor remains visible/unlocked during Layout Mode.
First-person Crosshair remains hidden during Layout Mode.

## Existing spatial architecture
Three distinct concepts remain:

1. **physical body**
    - actual object geometry/collider

2. **navigation blocker**
    - customers cannot navigate here
    - fixed fixtures use NavMeshModifier
    - movable furniture uses FurnitureNavigationBlocker + NavMeshModifierVolume

3. **layout clearance**
    - area remains walkable
    - furniture is not allowed to occupy it
    - used by CheckoutCounter queue/service lane

Do not merge these concepts.

## Existing checkout clearance
`CheckoutCounter` still has `LayoutClearanceZone`:
- Area Type SalesFloor
- Center `(0, 0.75, -2.0)`
- Half Extents `(0.8, 0.75, 1.55)`
- Block Furniture Body = true
- Block Furniture Access = true

This protects the queue/service lane from furniture placement.

## Existing navigation safety
Lesson 112 remains:
- `CustomerNavigationRecovery` checks NavMesh position, vertical tolerance and physical capsule clearance
- ceilings use `Ceiling` layer and are excluded from LayoutCamera and NavMeshSurface
- NavMesh rebuild happens once when exiting a dirty Layout Mode session

## Roadmap
### Lesson 115 — Grid & Footprint Visual Language
- subtle floor grid
- selected furniture body footprint
- access/front footprint
- checkout operational clearance visualization
- nearby occupied/conflicting spaces

### Lesson 116 — Grid/Precision Controls & Modular Asset Standard
- grid ON/OFF
- free precision placement
- modular asset measurements
- standard pivots and local-front convention

### Lesson 117 — Magnetic Socket Snapping
- compatible sockets
- zero-gap adjacency
- attach/detach hysteresis
- alignment guides

A later dedicated Layout Mode feel pass should tune:
- pan acceleration/deceleration
- orbit sensitivity
- zoom easing
- object-follow smoothing
- optional camera edge pan if desired

Do this after the main visual/precision interaction model is stable.

## Permanent principles
- GitHub `main` is source of truth.
- Inspect current scripts/prefabs/scene before every lesson.
- Never guess object names, paths, fields or values.
- Prefer semantic gameplay rules over incidental collider behavior.
- Fixed fixture geometry and movable furniture footprints can use different NavMesh annotation strategies.
- Navigation blocker and placement-clearance zone are different concepts.
- Avoid god classes.
- The building provides space; the player decides how the grocery operates inside it.
- Layout Mode works while the store is open.
- Stocked furniture can move.
- Grid and magnetic snapping are independent.
- 1 Unity unit = 1 metre.
- End every lesson with updated architecture and continuation-prompt `.md` files.
