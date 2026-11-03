# Little Market — System Architecture

Current proposed architecture after **Lesson 117 — Visible Furniture Access & Contextual Conflict Feedback**.

## Verified source of truth

Lesson 117 was designed after inspecting GitHub `main` at:

`11473828b553267e63273e35cd141545db1a92c2`

Commit: `Lesson 116 complete - protect furniture access clearance`.

This document describes the architecture *after applying and testing Lesson 117*. It does not claim that Lesson 117 has already been pushed. Re-verify GitHub `main` before the next lesson.

## Stack

- Unity 6.3; C#; JetBrains Rider; URP
- Unity Input System
- Unity AI Navigation 2.0.14
- Repository `camitak/LittleMarket`, branch `main`
- Main scene `Assets/Scenes/Market.unity`

## Foundational design

> The building provides space; the player decides how the grocery operates inside that space.

Little Market is a cozy first-person grocery-management game. Placement must support a believable shop without imposing arbitrary templates.

Spatial concepts remain distinct:

1. **Physical body** — colliders and geometry.
2. **Navigation blocker** — prevents customers from navigating through solid fixtures.
3. **Operational access** — space required for a customer or staff member to work; it is navigable, but other furniture bodies may not occupy it.

Never add physical colliders to purely visual access footprints.

## Current layout mode input

- LMB select/confirm; moving pointer positions furniture
- MMB drag orbit/tilt; mouse wheel zoom
- WASD pan; Shift+WASD faster pan
- R/T rotate; RMB/Esc/Q cancel; L exit
- Cursor visible/unlocked; first-person crosshair hidden

## User-authored camera values

Actual Lesson-116 pushed `LayoutCameraRig -> LayoutCameraController` values:

- Overview Pitch `62`; Overview Yaw `0`; Overview Padding `1.08`
- Fallback Distance `4`; Min Pitch `15`; Max Pitch `88`
- Min Distance `4`; Max Distance `32`
- Pan Speed `7`; Boost `2`; Yaw Sensitivity `.12`; Pitch Sensitivity `.10`
- **Scroll Zoom Sensitivity `2`**, deliberately tuned by user. Preserve.

The full-sales-floor startup overview is provisional. Future camera work must focus the player's relevant sales-floor/backroom area at a comfortable fixed distance, with manual full-store zoom-out and an eventual exterior-region policy. Do not force an ever-growing store into the startup camera.

## FurnitureDefinition is the placement-data authority

`Assets/_Game/Scripts/Store/Furniture/FurnitureDefinition.cs` contains:

- `PlacementArea`
- `BodyCenter`, `BodyHalfExtents`
- `AccessCenter`, `AccessHalfExtents`

`FurnitureIdentity` references the definition and provides persistent identity; prefabs do not duplicate the access data via LayoutClearanceZone.

Current standard shelf and refrigerated display:

- body center `(0,1.1,0)`, body half extents `(1.3,1.05,.52)`
- front-access center `(0,.8,-1.02)`, access half extents `(1.25,.75,.5)`

Current ambient and chilled backroom storage racks:

- area `Backroom`
- body center `(0,.8,0)`, body half extents `(1.22,.82,.3)`
- access center `(0,.75,-.8)`, access half extents `(1.15,.75,.5)`

Functional/front direction is local `-Z`. Prefab root pivot at floor level and root scale `(1,1,1)` remain desired standards; 1 unit = 1 metre; prefer .25-m modules when practical.

## Placement policy — Lesson 116 preserved

`Player/FurniturePlacementController` validates, in order:

1. selected body within permitted build area
2. selected access within permitted build area
3. body against explicit `LayoutClearanceZone`
4. access against explicit `LayoutClearanceZone`
5. **selected body against access footprints of *other* furniture**
6. selected body against colliders
7. selected access against colliders

**Body vs another item's access = forbidden. Access vs access = allowed.** Facing shelves can share one aisle. Access zones themselves never block walking. This contract is not changed in Lesson 117.

