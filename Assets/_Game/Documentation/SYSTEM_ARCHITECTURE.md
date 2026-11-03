# Little Market — System Architecture

Current proposed architecture after **Lesson 118 — Grid Snap / Free Precision & Modular Asset Standard**.

## Verified source of truth

Lesson 118 was designed after inspecting GitHub `main` at:

`a20b241856c29f85c8d75e97896f13110fc6d425`

Commit:

`Lesson 117 complete - visible furniture access and conflict feedback`

This document describes the architecture after applying and testing Lesson 118. Re-verify GitHub `main` before the next lesson.

## Stack

- Unity 6.3
- C#
- JetBrains Rider
- URP
- Unity Input System
- Unity AI Navigation 2.0.14
- repository `camitak/LittleMarket`
- branch `main`
- main scene `Assets/Scenes/Market.unity`

## Core design principle

> The building provides space; the player decides how the grocery operates inside that space.

Little Market is a cozy first-person grocery-management game. Placement is a major design pillar and must remain understandable, flexible and data-driven.

## Current Layout Mode controls

- LMB: select / confirm
- mouse movement: position selected furniture
- MMB drag: orbit / tilt camera
- mouse wheel: zoom
- WASD: pan
- Shift + WASD: faster pan
- R / T: rotate furniture
- G: toggle Grid Snap ON/OFF
- RMB / Esc / Q: cancel
- L: exit Layout Mode
- cursor visible/unlocked
- first-person crosshair hidden

## Layout camera baseline

Preserve the actual pushed LayoutCameraController values unless intentionally revisiting them in the dedicated camera-feel pass:

- Overview Pitch `62`
- Overview Yaw `0`
- Overview Padding `1.08`
- Fallback Distance `4`
- Min Pitch `15`
- Max Pitch `88`
- Min Distance `4`
- Max Distance `32`
- Pan Speed `7`
- Boost `2`
- Yaw Sensitivity `.12`
- Pitch Sensitivity `.10`
- **Scroll Zoom Sensitivity `2`**

The full-sales-floor startup overview remains provisional.

Future camera direction:

- focus the relevant sales-floor or backroom working region based on player context
- use a comfortable fixed default editing distance
- allow manual full-store zoom-out
- treat future exterior build space as its own focus/build region
- avoid progressively zooming farther out as the store expands

## Spatial concepts remain separate

### Physical body
Actual colliders and geometry.

### Navigation blocker
Defines where customers cannot navigate.

### Operational access
Space customers/staff need to use furniture or store systems. It is navigable but furniture bodies may be prohibited from occupying it.

Do not merge these responsibilities.

## FurnitureDefinition remains authoritative

FurnitureDefinition owns movable-furniture placement geometry:

- PlacementArea
- BodyCenter
- BodyHalfExtents
- AccessCenter
- AccessHalfExtents

Functional/front direction is local `-Z`.

### Current standard shelf / refrigerated display

- body center `(0,1.1,0)`
- body half extents `(1.3,1.05,.52)`
- access center `(0,.8,-1.02)`
- access half extents `(1.25,.75,.5)`

### Current ambient / chilled storage racks

- area `Backroom`
- body center `(0,.8,0)`
- body half extents `(1.22,.82,.3)`
- access center `(0,.75,-.8)`
- access half extents `(1.15,.75,.5)`

## Placement validation

FurniturePlacementController continues to validate:

1. selected body inside permitted build area
2. selected access inside permitted build area
3. selected body against explicit LayoutClearanceZone
4. selected access against explicit LayoutClearanceZone
5. selected body against access footprints of other furniture
6. selected body against colliders
7. selected access against colliders

Body vs another furniture's access is forbidden.

Access vs access remains allowed so facing furniture may share an aisle.

Lesson 118 does not alter these rules.

## Lesson 118 grid / precision architecture

FurniturePlacementController becomes the owner of **Grid Snap state**.

New serialized state:

`gridSnapEnabled = true`

The actual grid step remains:

`gridSize = .1 m`

The source initializer is updated to `.1` as well so code defaults and the current scene no longer disagree.

Public read-only presentation surface:

- `GridSnapEnabled`
- `GridSize`

### Grid Snap ON

When ON:

1. raycast finds the permitted floor point
2. X/Z are quantized to the `.1 m` placement grid
3. the existing provisional furniture body/alignment assist may run
4. normal placement validation runs

This preserves the placement feel that existed before Lesson 118.

### Grid Snap OFF / Free Precision

When OFF:

1. raycast finds the permitted floor point
2. raw X/Z are used directly
3. the old provisional `ApplyFurnitureSnap` assist is bypassed
4. normal placement validation still runs

This provides genuinely free positional placement while retaining all body/access/build-area rules.

The provisional body-based furniture snap is not the future magnetic system. Lesson 119 introduces explicit sockets as a separate architecture.

### Visual grid independence

`LayoutWorldVisualizer.visibleGridSize = .5 m` remains unchanged.

The pale-cyan `.5 m` grid is a visual reference and remains visible whether Grid Snap is ON or OFF.

Therefore:

- visual grid size != placement snap step
- visual grid visibility != Grid Snap enabled state

This distinction is permanent.

## LayoutModeUI additions

LayoutModeUI still owns:

- LayoutModeFrame visibility
- normal crosshair hiding/restoration

Lesson 118 adds:

- serialized `FurniturePlacementController placementController`
- serialized `TMP_Text gridSnapStatusText`

The status is intentionally compact:

