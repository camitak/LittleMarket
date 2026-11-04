# Little Market — System Architecture

Current architecture after **Lesson 126 — Structured & Free Placement Modes**.

## Verified source of truth

Lesson 126 was designed after verifying GitHub `main` at:

`03434f5f13b1b78ea088b885a187f6510ee50596`

Commit:

`Lesson 125 complete - safe end-of-day saves`

Before every future lesson, re-verify newest `main`.

## Permanent lesson-format rule

The user explicitly requested that implementation never be split across chat plus separate code/setup files.

From now on:
- complete lesson, code, explanation, Inspector/UI setup, and tests stay together in chat
- only required handoff `.md` files are generated separately
- no separate code/edit ZIPs unless explicitly requested

## Core principles

> The building provides space; the player decides how the grocery operates inside that space.

> The clock describes the store's day; it does not grant permission to manage the store.

Store Design Mode is a core cozy-game reward loop.

## Lesson 126 player-facing placement model

The old independent controls:
- G = Grid Snap
- Alt = bypass magnetic snap
- R/T = fixed 90-degree rotation

are replaced by one coherent concept.

### Structured placement

Structured mode means:
- `.5 m` grid position snapping
- magnetic compatible furniture sockets enabled
- R/T uses cardinal 90-degree world orientations

Best suited to:
- shelves
- refrigerated displays
- storage racks
- orderly fixture runs

### Free placement

Free mode means:
- raw continuous floor position
- magnetic socket assistance disabled
- holding R/T rotates continuously

Initial fine rotation speed:
- `90 degrees / second`

Best suited to:
- future decorations
- plants
- signs
- deliberately angled layouts
- fine positioning

## Controls

`G`
- toggles the persistent default between Structured and Free
- ignored while Ctrl is held, to avoid accidentally changing the default while using a temporary override

`Left Ctrl` / `Right Ctrl` held
- temporarily inverts the current default

Examples:

Default Structured:
- normal = Structured
- hold Ctrl = Free
- release Ctrl = Structured

Default Free:
- normal = Free
- hold Ctrl = Structured
- release Ctrl = Free

`Alt` no longer has a placement-mode role.

## Rotation behavior

### Structured

R/T moves to the next/previous cardinal WORLD orientation.

Examples:
- 0 -> R -> 90
- 90 -> R -> 180
- 180 -> T -> 90

Important free-angle bridge:
- if Free rotation leaves an item at 17 degrees
- returning to Structured does NOT immediately change 17 degrees
- first R -> 90
- first T -> 0

This preserves player-authored decorative angles while still providing a deliberate way back to structured orientation.

### Free

Hold:
- R = positive continuous yaw
- T = negative continuous yaw

Rotation uses frame-rate independent `Time.deltaTime`.

## Position behavior

Structured:
- cursor floor position is quantized to `GridSize`
- current `GridSize = .5 m`

Free:
- raw floor hit position

Switching modes can therefore move the preview between raw cursor position and nearest grid position. This is intentional and provides immediate visual feedback about the active assistance mode.

## Magnetic behavior

Magnetic sockets participate only while Structured mode is active.

Free mode:
- clears active magnetic snap
- does not capture or retain sockets

Structured mode:
- uses existing capture/release/facing logic unchanged

Current values:
- Magnetic enabled = true
- capture = `.30`
- release = `.55`
- max facing = `15°`

## Compatibility / serialization

The serialized field formerly named:

`gridSnapEnabled`

is migrated with:

`[FormerlySerializedAs("gridSnapEnabled")]`

to:

`structuredPlacementByDefault`

This preserves the current Inspector default (`true`) without manually rebuilding the component.

Placement-mode preference remains runtime/session state and is not added to save schema.

Save schema remains version 2.

## Placement validation

Unchanged:

1. body inside permitted build area
2. access inside permitted build area
3. body vs explicit clearance
4. access vs explicit clearance
5. selected body vs other furniture access
6. body vs blocking colliders
7. access vs blocking colliders

