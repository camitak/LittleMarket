# Little Market — System Architecture

Current architecture after **Lesson 115 — Grid & Footprint Visual Language**.

## Baseline
Lesson 115 is designed from GitHub `main` commit:

`aa83c6856db146001d8102a9289bd2ff6ba87c29`

Commit message:

`Lesson 114 complete - fixed fixture navigation and automatic layout overview`

GitHub `main` remains the source of truth.

## User-authored camera value
The actual pushed scene has:

`LayoutCameraRig -> LayoutCameraController -> Scroll Zoom Sensitivity = 2`

This is intentional user tuning and must be preserved. Do not reset it to the older lesson value `0.035`.

Current pushed camera also has:
- Overview Pitch 62
- Overview Yaw 0
- Overview Padding 1.08
- Fallback Distance 4
- Minimum Pitch 15
- Maximum Pitch 88
- Minimum Distance 4
- Maximum Distance 32
- Pan Speed 7
- Boost Multiplier 2
- Yaw Sensitivity .12
- Pitch Sensitivity .10
- Scroll Zoom Sensitivity 2

## Future Layout Mode camera direction
The current whole-sales-floor overview is acceptable temporarily but is not the intended long-term camera contract.

Preferred future behavior:
- entering from the sales floor focuses the sales-floor working area
- entering from the backroom focuses the backroom
- startup framing uses a comfortable fixed editing distance rather than always fitting the entire growing area
- manual zoom-out remains available when the player wants the full picture
- store growth should not automatically push the default camera farther and farther away
- future exterior/outdoor layout space should be handled as its own buildable/focus region rather than exposing the whole world indiscriminately

A future camera lesson should introduce player-area detection / focus context, likely through StoreBuildArea rather than hard-coded world coordinates.

Do not implement this by repeatedly changing global overview bounds.

## Lesson-115 visual architecture
Placement validation remains the source of truth.

Visual feedback is split into dedicated presentation components:

### LayoutWorldVisualizer
Lives on `StoreSystems`.

Responsibilities:
- show a subtle floor grid over each currently available build zone
- show semantic `LayoutClearanceZone` footprints such as the checkout queue/service lane
- create/destroy/toggle runtime-only visual meshes
- never create colliders
- never change placement validation

It references:
- `StoreSystems -> StoreLayoutModeController`
- `StoreSystems -> StoreBuildArea`

It displays both available SalesFloor and Backroom zones.

The grid is generated per build-zone floor bounds rather than from one union bound. This prevents phantom grid space across L-shaped expansion combinations.

Prototype visual grid:
- visible grid size: 0.5 m
- line width: 0.015 m
- floor offset: 0.025 m
- grid color: subtle pale cyan
- no physics / no raycast behavior

The visual 0.5 m grid is currently only a reference. `FurniturePlacementController.gridSize` remains the actual 0.1 m placement snap step until Lesson 116 adds explicit Grid Snap ON/OFF / precision behavior.

### FurniturePlacementVisualizer
Lives on `Player`.

Responsibilities:
- show selected furniture body footprint
- show selected furniture front/access footprint
- update pose/rotation with the selected furniture
- switch presentation between valid and invalid
- hide on confirm/cancel/exit

Prototype colors:
- valid body: translucent green
- invalid body: translucent red
- valid access: translucent amber
- invalid access: translucent red

The visualizer has no placement rules and no colliders.

## LayoutVisualUtility
Static rendering utility shared by the two visualizers.

Responsibilities:
- create transparent unlit runtime materials
- create a reusable unit XZ quad mesh
- create the floor-grid mesh from individual world-space floor Bounds

Runtime visuals use URP Unlit transparent material when available, with `Sprites/Default` as fallback.

## StoreBuildArea additions
Existing:
- `TryGetPlacementSurface`
- `TryGetAvailableBounds`
- `IsFootprintInside`
- `IsBuildFloorCollider`

Lesson 115 adds:

`CollectAvailableFloorBounds(StoreAreaType requiredArea, List<Bounds> results)`

This exposes each available zone's floor bounds separately for presentation systems.

