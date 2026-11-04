# Little Market — System Architecture

Current proposed architecture after **Lesson 122 — Context Prompt Readability & UX Baseline**.

## Verified source of truth

Lesson 122 was designed after inspecting GitHub `main` at:

`6c33793873a5cf0e16a288dbbe1c0bca70cfe7ce`

Commit:

`Lesson 121 complete - magnetic furniture socket snapping`

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
- main scene `Assets/Scenes/Market.unity`

## Core principle

> The building provides space; the player decides how the grocery operates inside that space.

Little Market is a cozy first-person grocery-management game. Placement remains a major design pillar.

## Permanent UX/UI rules

### Exact UI setup

Every UI lesson must provide:
- hierarchy path
- object names
- anchors
- pivot
- anchored position
- size/offsets
- font size
- alignment
- Raycast Target
- component references
- exact HEX colors
- RGBA where useful
- UX rationale

Never require the user to derive or search for a color.

### UX research

Before locking meaningful player-facing UX:
- research current game UX/UI best practices
- inspect relevant established-game/tool patterns
- prefer official engine/game/platform/accessibility guidance
- adapt principles to Little Market instead of copying mechanically
- treat user playtesting as evidence

### Accessibility/readability

- do not rely on color alone
- ensure text has stable contrast over changing 3D scenes
- prefer owned background containers for critical/contextual HUD text when scene contrast is unpredictable
- keep contextual copy short
- non-interactive HUD must not capture pointer input

## Lesson 122 — InteractionPrompt readability

### Problem found

Actual pushed `InteractionPrompt` before Lesson 122:
- direct child of `GameUI`
- center anchor
- anchored position `(0,-120)`
- size `600 x 80`
- white text `#FFFFFFFF`
- Font Size `28`
- no owned background
- Raycast Target ON

This made contextual messages blend into bright shelves, floor, lighting and Layout visuals.

### New hierarchy

`GameUI`
`└── InteractionPromptCard`
`    └── InteractionPrompt`

### InteractionPromptCard

- direct child of GameUI
- Anchor Min/Max `(0.5,0)`
- Pivot `(0.5,0)`
- Position `(0,50)`
- Size `900 x 76`
- Image Type `Sliced`
- Background `#2C2523F2`
- RGBA `(44,37,35,242)`
- Raycast Target OFF

### InteractionPrompt

- stretched inside card
- 24 px horizontal padding
- 10 px vertical padding
- Font Size `28`
- Center / Middle
- Word Wrapping ON
- Text `#FFF9EEFF`
- RGBA `(255,249,238,255)`
- Raycast Target OFF

The opaque RGB pair is approximately 14:1 contrast; the 95% background opacity keeps the card stable across moving game imagery.

### InteractionUI

`InteractionUI` now owns:
- `promptRoot`
- `interactionText`

`ShowPrompt()` activates the complete card.

`HidePrompt()` hides the complete card.

The public API remains unchanged, so existing gameplay systems continue using:
- `ShowPrompt(string)`
- `HidePrompt()`

### Copy discipline

Context prompts should be short and action-oriented.

Placement invalid copy becomes:

`Can't place here — keep furniture and customer access clear.`

Other existing examples:
- `LMB Move Standard Shelf`
- `LMB Buy & place £40.00`
- `[E] Pick up`

Persistent control legends do not belong in this prompt.

## InteractionPrompt placement policy

The contextual card lives bottom-center rather than near the center.

Reason:
- protects the world/editing focal region
- still close enough to be noticed
- top-left is Grid status
- top-right is Money
- bottom-left may contain Basket information
- bottom-right contains Layout controls

It appears only while context requires it.

## Current Layout Mode controls

- LMB select/place
- R/T rotate
- G Grid Snap
- Alt bypass Magnetic Snap
- MMB orbit
- Wheel zoom
- WASD pan
- Shift faster pan
- RMB/Esc/Q cancel
- L exit

## Grid Snap

- enabled by default
- `.5m`
- visible grid also `.5m`

Grid ON and OFF remain distinct.

## Magnetic Snap

Current Lesson-121 architecture remains unchanged.

- explicit FurnitureSnapSocket
- RunEdge Left/Right sockets
- capture `.30`
- release `.55`
- facing tolerance `15°`
- Alt bypass
- no auto-rotation
- Grid Snap and Magnetic Snap independent

First scope remains side-to-side furniture runs only.

## Magnetic guide

Valid:
- `#5FE3F0E6`

Invalid:
- `#C46B6BE6`

Top-left text also appends `MAGNET`.

## Placement validation

Unchanged:

1. body inside build area
2. access inside build area
3. body vs fixed clearance
4. access vs fixed clearance
5. body vs other furniture access
6. body vs physical colliders
7. access vs physical colliders

Body vs access forbidden.
Access vs access allowed.

## Existing UI colors

Layout grid:
- `#CFF7FA26`

Grid status:
- `#CFF7FADC`

Layout control panel:
- background `#FFF9EEDC`
- text `#594640FF`

Interaction prompt card:
- background `#2C2523F2`
- text `#FFF9EEFF`

## Two Point Museum control/feel research direction

The user reports Layout Mode still does not feel smooth enough and suggested Two Point Museum as a reference.

Research found a useful Two Point pattern:
- Room Item Snap can be the default behavior
- a temporary modifier switches to free placement
- the default can be inverted so free placement is primary and the modifier temporarily snaps
- snapped/free rotation behavior also changes with placement mode

This is relevant to Little Market but is deliberately not folded into Lesson 122.

### Timing decision

The dedicated Layout Controls & Feel pass should happen **next**, before adding more placement feature types.

The core foundations are now stable enough:
- build areas
- access rules
- visible grid
- Grid Snap
- free placement
- magnetic sockets
- readable HUD

Changing control feel now is safer than building more furniture interactions on top of a control scheme the user already says does not feel smooth enough.

## Lesson 123 planned — Layout Controls & Feel Benchmark

Research and compare:
- Two Point Museum
- other strong mouse/keyboard building modes where useful

Evaluate:
- persistent Grid mode vs hold-to-invert modifier
- free rotation vs 90° rotation
- press vs hold rotation
- mouse-driven rotation possibilities
- camera pan/orbit/zoom easing
- selected-object follow
- whether G remains useful
- whether Alt remains the best magnet bypass after a unified modifier design
- discoverability in the control legend

Do not copy another game blindly.

Goal:
make Little Market feel predictable, fast and comfortable while keeping its first-person-to-layout identity.

## Camera values to preserve until dedicated feel pass

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

## Navigation/save

No Lesson-122 changes.

Save:
- schema 2
- `little_market_dev_v2_save.json`

NavMesh rebuild:
- once on dirty Layout Mode exit

## Permanent engineering rules

- GitHub main is source of truth
- inspect current project before every lesson
- preserve user tuning
- exact UI details + HEX colors
- current UX research
- playtest feedback drives iteration
- readable HUD contrast over variable scenes
- no color-only essential state
- non-interactive UI must not intercept input
- Grid Snap and Magnetic Snap remain distinct
- placement/navigation/presentation/persistence remain separate
- avoid god classes
- end each lesson with complete architecture and fresh-chat continuation files