`LayoutFootprintUtility.OverlapsHorizontal` performs the shared 2D separating-axis oriented-rectangle test. Validation uses each other furniture's `TransformPoint(definition.AccessCenter)` and horizontally scaled access half-extents; any visual conflict comparison should use those same semantics.

`FurniturePlacementController` continues to collect `furnitureCandidates` on preview start for snapping and access validation. No new physics or blocking rules are introduced in Lesson 117.

Actual pushed Player Inspector values to preserve:

- `Selection Ray Distance = 35`
- `Preview Ray Distance = 30`
- `Grid Size = .1` **(scene override even though source initializer says .5)**
- `Furniture Snap Distance = .35`
- `Furniture Snap Gap = .01`
- `Alignment Snap Distance = .3`

Lesson 117 adds only a read-only integration surface:

- `SelectedFurniture` — active preview identity or null
- `TryGetSelectedBodyFootprint(out worldCenter, out rotation, out horizontalHalfExtents, out area)` — describes the current preview body, including the definition/fallback dimensions that the placement controller uses.

## Visual layers

### LayoutVisualUtility

`Assets/_Game/Scripts/Store/Layout/LayoutVisualUtility.cs`:

- creates runtime transparent URP Unlit materials
- creates reusable XZ quad
- creates floor-grid meshes per available build-zone bounds

Rendering utility only; not placement policy.

### LayoutWorldVisualizer (Lesson 115, unchanged)

Component on `StoreSystems`.

- pale-cyan reference grid over individual available SalesFloor/Backroom build zones
- purple **explicit** LayoutClearanceZone footprints (e.g. checkout queue)
- no colliders, no physics, no validation rules
- materials/meshes created and destroyed at runtime

Current visual grid: `.5m`, width `.015`, offset `.025`.

`StoreBuildArea.CollectAvailableFloorBounds` returns individual available floor zones, preventing phantom grid in L-shaped expansions.

### FurniturePlacementVisualizer (Lesson 115, unchanged)

Component on `Player`.

- selected furniture body: green when valid, red when invalid
- selected furniture access: amber when valid, red when invalid
- current selected pose, rotation, and validator `canPlace`
- hides when selection ends

Reference: `Player/FurniturePlacementController -> placementVisualizer`.

### LayoutFurnitureAccessVisualizer (Lesson 117, new)

New standalone component on `StoreSystems`.

Path: `Assets/_Game/Scripts/Store/Layout/LayoutFurnitureAccessVisualizer.cs`.

Inspector references:

- `Layout Mode -> StoreSystems / StoreLayoutModeController`
- `Placement Controller -> Player / FurniturePlacementController`

Defaults:

- Surface Offset `.04`
- Available Color `RGBA(240,201,106,48)` — soft translucent amber
- Blocked Color `RGBA(196,107,107,150)` — stronger translucent red

Responsibilities:

- during Layout Mode show the **existing, unselected** furniture access footprints, derived from each active FurnitureIdentity's FurnitureDefinition
- use existing `LayoutVisualUtility` runtime quad and transparent materials
- omit the currently selected furniture (already shown by FurniturePlacementVisualizer)
- when selected body intersects one of the other furniture access areas, paint **that specific existing access area** red
- when rotated/moved clear, return it to amber
- other access footprints overlapping each other remain amber; shared aisles are allowed
- hide outside Layout Mode
- no colliders, no NavMesh participation, no validation authority

Lifecycle:

- rebuild visual objects when Layout Mode opens or the current selection changes
- changing selection excludes the moving furniture's *previous* access quad
- selection end rebuilds the list; this allows a newly purchased/confirmed shelf to acquire its own access visual immediately
- `LateUpdate` follows FurniturePlacementController's preview update and refreshes colour/pose without rescanning the scene every frame
- destroyed/cancelled preview instances are hidden rather than leaving ghost visible quads
- runtime visual root/materials/mesh are destroyed with the component