It does not alter build-zone availability or placement rules.

## LayoutClearanceZone additions
Existing overlap logic remains unchanged.

Public read-only presentation data is added:
- `AreaType`
- `Center`
- `HalfExtents`
- `BlocksFurnitureBody`
- `BlocksFurnitureAccess`

This allows LayoutWorldVisualizer to display semantic operational space without duplicating configuration.

Current CheckoutCounter zone remains:
- SalesFloor
- center `(0, 0.75, -2.0)`
- half extents `(0.8, 0.75, 1.55)`
- blocks body and access

## FurniturePlacementController integration
The controller keeps all existing selection, snapping, validation, purchase and confirmation behavior.

New serialized reference:
- `FurniturePlacementVisualizer placementVisualizer`

During preview it sends:
- selected furniture Transform
- body center / half extents
- access center / half extents
- current `canPlace`

The visualizer mirrors the validator state.

The existing furniture tint and access-outline logic remains for now. The new footprint fills complement it. A future cleanup may move all preview rendering into the visualizer after the full placement visual language is proven.

## Layout visual language
When Layout Mode is active:
- pale cyan grid = dimensional reference
- translucent purple operational zone = keep this service/work/queue area clear
- translucent green body footprint = furniture body fits
- translucent amber access footprint = customer/front access required
- red selected footprint = current placement is invalid

This creates a stable visual vocabulary before precision/grid toggles and magnetic snapping are added.

## Existing navigation architecture
Fixed fixtures:
- `FixedNavigationBlocker + NavMeshModifier`
- CustomerBasketStation and CheckoutCounter geometry = Not Walkable

Movable furniture:
- `FurnitureNavigationBlocker + NavMeshModifierVolume`
- semantic body footprint = Not Walkable

Customer recovery:
- same-agent NavMesh sample
- vertical tolerance
- physical capsule clearance

Ceiling:
- excluded from layout camera rendering and NavMesh build

NavMesh:
- rebuilt once on dirty Layout Mode exit

## Existing Layout Mode controls
- Left Mouse select / confirm
- mouse movement moves selected furniture
- Middle Mouse drag orbits/tilts camera
- mouse wheel zoom
- WASD pan
- Shift + WASD faster pan
- R/T rotate
- Right Mouse / Esc / Q cancel
- L exit

Cursor visible/unlocked.
Crosshair hidden.

## Roadmap
### Lesson 116 — Grid / Precision Controls & Modular Asset Standard
- independent Grid Snap ON/OFF
- preserve visual grid independently from snapping
- free precision placement
- likely surface controls in Layout Mode UI
- formal 1 Unity unit = 1 metre production standard
- 0.25 m asset module target where practical
- consistent floor-root pivot / local -Z functional-front convention

### Lesson 117 — Magnetic Socket Snapping
- explicit compatible sockets
- zero-gap adjacency
- sticky attach/detach hysteresis
- alignment guides

### Later — Layout Mode Camera & Feel Pass
After precision/snapping is stable:
- area-aware startup focus (sales floor vs backroom)
- fixed comfortable default editing distance
- optional manual full-store overview
- exterior/outdoor layout-region policy
- pan acceleration/deceleration
- orbit sensitivity
- zoom easing
- object-follow smoothing
- optional edge-pan evaluation

## Permanent principles
- GitHub `main` is source of truth.
- Inspect current scripts/prefabs/scene before every lesson.
- Never guess object names, paths, fields or values.
- Preserve user-authored Inspector tuning unless intentionally changing it.
- Placement rules and placement presentation are separate responsibilities.
- Prefer semantic gameplay zones over incidental collider behavior.
- Navigation blockers and layout-clearance zones are different concepts.
- Avoid god classes.
- The building provides space; the player decides how the grocery operates inside it.
- Layout Mode works while the store is open.
- Stocked furniture can move.
- Grid visibility and grid snapping are independent concepts.
- Magnetic snapping is independent from grid snapping.
- 1 Unity unit = 1 metre.
- End every lesson with updated architecture and continuation-prompt `.md` files.
