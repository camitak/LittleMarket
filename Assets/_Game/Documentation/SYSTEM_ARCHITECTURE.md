# Little Market — System Architecture

Current architecture after **Lesson 116 — Mutual Furniture Access Clearance**.

## Baseline

Lesson 116 is designed from GitHub `main` commit:

`c075ece13af8a54f57741b0ebbaf509abb2201a4`

Commit message:

`Lesson 115 complete - grid and footprint visual language`

GitHub `main` remains the source of truth.

Before every future lesson, verify the user's actual latest push rather than assuming this commit is still current.

## Core game principle

> The building provides space; the player decides how the grocery operates inside that space.

Little Market is a cozy/cute first-person grocery-store management game.

Placement is a major design pillar and should not be rushed.

## User-authored camera value

The actual Lesson-115 pushed scene has:

`LayoutCameraRig -> LayoutCameraController -> Scroll Zoom Sensitivity = 2`

This is intentional user tuning and must be preserved unless intentionally revisited during the dedicated camera-feel pass.

Current pushed camera values:

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

The current whole-sales-floor automatic overview is acceptable temporarily but is not the intended long-term camera contract.

Preferred future behavior:

- entering Layout Mode from the sales floor focuses a useful sales-floor working region
- entering Layout Mode from the backroom focuses the backroom
- startup framing uses a comfortable fixed editing distance rather than always fitting the complete growing area
- manual zoom-out remains available when the player wants a full-store view
- store growth must not automatically push the default camera farther and farther away
- future exterior/outdoor Layout Mode space should be treated as its own buildable/focus region
- exterior visibility should not mean exposing the entire world by default

Likely future architecture:

`StoreBuildArea / area-context query -> relevant build region -> camera focus center`

Do not implement this prematurely during precision/snapping lessons unless required.

## Layout Mode controls

Current controls:

- Left Mouse: select furniture / confirm placement
- Mouse movement: position selected furniture
- Middle Mouse drag: orbit / tilt
- Mouse wheel: zoom
- WASD: pan
- Shift + WASD: faster pan
- R/T: rotate furniture
- Right Mouse / Esc / Q: cancel
- L: exit Layout Mode

Cursor remains visible/unlocked during Layout Mode.

The first-person Crosshair remains hidden.

## Spatial architecture

Little Market deliberately keeps several spatial concepts separate.

### Physical body

The real furniture geometry and colliders.

Purpose:

- physical collision
- interaction raycasts
- world presence

### Navigation blocker

Defines where customers cannot navigate.

Fixed fixtures and movable furniture use different annotation strategies appropriate to their lifetime.

### Operational / access footprint

Defines space that must remain available for customers or store operation.

This space is not itself a physical obstacle.

Customers may move through it.

Furniture placement may be forbidden from occupying it.

Examples:

- shelf customer access
- refrigerated-display customer access
- storage-rack working access
- checkout queue/service clearance

These concepts must not be merged.

## FurnitureDefinition

`FurnitureDefinition` remains the authoritative data source for movable-furniture placement geometry.

Placement data:

- `PlacementArea`
- `BodyCenter`
- `BodyHalfExtents`
- `AccessCenter`
- `AccessHalfExtents`

The furniture prefab must not duplicate these values in a separate clearance component unless a genuinely different semantic zone is required.

### Current Standard Shelf definition

Body:

- Half Extents `(1.3, 1.05, 0.52)`
- Center `(0, 1.1, 0)`

Front access:

- Half Extents `(1.25, 0.75, 0.5)`
- Center `(0, 0.8, -1.02)`

Functional/front direction remains local `-Z`.

### Current Refrigerated Display definition

Uses the same placement body/access dimensions as Standard Shelf.

### Current Ambient Storage Rack definition

Placement area:

`Backroom`

Body:

- Half Extents `(1.22, 0.82, 0.3)`
- Center `(0, 0.8, 0)`