Body vs other furniture access is forbidden.
Access vs access is allowed.

## Layout status UI

The existing:

`GridSnapStatusText`

is renamed player-facing to:

`PlacementModeStatusText`

Hierarchy remains under the Layout/Store Design frame.

RectTransform:
- anchors `(0,1)` to `(0,1)`
- pivot `(0,1)`
- anchored position `(28,-28)`
- size `720 x 28`

TMP:
- font size `18`
- left / middle
- Raycast Target OFF
- color `#CFF7FADC`
- RGBA `(207,247,250,220)`

Runtime examples:

`STRUCTURED · GRID 0.50 m`

`STRUCTURED · GRID 0.50 m · MAGNET`

`FREE · PRECISION`

`FREE · PRECISION · CTRL TEMPORARY`

`STRUCTURED · GRID 0.50 m · CTRL TEMPORARY`

Status communicates mode with text, not color alone.

## Controls panel

Panel stays:
- bottom-right
- anchor/pivot `(1,0)`
- anchored position `(-28,28)`
- `360 x 190`
- background `#FFF9EEDC`
- RGBA `(255,249,238,220)`
- Raycast OFF

ControlsText:
- full stretch with `16 px` padding
- font `17`
- left / top
- color `#594640FF`
- RGBA `(89,70,64,255)`
- Raycast OFF

Text:

STORE DESIGN CONTROLS
LMB   Select / Place
R / T   Rotate      G   Placement Mode
Ctrl   Temporary Opposite Mode
MMB   Orbit         Wheel   Zoom
WASD   Pan          Shift   Faster
RMB / Esc   Cancel  L   Exit

## Current placement Inspector values

Preserve:
- Selection Ray Distance `35`
- Preview Ray Distance `30`
- Body Contact Tolerance `.01`
- Structured Placement By Default `true`
- Grid Size `.5`
- Free Rotation Degrees Per Second `90`
- Magnetic Snap Enabled `true`
- Socket Capture Distance `.30`
- Socket Release Distance `.55`
- Maximum Socket Facing Angle `15`
- Outline Width `.01` (scene value; do not reset to source initializer)

## Camera values — scene source of truth

Untouched in Lesson 126:
- Overview Pitch 62
- Overview Yaw 0
- Overview Padding 1.08
- Fallback Distance 4
- Min Pitch 15
- Max Pitch 88
- Min Distance 4
- Max Distance 32
- Pan 7
- Boost 2
- Yaw .12
- Pitch .10
- Scroll Zoom Sensitivity 2

## Save / expansion rules

Unchanged from Lessons 124–125.

Time/customer state does not gate:
- layout
- expansion purchase
- saving

End-of-day completion is idempotent via StoreReportHistory day presence.

## Roadmap

### Lesson 127 — Layout Camera Feel
Implement:
- acceleration/deceleration for WASD pan
- orbit smoothing
- zoom smoothing
- useful working-region startup focus
- preserve Scroll Zoom Sensitivity 2 as user-tuned input scale
- prepare frame-target API for future expansions

### Lesson 128 — In-Mode Build Catalog
Reuse FurnitureCatalog/FurnitureShop.

### Lesson 129 — Decorations Foundation

### Lesson 130 — World-Space Expansion Preview & Purchase

Then:
- copy/eyedrop
- repeat placement
- selling/refunds
- variants
- Ambience
- autosave strategy
- exact player carry-state persistence
- advanced fixture/socket semantics

## Permanent engineering/UX rules

- GitHub main first
- inspect exact source and scene
- preserve scene-authored tuning
- lesson implementation stays together in chat
- only handoff `.md` files separate
- exact UI instructions and HEX colors
- research important player-facing UX
- player playtesting drives iteration
- time is simulation, not permission
- save/load transitions must be safe/idempotent
- one business authority
- no color-only critical state
- placement/navigation/presentation/persistence remain separate
- avoid god classes
- end lesson with architecture + continuation `.md`
