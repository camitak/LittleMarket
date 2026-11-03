# Little Market — System Architecture

Current proposed architecture after **Lesson 120 — Grid Snap Readability & Permanent UX Rules**.

## Verified source of truth

Lesson 120 was designed after inspecting GitHub `main` at:

`1777dc6c1c258502f389efa4baaa62425bb8786a`

Commit:

`Lesson 119 complete - layout HUD hierarchy and control legend`

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

## Permanent project UX/UI rules

These are permanent project rules and must be carried into every fresh-chat continuation prompt.

### UI implementation detail rule

Whenever a lesson asks the user to create or edit UI, provide:

- exact Hierarchy path
- exact GameObject names
- exact RectTransform anchors
- exact pivot
- exact anchored position
- exact width/height or stretch offsets
- font size
- alignment
- Raycast Target state
- required component references
- exact color HEX values
- RGBA values when useful
- explanation of why that layout is being chosen

Never tell the user to choose/find an approximate color without also giving the intended HEX.

### UX research rule

Before making or locking in a meaningful player-facing UX decision, search current game-UX/game-interface best practices and relevant examples from established games/tools.

Prefer:

- official engine/game documentation
- platform design guidance
- accessibility guidance
- reputable game UX sources

Do not copy another game mechanically. Use research to validate the principle, then adapt it to Little Market.

User playtest feedback outranks theory when the actual interaction clearly feels wrong. Treat that feedback as evidence and iterate.

### Accessibility rule

Do not rely on color alone for essential state.

Pair important color changes with at least one of:

- text
- shape
- icon
- spatial change
- animation
- sound where appropriate

Maintain readable contrast and avoid unnecessary HUD clutter.

## Layout Mode controls

- LMB: select / place
- R / T: rotate
- G: Grid Snap ON/OFF
- MMB drag: orbit / tilt
- Wheel: zoom
- WASD: pan
- Shift + WASD: faster pan
- RMB / Esc / Q: cancel
- L: exit

## Lesson 120 — Grid Snap readability

The previous `.1 m` snap interval felt effectively free in playtesting.

Lesson 120 changes the default/current Grid Snap interval to:

`0.5 m`

Reasoning:

- the visible floor grid is already `.5 m`
- a snapped mode should feel perceptibly different from unrestricted placement
- matching the visible reference grid to the snap interval makes the system predictable
- Grid OFF remains the precision/free-placement mode
- future magnetic sockets will handle intentional furniture-to-furniture adjacency

This aligns with established build-mode patterns where grid placement and off-grid/free placement are distinct modes.

### Grid Snap ON

- floor target quantizes X/Z to `.5 m`
- visible `.5 m` grid predicts the base snap locations
- the existing provisional body/alignment furniture assist still runs near other furniture until replaced by explicit socket snapping
- placement validation remains unchanged

### Grid Snap OFF

- raw permitted floor position
- old provisional body/alignment assist bypassed
- normal validation remains active

### Important temporary caveat

The old `ApplyFurnitureSnap` system can still move a Grid-ON object away from the exact `.5 m` root point when it detects nearby furniture.

This is temporary compatibility behavior so shelf adjacency is not lost before socket snapping exists.

Lesson 121 should replace the architectural role of that provisional assist with explicit magnetic sockets.

## Current grid values after Lesson 120

### Player -> FurniturePlacementController

- Grid Snap Enabled `true`
- **Grid Size `.5`**
- Furniture Snap Distance `.35`
- Furniture Snap Gap `.01`
- Alignment Snap Distance `.3`

The source initializer should also be `.5f`, matching the scene.

### StoreSystems -> LayoutWorldVisualizer

- Visible Grid Size `.5`
- Grid Line Width `.015`
- Grid Surface Offset `.025`
- Grid Color `#CFF7FA26`
    - RGBA `(207, 247, 250, 38)`

The visual grid and snapping remain conceptually separate systems, even though they intentionally use the same interval now.

If a future fine snap option is added, it must be communicated clearly rather than silently using an invisible subdivision.

## Layout Mode HUD after Lesson 119

### Top-left Grid status

`GameUI -> LayoutModeFrame -> GridSnapStatusText`

Runtime states after Lesson 120:

- `GRID SNAP ON · 0.50 m`
- `GRID SNAP OFF · FREE`