Access:

- Half Extents `(1.15, 0.75, 0.5)`
- Center `(0, 0.75, -0.8)`

### Current Chilled Storage Rack definition

Placement area:

`Backroom`

Uses the same body/access dimensions as Ambient Storage Rack.

## Lesson-116 mutual furniture clearance rule

Before Lesson 116, selected furniture validated:

- its body inside its build area
- its access footprint inside its build area
- body/access against fixed `LayoutClearanceZone`s
- body/access against physical colliders

However, an already-placed furniture's access footprint existed only in its `FurnitureDefinition`.

Therefore another furniture body could enter that already-placed access footprint.

Lesson 116 fixes this.

Every active `FurnitureIdentity` with a valid `FurnitureDefinition` now contributes an implicit operational access footprint during furniture placement.

The rule is:

`selected furniture BODY` must not overlap `other furniture ACCESS`.

This is evaluated semantically from the other furniture's definition rather than by adding physical colliders.

## Shared-aisle rule

Furniture access footprints are allowed to overlap one another.

This is intentional.

Two shelves facing each other should be able to share one customer aisle.

Therefore Lesson 116 does **not** enforce:

`ACCESS vs ACCESS = invalid`

Instead:

`BODY vs ACCESS = invalid`

This makes operational access a protected usable region without artificially doubling aisle requirements.

Physical furniture bodies remain independently protected by the existing collider validation.

## LayoutFootprintUtility

New static utility:

`Assets/_Game/Scripts/Store/Layout/LayoutFootprintUtility.cs`

Responsibility:

- perform horizontal oriented-rectangle overlap tests
- work with world centers, rotations and XZ half-extents
- know geometry only
- contain no furniture/checkout/gameplay policy

It uses a 2D Separating Axis Theorem test across the four horizontal axes of the two oriented footprints.

Consumers currently include:

- `LayoutClearanceZone`
- `FurniturePlacementController`

This avoids maintaining duplicate overlap mathematics in multiple gameplay systems.

## LayoutClearanceZone

`LayoutClearanceZone` remains the explicit semantic clearance system for fixed/world operational zones.

Public presentation properties:

- `AreaType`
- `Center`
- `HalfExtents`
- `BlocksFurnitureBody`
- `BlocksFurnitureAccess`

Its placement-policy responsibilities remain unchanged.

Lesson 116 changes only its internal overlap implementation so it delegates the generic horizontal footprint intersection test to `LayoutFootprintUtility`.

### CheckoutCounter clearance

Current scene values remain:

- Area Type: SalesFloor
- Center `(0, 0.75, -2.0)`
- Half Extents `(0.8, 0.75, 1.55)`
- Blocks Furniture Body: true
- Blocks Furniture Access: true

No Inspector changes are required in Lesson 116.

The checkout zone remains explicit because it is a fixed operational/service area rather than furniture-definition access data.

## FurniturePlacementController

Lives on `Player`.

Current serialized references:

- `PlayerInteraction`
- `InteractionUI`
- `StoreLayoutModeController`
- `StoreBuildArea`
- `FurniturePlacementVisualizer`

Current actual scene placement values:

- Selection Ray Distance `35`
- Preview Ray Distance `30`
- Grid Size `.1`
- Furniture Snap Distance `.35`
- Furniture Snap Gap `.01`
- Alignment Snap Distance `.3`

The actual scene value `gridSize = .1` is authoritative even though the C# field initializer currently uses `.5`.

Do not reset the Inspector value.

### Preview candidate collection

At preview start, `FurniturePlacementController` collects the active `FurnitureIdentity` objects once.

The collection is used for:

- existing provisional furniture snapping
- Lesson-116 furniture-access validation

The old internal name `snapCandidates` becomes `furnitureCandidates` because the collection now has broader responsibility.

### Placement validation order

Current validation covers:

