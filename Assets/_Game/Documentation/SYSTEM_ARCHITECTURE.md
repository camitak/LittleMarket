# Little Market — System Architecture

Current proposed architecture after **Lesson 121 — Magnetic Socket Snapping**.

## Verified source of truth

Lesson 121 was designed after inspecting GitHub `main` at:

`d59fc0d1a094c1be0879125862b1113836291a2b`

Commit:

`Lesson 120 complete - readable grid snap and permanent UX rules`

Re-verify GitHub `main` before every future lesson.

## Stack

- Unity 6.3
- C#
- JetBrains Rider
- URP
- Unity Input System
- Unity AI Navigation 2.0.14
- Repository `camitak/LittleMarket`
- branch `main`
- scene `Assets/Scenes/Market.unity`

## Core design principle

> The building provides space; the player decides how the grocery operates inside that space.

Little Market is a cozy first-person grocery-management game. Placement is a major design pillar.

## Permanent UX/UI rules

### UI implementation detail

Every UI lesson must provide:

- exact Hierarchy path
- exact GameObject names
- anchors
- pivot
- anchored position
- width/height or offsets
- font size
- alignment
- Raycast Target
- references
- exact HEX colors
- RGBA when helpful
- UX rationale

Never make the user derive or search for a color.

### UX research

Before locking meaningful player-facing UX decisions:

- research current game UX/UI best practices
- inspect relevant established game/tool patterns
- prefer official engine/game/platform/accessibility sources
- adapt the principle to Little Market
- treat user playtesting as evidence and iterate when the real interaction feels wrong

### Accessibility

Do not rely on color alone for essential state.

Use text, geometry, iconography, animation or sound in combination where appropriate.

## Layout controls after Lesson 121

- LMB: select / place
- R / T: rotate
- G: Grid Snap ON/OFF
- Alt: temporarily bypass Magnetic Snap
- MMB drag: orbit / tilt
- Wheel: zoom
- WASD: pan
- Shift + WASD: faster pan
- RMB / Esc / Q: cancel
- L: exit

## Placement constraints remain layered

### Physical body
Furniture/object colliders and authored body footprint.

### Navigation blocker
Customer non-walkable body volume.

### Operational access
Customer/staff usage space. Navigable, but furniture bodies may not occupy it.

These remain independent.

## Grid Snap

FurniturePlacementController owns Grid Snap.

- enabled by default
- Grid Size `.5 m`

Grid ON:

- floor X/Z quantize to `.5m`
- then Magnetic Snap may override the base grid position when a compatible socket is captured
- validation runs normally

Grid OFF:

- raw floor point
- Magnetic Snap may still capture a compatible socket
- validation runs normally

Grid Snap and Magnetic Snap are intentionally independent.

## Magnetic Socket Snapping — Lesson 121

Lesson 121 retires the old generic body-bounds furniture snapping fields/methods:

- `furnitureSnapDistance`
- `furnitureSnapGap`
- `alignmentSnapDistance`
- `ApplyFurnitureSnap`
- `EvaluateSnapCandidate`
- `GetHorizontalHalfExtents`

The replacement is explicit semantic sockets.

### FurnitureSnapSocket

New component:

`Assets/_Game/Scripts/Store/Furniture/FurnitureSnapSocket.cs`

Current enums:

- `FurnitureSnapSocketType.RunEdge`
- `FurnitureSnapSocketSide.Left`
- `FurnitureSnapSocketSide.Right`

Compatibility:

- same SocketType
- opposite Side
- different FurnitureIdentity
- same StoreAreaType
- socket outward directions must face one another within the controller's facing-angle tolerance

A socket's Transform is meaningful data:

- position = exact attachment location
- forward = outward direction from the furniture edge

The component's editor gizmo uses:

- `#5FE3F0FF`
- RGBA `(95,227,240,255)`

### Current socket scope

Lesson 121 intentionally supports **side-to-side furniture runs only**.

Sales floor:

- Standard Shelf
- Refrigerated Display

Backroom:

- Ambient Storage Rack
- Chilled Storage Rack

Because compatibility is RunEdge-based and area-restricted:

- shelf ↔ shelf works
- shelf ↔ refrigerated display works
- refrigerated ↔ refrigerated works
- ambient ↔ chilled rack works
- sales-floor furniture cannot magnetically join backroom furniture

No front/back sockets yet.

Back-to-back gondolas/double-sided fixtures need a deliberate future access policy and should not emerge accidentally.

### Socket authoring positions

#### PF_Shelf

Direct children of root:

`Snap_Left`
- local position `(-1.30, 0, 0)`
- local rotation `(0, -90, 0)`
- scale `(1,1,1)`
- Type `RunEdge`
- Side `Left`

`Snap_Right`
- local position `(1.30, 0, 0)`
- local rotation `(0, 90, 0)`
- scale `(1,1,1)`
- Type `RunEdge`
- Side `Right`

#### PF_RefrigeratedDisplay

Same as PF_Shelf:

- X `-1.30 / 1.30`
- Y `0`
- Z `0`
- outward rotations `-90 / 90`

#### PF_StorageRack_Ambient

`Snap_Left`
- local position `(-1.22, 0, 0)`
- rotation `(0,-90,0)`

`Snap_Right`
- local position `(1.22,0,0)`
- rotation `(0,90,0)`

#### PF_StorageRack_Chilled

Same rack values:

- X `-1.22 / 1.22`
- Y `0`
- Z `0`

### Magnetic controller values

Player -> FurniturePlacementController:

Placement:
- Selection Ray Distance `35`
- Preview Ray Distance `30`
- Body Contact Tolerance `.01`

Grid Snap:
- Enabled `true`
- Grid Size `.5`

Magnetic Snap:
- Magnetic Snap Enabled `true`
- Socket Capture Distance `.30`
- Socket Release Distance `.55`
- Maximum Socket Facing Angle `15`

Capture/release hysteresis prevents snap flicker:

- enter magnet at `.30m`
- remain magnetized until pointer's unsnapped/base target exceeds `.55m`

### Player control

Magnetic Snap never rotates furniture automatically.

Player controls orientation using R/T.

Holding either Alt key temporarily bypasses Magnetic Snap. This allows precision escape without toggling Grid Snap or changing persistent state.

### Priority order

When moving furniture:

1. raycast permitted floor
2. apply Grid Snap if enabled
3. attempt/retain compatible Magnetic Snap unless Alt is held
4. move preview
5. validate all placement rules
6. render feedback

This means a captured semantic socket has higher positional priority than the base grid. This override is explicit because the UI and world guide announce MAGNET state.

## Magnetic feedback

FurniturePlacementVisualizer remains the selected-object presentation component and gains a magnetic snap guide.

When captured:

- a diamond appears at the target socket
- top-left status appends `MAGNET`
- the furniture physically locks to the socket

This avoids color-only feedback.

Guide valid color:

- `#5FE3F0E6`
- RGBA `(95,227,240,230)`

Guide invalid color:

- `#C46B6BE6`
- RGBA `(196,107,107,230)`

Guide values:

- Size `.18`
- Width `.035`
- Surface Offset `.085`

## Layout HUD

### GridSnapStatusText

Path:

`GameUI -> LayoutModeFrame -> GridSnapStatusText`

Resize Width from `340` to `520` so MAGNET state fits.

Keep:

- Anchor Min `(0,1)`
- Anchor Max `(0,1)`
- Pivot `(0,1)`
- Anchored Position `(28,-28)`
- Height `28`
- Font Size `18`
- Alignment Left / Middle
- Raycast Target Off
- Color `#CFF7FADC`
- RGBA `(207,247,250,220)`

Runtime examples:

- `GRID SNAP ON · 0.50 m`
- `GRID SNAP OFF · FREE`
- `GRID SNAP ON · 0.50 m · MAGNET`
- `GRID SNAP OFF · FREE · MAGNET`

### LayoutControlsPanel

Path:

`GameUI -> LayoutModeFrame -> LayoutControlsPanel`

Keep:

- bottom-right
- position `(-28,28)`
- size `360 x 190`
- Raycast Target Off
- background `#FFF9EEDC`
- text `#594640FF`

ControlsText must include:

`Alt   Bypass Magnet`

No new color is required.

## Contact tolerance

Exact socket alignment intentionally places authored body edges against one another.

