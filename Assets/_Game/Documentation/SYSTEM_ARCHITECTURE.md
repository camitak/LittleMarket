# Little Market — System Architecture

Current architecture after **Lesson 127 — Smooth Local-Region Store Design Camera**.

## Verified source of truth

Lesson 127 was designed after verifying GitHub `main` at:

`9f5feaf89c095fc4080f841a2a00b8683b1563fd`

Commit:

`Lesson 126 complete - structured and free placement modes`

Re-verify newest `main` before every future lesson.

## Permanent lesson-format rule

The full lesson implementation belongs in chat:
- code
- explanation
- Inspector setup
- UI setup
- tests

Only the required handoff `.md` files are created separately unless the user explicitly asks for another artifact.

## Core principles

> The building provides space; the player decides how the grocery operates inside that space.

> The clock describes the store's day; it does not grant permission to manage the store.

Store Design Mode is a central cozy-game loop.

## Lesson 127 camera goal

The Store Design camera should feel:
- smooth but responsive
- locally focused
- scalable as the store expands
- predictable when entering from different areas
- capable of deliberate target framing later

The camera should NOT automatically zoom farther and farther out just because more store floor has been unlocked.

## Local working-region entry

`StoreBuildArea` now exposes:

`TryGetWorkingZoneBounds(Vector3 worldPoint, out Bounds bounds, out StoreAreaType areaType)`

It considers only currently available build zones.

Selection rules:
1. if the player is inside an available build zone, that local zone wins
2. if the player is not inside any zone, use the horizontally nearest available zone
3. ties are broken by distance to zone center

This means:
- entering Store Design Mode from the Sales Floor focuses the local Sales Floor build zone
- entering from the Backroom focuses the local Backroom build zone
- entering from a purchased expansion focuses that expansion zone
- future store growth does not cause normal entry to frame all same-type floor space

Current base zones:
- Sales Floor: approximately `12 x 10 m`, centered around `(2, 0)` on XZ
- Backroom: approximately `4 x 10 m`, centered around `(-6, 0)` on XZ

## Working distance

`LayoutCameraController.ActivateForWorkingBounds(...)`:
- uses the selected local zone center as camera pivot
- uses fixed `workingDistance`
- does NOT calculate a full-zone fit

Initial value:
- Working Distance `9`

This is intentionally much closer than the previous full-SalesFloor framing distance.

Player can:
- zoom out manually for broader context
- zoom in for detail
- pan to adjacent regions

## Future target framing API

`LayoutCameraController` keeps/introduces:
- `ActivateForBounds(Bounds)` — immediate full fit when activating
- `FocusWorkingBounds(Bounds)` — smooth focus at working distance
- `FrameBounds(Bounds)` — smooth focus + fit target bounds

`FrameBounds()` is intended for future systems such as:
- selecting a locked expansion
- jump-to-object
- catalog placement focus
- copy/eyedrop focus if ever useful

Normal Store Design entry should use `ActivateForWorkingBounds`, not full fitting.

## Camera smoothing model

The camera separates:
- current state
- target state

State:
- pivot position
- yaw
- pitch
- distance

Input updates target state.
Current state smoothly approaches the target.

This produces polish without inertial overshoot.

### Pan

WASD moves the target pivot.

Current pivot uses `Vector3.SmoothDamp`.

Initial:
- Pan Speed `7`
- Boost `2`
- Pan Smooth Time `.08`

Shift supports left or right Shift.

### Orbit

MMB mouse movement updates target yaw/pitch.

Current yaw/pitch use SmoothDamp / SmoothDampAngle.

Initial:
- Yaw sensitivity `.12`
- Pitch sensitivity `.10`
- Orbit Smooth Time `.06`

No rotational inertia is added; the camera simply catches the input target smoothly.

### Zoom

Wheel changes target distance.
Current distance smooths toward it.

Initial:
- Scroll Zoom Sensitivity `2`
- Zoom Smooth Time `.10`

The user's tuned zoom input scale `2` is preserved.

### Time source

Camera smoothing uses `Time.unscaledDeltaTime`.

The store clock being suspended does not affect camera feel.

## Current LayoutCameraController Inspector values

Camera:
- Layout Camera = existing child LayoutCamera

Working View:
- Working Distance `9`

Overview / Target Framing:
- Overview Pitch `62`
- Overview Yaw `0`
- Overview Padding `1.08`
- Fallback Distance `4`

Orbit Limits:
- Minimum Pitch `15`
- Maximum Pitch `88`
- Minimum Distance `4`
- Maximum Distance `32`

Movement:
- Pan Speed `7`
- Boost Multiplier `2`
- Pan Smooth Time `.08`

Mouse:
- Yaw Sensitivity `.12`
- Pitch Sensitivity `.10`
- Scroll Zoom Sensitivity `2`
- Orbit Smooth Time `.06`
- Zoom Smooth Time `.10`

## Placement behavior

Unchanged from Lesson 126.

Structured:
- `.5m` grid
- magnetic sockets
- cardinal rotation

Free:
- continuous floor position
- no magnet
- continuous R/T rotation

G:
- persistent default

Ctrl:
- temporary inverse

Camera smoothing must not affect placement validation or selected-furniture transforms.

## Placement validation

Unchanged:
1. body inside permitted build area
2. access inside permitted build area
3. body vs fixed clearance
4. access vs fixed clearance
5. body vs other furniture access
6. body vs blocking colliders
7. access vs blocking colliders

## Save / expansion rules

Unchanged:
- time/customer state does not gate saving or expansion purchase
- live customer inventory is reconciled in save snapshots
- end-of-day completion is idempotent
- save schema remains v2

## Deliberately not implemented in Lesson 127

- edge scrolling
- camera following the selected furniture
- camera bounds/clamping
- minimap
- top-down toggle
- exterior build zones
- world-space expansion selection

These should only be added if playtesting demonstrates a need.

## Roadmap

### Lesson 128 — In-Mode Build Catalog
Reuse the existing FurnitureCatalog/FurnitureShop authority from Store Design Mode.

Goals:
- open catalog directly while designing
- categories
- preview-first purchasing
- no duplicate purchase/business logic
- computer remains alternate entry point

### Lesson 129 — Decorations Foundation

### Lesson 130 — World-Space Expansion Preview & Purchase

Then:
- copy/eyedrop
- repeat placement
- selling/refunds
- variants/colors
- Ambience
- autosave
- exact carry-state persistence
- advanced fixtures
- optional camera refinements if playtesting requests them

## Permanent engineering/UX rules

- GitHub main first
- exact current source and scene
- preserve tuned Inspector values
- lesson implementation stays entirely in chat
- only handoff `.md` files separate
- research meaningful player-facing UX/mechanics
- player playtesting drives iteration
- time is simulation, not permission
- save/load must be safe/idempotent
- one business authority
- no color-only critical information
- placement/navigation/presentation/persistence remain separate
- avoid god classes
- end lessons with architecture + continuation `.md`