1. selected body inside allowed build area
2. selected access inside allowed build area
3. selected body against explicit `LayoutClearanceZone`s
4. selected access against explicit `LayoutClearanceZone`s
5. selected body against access footprints of other furniture
6. selected body against physical colliders
7. selected access against physical colliders

The fifth rule is new in Lesson 116.

Existing access-vs-body collider validation remains.

Access-vs-access overlap remains allowed.

## Lesson-115 visual architecture

Placement validation remains the source of truth.

Visual feedback remains separated into dedicated presentation components.

### LayoutWorldVisualizer

Lives on `StoreSystems`.

Responsibilities:

- show a subtle floor grid over each currently available build zone
- show explicit `LayoutClearanceZone` footprints such as checkout queue/service space
- create/destroy/toggle runtime-only visual meshes
- never create colliders
- never change placement validation

References:

- `StoreSystems -> StoreLayoutModeController`
- `StoreSystems -> StoreBuildArea`

It displays available SalesFloor and Backroom zones.

The grid is generated independently for each build-zone floor bounds so L-shaped expansions do not create phantom grid space.

Prototype visual grid:

- visible grid size `.5 m`
- line width `.015 m`
- floor offset `.025 m`
- subtle pale cyan
- no physics
- no raycast behavior

The visual `.5 m` grid remains independent from the actual placement snap.

### FurniturePlacementVisualizer

Lives on `Player`.

Responsibilities:

- show selected furniture body footprint
- show selected furniture access/front footprint
- follow selected position/rotation
- communicate valid/invalid state
- hide on confirmation/cancel/exit

Current visual vocabulary:

- green body = valid selected furniture body
- amber access = required selected-furniture customer access
- red selected footprint = invalid placement
- pale cyan floor grid = dimensional reference
- translucent purple zone = explicit operational/service clearance

It owns no placement rules and no colliders.

Lesson 116 does not add persistent visual meshes for every furniture access zone.

The currently selected object's red/green state communicates the validation result.

A future contextual-conflict visualization may be added if hidden existing access zones prove confusing during playtesting.

### LayoutVisualUtility

Static rendering utility.

Responsibilities:

- transparent runtime materials
- reusable XZ quad
- floor-grid mesh generation

It remains independent from `LayoutFootprintUtility`.

`LayoutVisualUtility` = presentation geometry.

`LayoutFootprintUtility` = placement intersection mathematics.

Do not merge them.

## StoreBuildArea

Relevant methods:

- `TryGetPlacementSurface`
- `TryGetAvailableBounds`
- `IsFootprintInside`
- `IsBuildFloorCollider`
- `CollectAvailableFloorBounds`

`CollectAvailableFloorBounds` exposes individual currently available zone floor bounds for presentation systems.

Available build-zone data remains the authoritative definition of where furniture may be placed.

## Furniture snapping

Current snapping predates the future magnetic-socket architecture.

Current values:

- snap distance `.35`
- snap gap `.01`
- alignment distance `.3`

Current snapping is body-bounds based.

It remains unchanged by Lesson 116.

Lesson 118 will replace/augment this with explicit compatible magnetic sockets, zero-gap adjacency and hysteresis.

Do not prematurely merge Lesson-116 access clearance with future magnetic snapping.

## Navigation architecture

### Fixed fixtures

Use:

`FixedNavigationBlocker + NavMeshModifier`

Current fixed blocked geometry includes:

- CustomerBasketStation
- CheckoutCounter

Their collected geometry is marked `Not Walkable`.

### Movable furniture

Use:

`FurnitureNavigationBlocker + NavMeshModifierVolume`

The blocker uses semantic furniture body data.

It remains separate from customer-access footprints.

### Customer recovery

Customer recovery validates:

- matching NavMesh agent position
- vertical tolerance
- physical capsule clearance

### Ceiling

Ceilings are excluded from:

- LayoutCamera rendering
- NavMeshSurface collection

### Layout Mode exit