This is deliberately separate from LayoutWorldVisualizer's fixed zones and FurniturePlacementVisualizer's selected object to avoid a god class.

### Colour vocabulary

- cyan grid: spatial reference
- purple: explicit fixed operational zone (checkout)
- soft amber: existing furniture's required customer/staff access; **not forbidden to other access zones**
- red existing access: the selected *body* is intruding into it
- green selected body: placement passes all rules
- red selected body/access: placement fails one or more rules

A red selected object can be invalid for reasons other than another furniture's access (e.g. wall, build boundary or checkout). Do not falsely imply that all red previews indicate an access collision.

## Fixed clearances

`LayoutClearanceZone` remains the explicit policy component for checkout/operational zones, using the same `LayoutFootprintUtility` test.

Actual checkout scene values:

- `AreaType SalesFloor`
- center `(0,.75,-2)`
- half extents `(.8,.75,1.55)`
- blocks both furniture body and furniture access

It stays purple and unchanged. Existing furniture access quads are **not** added as LayoutClearanceZone components on shelf prefabs.

## Build areas

`StoreBuildArea` owns available floors and footprint inclusion. Relevant APIs:

- `TryGetPlacementSurface`, `IsFootprintInside`, `IsBuildFloorCollider`
- `TryGetAvailableBounds`, `CollectAvailableFloorBounds`

LayoutWorldVisualizer displays each individual available floor zone. A future camera pass can query area context.

## Navigation

- fixed authored fixtures (CheckoutCounter, CustomerBasketStation) use `FixedNavigationBlocker + NavMeshModifier` and `Not Walkable` geometry
- movable fixtures use `FurnitureNavigationBlocker + NavMeshModifierVolume` for the semantic body
- customer recovery checks NavMesh same-agent position, vertical tolerance and capsule clearance
- ceilings excluded from LayoutCamera and NavMeshSurface
- on dirty Layout Mode exit, NavMesh rebuilds once

Lesson 117 introduces no NavMesh or collider changes.

## Save and checkout roadmap

- Save schema `2`; file `little_market_dev_v2_save.json`
- Save/load still disallows active customers until customer-carried stock and visit state are deliberately persisted
- Lesson 117 adds no saved fields: visuals are regenerated from FurnitureDefinition on entering Layout Mode
- Desired tactile checkout: unload → scan → bag → card/cash payment → leave
- Future checkout work: cash drawer/change, tender keypad, PLU, upgrades, CheckoutRegistry, multiple placeable checkouts, self-checkouts

## Forward roadmap

### Lesson 118 — Grid / Precision Controls & Modular Asset Standard

- independent Grid Snap ON/OFF, visual grid independent of actual snapping
- free placement and explicit Layout Mode status/control
- `1u = 1m`, .25-m asset modules where practical
- floor-root pivot, scale one, local `-Z` functional front
- preserve actual current scene snap value `.1` unless intentionally changed

### Lesson 119 — Magnetic Socket Snapping

- compatible sockets, zero-gap adjacency, attach/detach hysteresis, alignment guides
- independent from grid snap

### Later — Layout Camera & Feel Pass

- sales-floor-vs-backroom entry context
- fixed useful editing distance, manual full-store zoom-out
- exterior/outdoor layout policy
- pan/orbit/zoom easing, object-follow smoothing, optional edge pan

## Permanent development/teaching rules

- GitHub `main` is the current source of truth; inspect before each lesson
- read this document and current complete relevant scripts, prefabs, definitions and scene
- never invent scene names, paths, inspector values or existing code
- preserve user-authored values, especially scroll sensitivity 2 and grid snap .1
- keep validation, navigation and visualization separate
- keep old access-sharing behavior unchanged unless explicitly reconsidered
- modular scripts; full replacement code for substantial changes, anchored edits for tiny ones
- explain *why*, exact Inspector setup, regression tests, suggested commit and next roadmap
- finish every lesson with complete revised `SYSTEM_ARCHITECTURE.md` and fresh-chat continuation `.md`