Grid ON:
`GRID  ON  ·  0.10 m  ·  [G]`

Grid OFF:
`GRID  OFF  ·  FREE  ·  [G]`

The status remains visible whenever Layout Mode is active, including when no furniture is selected.

The UI reads placement state; it does not own or mutate placement rules.

## Existing visual architecture

### LayoutVisualUtility
Rendering helper for runtime transparent materials, unit XZ quad and grid meshes.

### LayoutFootprintUtility
Geometry helper for horizontal oriented-footprint overlap.

Do not merge these utilities.

### LayoutWorldVisualizer
On StoreSystems.

Shows:

- `.5 m` pale-cyan reference grid over each available build zone
- purple explicit LayoutClearanceZone areas

No physics or placement authority.

### FurniturePlacementVisualizer
On Player.

Shows selected furniture:

- green body when valid
- amber selected access when valid
- red when invalid

### LayoutFurnitureAccessVisualizer
On StoreSystems.

Shows existing unselected furniture access:

- soft amber normally
- red when the selected furniture body intrudes into that access zone

Shared access remains allowed.

## Modular asset production standard

Lesson 118 formalizes the standard for **new and revised furniture assets**.

### World scale

`1 Unity unit = 1 metre`

FurnitureDefinition body/access dimensions are authored in metres.

### Root transform

Every placeable furniture prefab root should use:

- local position `(0,0,0)` inside the prefab
- local rotation identity
- local scale `(1,1,1)`

Current Shelf, Refrigerated Display, Ambient Storage Rack and Chilled Storage Rack roots already satisfy the scale/position baseline.

Child meshes may use local scale as needed. The root is the gameplay coordinate system.

### Root pivot

The root pivot should be at floor level (`Y = 0`).

Prefer X/Z near the center of the physical body footprint unless the asset has a strong reason otherwise.

This lets placement position correspond to a meaningful floor point.

### Functional front

Local `-Z` is the standard functional/front direction.

Examples:

- shelf customer-facing side
- refrigerated-display customer-facing side
- storage-rack working side

AccessCenter should therefore normally be located toward negative local Z.

### Modular dimensions

Prefer `.25 m` increments for new horizontal furniture dimensions, offsets and future socket locations **where practical**.

This is a production target, not an immediate migration requirement.

Existing prototype assets do not need to be remodeled simply because some current child geometry uses non-.25 dimensions.

Gameplay fit, readability and art quality may justify exceptions.

### Runtime scaling

Do not use arbitrary root scaling to create furniture variants.

Prefer:

- dedicated prefab / definition variants
- modular child geometry
- explicit body/access data

Root scale should remain one so placement, navigation and socket geometry stay predictable.

## Build areas

StoreBuildArea remains authoritative for available floor regions and footprint containment.

Relevant APIs:

- TryGetPlacementSurface
- IsFootprintInside
- IsBuildFloorCollider
- TryGetAvailableBounds
- CollectAvailableFloorBounds

## Navigation

Fixed fixtures:

- FixedNavigationBlocker + NavMeshModifier
- CheckoutCounter / CustomerBasketStation geometry marked Not Walkable

Movable furniture:

- FurnitureNavigationBlocker + NavMeshModifierVolume
- semantic body footprint

Customer recovery:

- NavMesh agent position
- vertical tolerance
- physical capsule clearance

Ceilings remain excluded from LayoutCamera and NavMeshSurface.

A dirty Layout Mode session rebuilds NavMesh once on exit.

Lesson 118 changes no navigation behavior.

## Save

- schema `2`
- filename `little_market_dev_v2_save.json`

Grid Snap state is a Layout Mode interaction preference and is not added to the save schema in Lesson 118.

It persists while the current runtime controller exists, but a new play session starts from the serialized default.

Save/load still blocks active customers until their in-progress state is intentionally persisted.

## Checkout future

Desired tactile flow remains:

customer unloads -> physical scan -> bag -> card/cash payment -> leaves

Later:

- cash drawer / change
- tender keypad
- PLU
- upgraded terminals
- CheckoutRegistry
- multiple placeable checkouts
- self-checkouts

## Forward roadmap

### Lesson 119 — Magnetic Socket Snapping

Introduce an explicit system independent from Grid Snap:

- compatible socket types
- zero-gap adjacency
- socket world transforms
- capture distance
- attach/detach hysteresis
- alignment guides
- clear separation from free/grid placement

Grid Snap OFF must not inherently disable the future magnetic system unless the player explicitly chooses to bypass/disable magnetism.

### Later — Layout Camera & Feel Pass

- sales-floor/backroom entry context
- useful fixed editing distance
- manual full-store zoom-out
- exterior/outdoor focus regions
- pan/orbit/zoom easing
- selected-object follow smoothing
- optional edge-pan evaluation

## Permanent development rules

- GitHub `main` is the source of truth
- inspect the actual latest scripts, scene, prefabs and definitions before every lesson
- never invent paths, hierarchy or Inspector values
- preserve user-authored tuning, especially camera Scroll Zoom Sensitivity `2`
- keep validation, navigation and visualization separate
- visual grid and placement grid are independent
- future magnetic snapping and Grid Snap are independent
- keep FurnitureDefinition authoritative for furniture spatial data
- avoid god classes
- substantial rewrites get complete replacement code; small changes may use anchored edits
- include exact Inspector setup and focused regression tests
- finish each lesson with an updated complete architecture document and fresh-chat continuation prompt