Current text color:

- `#CFF7FADC`
- RGBA `(207, 247, 250, 220)`

Raycast Target OFF.

### Bottom-right controls

`GameUI -> LayoutModeFrame -> LayoutControlsPanel`

Current verified values:

- Anchor Min `(1,0)`
- Anchor Max `(1,0)`
- Pivot `(1,0)`
- Anchored Position `(-28,28)`
- Size `360 x 190`
- Raycast Target OFF

Panel color:

- `#FFF9EEDC`
- RGBA `(255,249,238,220)`

`ControlsText`:

- stretch inside panel with 16 px padding
- Font Size `17`
- Raycast Target OFF
- text color `#594640FF`
- RGBA `(89,70,64,255)`

### ClockPanel

`GameUI -> ClockPanel`

Hidden during Layout Mode and restored to its previous active state on exit.

This remains appropriate because Layout Mode suspends StoreClock.

Money remains visible because furniture purchases can occur during Layout Mode.

### InteractionPrompt

Center-lower generic prompt is reserved for contextual information only:

- hover movable furniture
- purchase confirmation/action
- invalid placement explanation

It is not a permanent control legend.

## Furniture placement validation

Unchanged:

1. body inside permitted build area
2. access inside permitted build area
3. body vs explicit LayoutClearanceZone
4. access vs explicit LayoutClearanceZone
5. body vs another furniture's access
6. body vs colliders
7. access vs colliders

Body vs other access = forbidden.

Access vs access = allowed.

## Visual language

- cyan grid `#CFF7FA26`
- fixed operational clearance purple `#977EFF42`
- selected valid body green `#6FCF97`
- existing/valid access amber family
- conflicts/invalid red family

Current existing furniture access:

- amber `#F0C96A30`
- conflict red `#C46B6B96`

Do not rely on these colors alone; placement state also uses text and geometry.

## FurnitureDefinition authority

FurnitureDefinition remains authoritative for:

- PlacementArea
- BodyCenter
- BodyHalfExtents
- AccessCenter
- AccessHalfExtents

Local `-Z` remains functional/front direction.

## Modular production standard

For new/revised placeable furniture:

- 1 Unity unit = 1 metre
- root position `(0,0,0)`
- root rotation identity
- root scale `(1,1,1)`
- floor-level root pivot
- local `-Z` = front/functional
- prefer `.25 m` modular dimensions/offsets/socket locations where practical
- child meshes may use local scaling
- avoid arbitrary root scaling

The `.25 m` asset-module guideline does not require the interactive Grid Snap interval to also be `.25 m`.

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

- fixed fixtures use FixedNavigationBlocker + NavMeshModifier
- movable furniture uses FurnitureNavigationBlocker + NavMeshModifierVolume
- operational access is navigable and not a NavMesh blocker
- dirty Layout Mode rebuilds NavMesh once on exit

## Save

- schema `2`
- `little_market_dev_v2_save.json`

Grid Snap remains a runtime Layout preference, not saved in Lesson 120.

## Forward roadmap

### Lesson 121 — Magnetic Socket Snapping

Build a real furniture-to-furniture snapping system independent from Grid Snap:

- explicit socket component/data
- compatible socket types
- side/end attachment semantics
- zero-gap adjacency
- capture radius
- attach/detach hysteresis
- visual alignment feedback
- Grid ON and Grid OFF both remain meaningful
- retire the old generic body-bounds snapping architecture once sockets replace it safely

### Later — Layout Camera & Feel Pass

- sales-floor/backroom entry context
- comfortable fixed editing distance
- manual full-store zoom-out
- exterior build/focus regions
- easing/smoothing
- selected-object follow
- optional edge pan

## Permanent engineering/teaching rules

- GitHub `main` is source of truth
- inspect actual project before every lesson
- preserve user-authored tuning
- search current game UX best practices for meaningful player-facing UX decisions
- give exact UI setup details
- always give HEX for any requested UI/gameplay color
- user playtest feedback is evidence and should drive iteration
- protect the center from persistent HUD clutter
- non-interactive HUD must not intercept pointer raycasts
- do not convey essential state by color alone
- keep placement, visualization and navigation separate
- FurnitureDefinition remains the movable furniture spatial authority
- avoid god classes
- end each lesson with complete architecture and fresh-chat continuation files