Physics overlap queries can treat exact contact as an overlap, so FurniturePlacementController now shrinks only the selected BODY collider-check half extents horizontally by `.01m` during broad collision validation.

Important:

- authored body dimensions are unchanged
- build-area containment uses full body dimensions
- body-vs-access SAT uses full body dimensions
- clearance-zone checks use full body dimensions
- only the body Physics.OverlapBox broad collision test receives the small contact tolerance

This permits clean surface contact without redefining furniture geometry.

## Furniture placement validation

Still:

1. body inside build area
2. access inside build area
3. body vs LayoutClearanceZone
4. access vs LayoutClearanceZone
5. selected body vs other furniture access
6. body vs colliders
7. access vs colliders

Body vs another furniture access = forbidden.

Access vs access = allowed/shared aisle.

## Existing visual language

- Grid `#CFF7FA26`
- Fixed clearance `#977EFF42`
- Valid body `#6FCF9766`
- Invalid body `#C46B6B80`
- Selected access `#F0C96A66`
- Existing access `#F0C96A30`
- Existing access conflict `#C46B6B96`
- Magnetic guide valid `#5FE3F0E6`
- Magnetic guide invalid `#C46B6BE6`

Important states are not color-only.

## FurnitureDefinition

Remains authoritative for:

- PlacementArea
- BodyCenter
- BodyHalfExtents
- AccessCenter
- AccessHalfExtents

Sockets are prefab-authored relational attachment points, not a replacement for FurnitureDefinition geometry.

## Modular asset standard

- 1 Unity unit = 1 metre
- root `(0,0,0)`
- root rotation identity
- root scale `(1,1,1)`
- floor-level root
- local `-Z` functional/front
- prefer `.25m` modules/socket positions where practical
- authored gameplay dimensions may use necessary exceptions
- socket transforms are authored relative to scale-one furniture roots

## Camera baseline

Preserve:

- Overview Pitch `62`
- Overview Yaw `0`
- Overview Padding `1.08`
- Fallback Distance `4`
- Min Pitch `15`
- Max Pitch `88`
- Min Distance `4`
- Max Distance `32`
- Pan `7`
- Boost `2`
- Yaw `.12`
- Pitch `.10`
- Scroll Zoom Sensitivity **2**

## Navigation

Unchanged.

- fixed fixtures: FixedNavigationBlocker + NavMeshModifier
- movable furniture: FurnitureNavigationBlocker + NavMeshModifierVolume
- access spaces remain navigable
- dirty Layout Mode rebuilds once on exit

## Save

- schema `2`
- `little_market_dev_v2_save.json`

Sockets are prefab configuration and require no save-schema change.

Furniture transforms continue to persist as before.

## Forward roadmap

### Lesson 122 — Magnetic Snap Feel & Alignment Validation

Do not immediately add more socket types.

First playtest Lesson 121:

- capture strength
- release feel
- Alt bypass
- snap stability while rotating
- shelf/refrigerator mixed runs
- rack runs
- exact-contact validity
- visual clarity of the diamond/MAGNET status

Tune capture/release before expanding the system.

If the feel is good, then evaluate:

- back-to-back/double-sided fixture policy
- endcaps
- wall fixtures
- alignment/socket previews before capture
- optional sound/haptic feedback later

### Later — Layout Camera & Feel Pass

- sales-floor/backroom focus context
- fixed useful editing distance
- manual full-store overview
- exterior build/focus regions
- easing/smoothing
- selected-object follow
- optional edge pan

## Permanent engineering/teaching rules

- GitHub `main` is source of truth
- inspect actual current project before every lesson
- preserve user-authored tuning
- search current game UX best practices for meaningful player-facing UX decisions
- give exact UI setup details
- always give exact HEX for any color the user must set
- user playtest feedback is evidence
- protect the center from persistent clutter
- informational HUD must not intercept pointer input
- do not communicate essential state only through color
- Grid Snap and Magnetic Snap are independent
- Magnetic Snap must be predictable and escapable
- placement, visualization, navigation and persistence remain separate
- FurnitureDefinition remains spatial geometry authority
- socket transforms define relational attachment semantics
- avoid god classes
- end every lesson with complete architecture and fresh-chat continuation files