Navigation is marked dirty when furniture placement is confirmed.

NavMesh rebuilding occurs once on Layout Mode exit when required.

Lesson 116 does not change this behavior.

## PF_Shelf

Current prefab:

`Assets/_Game/Prefabs/Store/PF_Shelf.prefab`

Root scale remains:

`(1,1,1)`

Root contains the shelf identity/runtime components, sales shelf behavior and navigation blocker infrastructure.

Customer-facing shelf geometry and `Customer_Point` remain on local `-Z`.

The prefab does not gain a `LayoutClearanceZone` in Lesson 116.

Its operational access continues to come from `SO_Furniture_StandardShelf`.

## Modular production standard direction

The project direction remains:

- 1 Unity unit = 1 metre
- root transform scale `(1,1,1)`
- root pivot at floor level
- local `-Z` = functional/front direction
- target `.25m` modules where practical

These standards become an explicit focus in Lesson 117.

## Save system

Current save schema:

`2`

Filename:

`little_market_dev_v2_save.json`

Save/load still blocks while active customers exist because in-progress customer-carried inventory and visit state are not yet intentionally persisted.

Lesson 116 does not change persistence.

Furniture operational access remains derived from `FurnitureDefinition`, so no new save fields are required.

## Checkout future

Desired tactile checkout flow remains:

customer unloads -> player physically scans -> bags -> card/cash payment -> customer leaves

Future possibilities:

- cash drawer
- physical cash/change
- keypad tender/manual amount
- PLU entry later
- upgraded checkout terminals
- CheckoutRegistry
- multiple player-placeable checkouts
- self-checkouts

## Roadmap

### Lesson 117 — Grid / Precision Controls & Modular Asset Standard

Introduce:

- independent Grid Snap ON/OFF
- visual grid independent from snapping
- free precision placement
- Layout Mode snap status/control
- 1 Unity unit = 1 metre formal production standard
- target `.25m` asset modules where practical
- floor-level root pivot
- root scale `(1,1,1)`
- local `-Z` functional/front convention

The actual current scene's `.1` snap step remains preserved until this lesson intentionally changes the interaction model.

### Lesson 118 — Magnetic Socket Snapping

Introduce:

- explicit compatible sockets
- zero-gap adjacency
- attach/detach hysteresis
- alignment guides
- grid snapping and magnetic snapping as independent systems

### Later — Layout Camera & Feel Pass

After the main precision/snapping contract is stable:

- area-aware entry focus
- sales floor vs backroom context
- comfortable fixed default editing distance
- manual full-store zoom-out
- exterior/outdoor focus-region policy
- pan acceleration/deceleration
- orbit tuning
- zoom easing
- object-follow smoothing
- optional edge-pan evaluation

## Permanent principles

- GitHub `main` is source of truth.
- Inspect the actual current scripts, prefabs, definitions and scene before every lesson.
- Never guess hierarchy, fields, paths or Inspector values.
- Preserve user-authored tuning unless intentionally changing it.
- Placement rules and placement presentation are separate responsibilities.
- Prefer semantic gameplay rules over incidental collider behavior.
- Furniture-definition access data is authoritative for movable-furniture operational space.
- Do not duplicate access dimensions into prefab clearance components without a separate semantic reason.
- Access-vs-access overlap is allowed so aisles may be shared.
- Furniture body vs another furniture's required access is forbidden.
- Navigation blockers and layout-clearance/access zones are different concepts.
- Grid visibility and grid snapping are independent concepts.
- Magnetic snapping and grid snapping are independent concepts.
- Avoid god classes.
- Use shared low-level utilities when multiple systems need identical geometric operations.
- The building provides space; the player decides how the grocery operates inside it.
- Layout Mode works while the store is open.
- Stocked furniture can move.
- 1 Unity unit = 1 metre.
- End every lesson with a complete updated architecture document and fresh-chat continuation prompt.