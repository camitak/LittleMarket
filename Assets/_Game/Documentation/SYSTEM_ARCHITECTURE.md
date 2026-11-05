# Little Market — System Architecture

Current architecture after **Lesson 129 — Store Design Input Ownership & Placement-Area Focus**.

## Verified source of truth

Lesson 129 was designed after verifying GitHub `main` at:

`4c764027aa5a5f60f2efed59095d12597e0adf49`

Commit:

`Lesson 128 complete - in-mode build catalog`

Re-verify newest `main` before every future lesson.

## Why Lesson 129 became a correction/polish pass

Player testing of Lesson 128 found:
- scrolling the Build Catalog also zoomed the Store Design camera
- buying Backroom furniture while viewing Sales Floor created the preview in the correct area but left the camera elsewhere, making the purchase look broken
- LiberationSans SDF does not contain U+2713 CHECK MARK, producing a TMP fallback warning
- audit found Pause Menu and Manager Book could still open while Store Design Mode owned player/cursor/time state, creating conflicting mode ownership

These issues are corrected before proceeding to decorations.

## Input ownership principle

When the pointer is over interactive UI:
- UI owns mouse wheel
- UI owns MMB
- world selection/placement already yields to UI

Keyboard camera pan remains available while Build Catalog is open because it does not conflict with ScrollRect and is useful while browsing.

## Camera/UI arbitration

`LayoutCameraController` now uses EventSystem pointer-over-UI detection.

When pointer is over UI:
- wheel does NOT change camera target distance
- MMB does NOT orbit

When pointer is outside UI:
- wheel zoom remains unchanged
- MMB orbit remains unchanged

All camera tuning values remain:
- Working Distance 9
- Pitch 62
- Yaw 0
- Padding 1.08
- Fallback 4
- Min/Max pitch 15/88
- Min/Max distance 4/32
- Pan 7
- Boost 2
- Pan Smooth .08
- Yaw .12
- Pitch .10
- Scroll 2
- Orbit Smooth .06
- Zoom Smooth .10

## Placement-area camera routing

FurnitureDefinition.PlacementArea remains authoritative.

Before FurnitureShop creates a purchased preview:
1. ensure Store Design Mode is active
2. ask StoreLayoutModeController to focus the furniture's PlacementArea
3. if current camera pivot is already inside an available zone of that area, do not recenter
4. otherwise find the nearest available zone of the required area relative to current camera pivot
5. immediately focus that zone at working distance
6. create/start preview

This means:
- buying Storage furniture from Sales Floor jumps to Backroom before preview starts
- buying SalesFloor furniture while viewing Backroom jumps to Sales Floor
- repeated purchases in the same area do not keep re-centering the camera
- computer furniture purchases use the exact same routing because the behavior lives in FurnitureShop / StoreLayoutModeController, not BuildCatalogUI

## StoreBuildArea additions

New overload:

`TryGetWorkingZoneBounds(Vector3 worldPoint, StoreAreaType requiredArea, out Bounds bounds)`

It selects the nearest available local zone matching the requested area.

New query:

`IsPointInsideAvailableArea(Vector3 worldPoint, StoreAreaType requiredArea)`

Used to avoid unnecessary camera re-centering when already working in the correct area.

## Layout camera API

New:
- `PivotPosition`
- `FocusWorkingBoundsImmediate(Bounds)`

Immediate focus preserves current yaw/pitch and resets distance to Working Distance.

This is used for area transitions before starting a purchase preview so the furniture appears in the visible workspace immediately.

Existing future APIs remain:
- FocusWorkingBounds
- FrameBounds

## Build Catalog active marker

The unsupported:
`✓ ALL`

is replaced with ASCII-only:
`> ALL`

Same for other active categories.

Active category remains communicated by:
- text prefix `>`
- selected/disabled button state/color

No font fallback warning and no color-only critical state.

## Build Catalog Escape behavior

When catalog is open and no placement is active:
- Escape closes catalog

During furniture placement:
- Escape remains owned by FurniturePlacementController and cancels placement
- catalog then reopens as before

## Cross-mode ownership

Store Design Mode owns:
- player enabled/disabled state
- cursor state
- customer suspension
- store-clock suspension
- layout camera

Therefore generic full-screen modal systems must not independently open while Store Design is active.

### PauseMenuUI
Now references StoreLayoutModeController.

While Store Design is active:
- Escape does not open Pause Menu
- PauseMenuUI.Open() refuses to open

### ManagerBookUI
Now references StoreLayoutModeController.

While Store Design is active:
- M does not open Manager Book
- ManagerBookUI.Open() refuses to open

Ordering terminal cannot normally open because PlayerInteraction is disabled in Store Design.
EndOfDay cannot naturally appear while StoreClock is suspended.

## Store Design Escape hierarchy

When Build Catalog is open:
- Escape closes catalog

When placement is active:
- Escape cancels placement

When neither is active:
- Escape does not open Pause while Store Design is active

Exit Store Design:
- L

This avoids one key performing incompatible actions across stacked states.

## Placement

Unchanged:
Structured:
- .5m grid
- magnetic sockets
- cardinal R/T

Free:
- raw position
- no magnet
- continuous R/T

G toggles default.
Ctrl temporarily inverts.

## Catalog / purchase business rules

Unchanged:
- one FurnitureShop authority
- preview first
- no spend until confirmation
- cancel costs nothing
- Build Catalog and Store Computer are two UI entry points into the same authority

## Save

Unchanged:
- schema v2
- time/customer state does not gate save
- end-of-day completion idempotent

## Roadmap revision

### Lesson 130 — Decorations Foundation
Lesson 129 was used for necessary Store Design polish before adding more content.

### Lesson 131 — World-Space Expansion Preview & Purchase

Then:
- copy/eyedrop
- repeat placement
- selling/refunds
- variants/colors
- Ambience
- autosave
- exact carry persistence
- advanced fixture/socket semantics

## Permanent engineering/UX rules

- GitHub main first
- inspect exact current source and scene
- preserve user-tuned values
- player testing can reorder roadmap
- one business authority, multiple UI entry points
- modal/input ownership must be explicit
- mouse input over UI must not leak into world/camera controls
- lesson implementation stays in chat
- only handoff `.md` files separate
- time is simulation, not permission
- save/load idempotent
- no color-only critical states
- avoid god classes
