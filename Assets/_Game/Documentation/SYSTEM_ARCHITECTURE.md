# Little Market — System Architecture

Current proposed architecture after **Lesson 123 — Store Design Mode Mechanics Blueprint**.

## Verified source of truth

Lesson 123 was designed after inspecting GitHub `main` at:

`e21894f5006d5c6f9d5753e00071b0bbfea20369`

Commit:

`Lesson 122 complete - readable contextual prompt card`

No gameplay code was intentionally changed in Lesson 123. This lesson locks the next mechanical direction before implementation.

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

Little Market is a cozy first-person grocery-management game.

The design/build loop is now considered a core reward loop, not a utility feature.

## Permanent UX/UI rules

Every UI task must include exact:
- hierarchy
- object names
- anchors
- pivot
- position
- size/offsets
- font size
- alignment
- Raycast Target
- references
- HEX colors
- RGBA where useful
- UX rationale

Research meaningful player-facing UX against current game/UI best practice and relevant games before locking it.

User playtest feedback is evidence.

Do not rely on color alone.

## Conceptual evolution: Layout Mode -> Store Design Mode

Existing code names may remain `LayoutMode` temporarily.

Player-facing concept becomes **Store Design Mode**.

It contains:

- Arrange
- Build Catalog
- Expansions
- later decoration/finish tools

Do not implement free room/wall drawing. The shell remains provided by the building/expansion system.

## Proposed placement interaction model

### Structured placement

- `.5m` grid
- magnetic socket assistance
- 90-degree rotation

### Free placement

- raw position
- no magnetic assistance
- fine/continuous rotation

### Proposed controls for next implementation pass

`G`
- toggles persistent default Structured/Free state

`Ctrl` held
- temporarily inverts the current default

This adapts the Two Point Museum pattern.

Current Alt-only magnetic bypass should be retired if Ctrl becomes the unified temporary Free-mode modifier.

### Rotation proposal

Structured:
- R/T = +/-90 degrees

Free:
- holding R/T = continuous rotation
- initial playtest speed target `90°/sec`

## Current placement foundation to preserve

- visible `.5m` grid
- Grid Snap implementation
- explicit RunEdge magnetic sockets
- body/access separation
- validation rules
- selected body/access visuals
- existing access visuals
- fixed clearance visuals
- readable contextual card
- NavMesh rebuild-on-exit strategy

## Build Catalog direction

Store Design Mode gets direct access to the same FurnitureCatalog/FurnitureShop authority.

Do not build a second purchase system.

Future player shortcut proposal:
- `B` opens/closes Build Catalog

Initial category architecture:
- All
- Fixtures
- Storage
- Refrigeration
- Checkout
- Décor
- Lighting & Signs

FurnitureDefinition should later gain catalog metadata rather than creating a separate DecorationDefinition.

Decorations use the same placement/save pipeline.

Decoration prefabs can omit navigation blockers and operational access where appropriate.

Purchase:
- preview first
- spend money on placement confirmation
- cancel without cost

Later:
- Shift repeat placement
- copy/eyedrop
- variants/colors

## Store computer role

Store computer remains the diegetic management hub.

Products:
- stay in the terminal

Furniture:
- computer becomes alternate entry into Store Design Mode + Build Catalog

Expansion:
- computer becomes overview/alternate entry into Store Design Mode expansion view

One business system, multiple entry points.

## Existing expansion business authority

StoreExpansionSystem remains authoritative for:
- cost
- required level
- ownership
- closed/empty purchase rule
- spend
- unlock
- save restoration
- NavMesh rebuild

StoreExpansionZone remains authoritative for:
- definition
- unlocked geometry root
- locked barrier
- build zone
- available/unavailable build space

## Expansion world interaction direction

Locked adjacent expansions become visible in Store Design Mode.

Do not use "lights off" as the only state cue.

Recommended visual language:

Base locked:
- fill `#625D7040`
- border `#B9AFCE99`

Available/hover:
- fill `#6FCF9738`
- border `#A9E6D5E6`

Condition/level locked:
- fill `#6B657A33`
- border `#8A7F96A6`

Text/icon labels must accompany colors.

Hover:
- highlight zone
- world label with name + price/requirement

Click:
- select zone
- camera frames it
- side details card opens

Purchase:
- explicit confirmation
- preserve closed/empty requirement for now

Unavailable copy:
`Available when the store is closed and empty.`

Successful reveal:
- spend
- barrier opens
- geometry activates
- lights/reveal turn on
- grid expands
- camera stays framed
- NavMesh rebuild
- space immediately becomes buildable

## Camera direction

Normal Store Design:
- focus useful working region, not entire store

Selecting locked expansion:
- temporarily expand camera bounds/focus to that outside/adjacent zone

Purchased expansion:
- becomes normal camera/build region

This is the preferred answer to the earlier future-exterior camera issue.

## Decoration philosophy

Decoration is a central cozy-game pillar.

Future soft Ambience system:
- local decoration improves guest mood/reputation
- diminishing returns
- variety preferred over spam
- no harsh decoration gating
- creative expression first

Do not implement Ambience until catalog and placement UX are mature.

## Recommended future selling policy

Furniture/decor:
- 100% refund
- functional item may need to be empty/unoccupied

Expansion:
- permanent, non-refundable

This encourages experimentation while keeping expansion progression meaningful.

## Current Layout/Store Design controls before next implementation

- LMB select/place
- R/T rotate 90°
- G Grid Snap toggle
- Alt magnetic bypass
- MMB orbit
- Wheel zoom
- WASD pan
- Shift faster pan
- RMB/Esc/Q cancel
- L exit

These are expected to change in the next control pass.

## Current UI colors

Grid:
`#CFF7FA26`

Grid status:
`#CFF7FADC`

Controls panel:
- background `#FFF9EEDC`
- text `#594640FF`

Prompt card:
- background `#2C2523F2`
- text `#FFF9EEFF`

Magnetic guide:
- valid `#5FE3F0E6`
- invalid `#C46B6BE6`

## Placement validation

Unchanged:

1. body inside permitted build area
2. access inside area
3. body vs fixed clearance
4. access vs fixed clearance
5. body vs other furniture access
6. body vs physical colliders
7. access vs physical colliders

Body vs other access forbidden.
Access vs access allowed.

## Camera tuning to preserve until dedicated implementation

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

## Implementation roadmap

### Lesson 124 — Structured vs Free Placement + Rotation Feel

Implement:
- coherent placement mode state
- Ctrl temporary inversion
- Grid/magnet participation by mode
- 90-degree structured rotation
- continuous free rotation
- updated HUD/legend
- preserve validation

### Lesson 125 — Layout Camera Feel

Implement:
- pan acceleration/deceleration
- orbit smoothing
- zoom easing
- working-region focus
- prepare frame-target API for future expansions

### Lesson 126 — In-Mode Build Catalog

Implement:
- direct Layout Build Catalog
- categories
- FurnitureShop reuse
- computer deep link

### Lesson 127 — Decorations Foundation

Implement:
- catalog category metadata
- decoration definitions using FurnitureDefinition
- non-navigation-blocking decorative prefabs
- save/placement integration

### Lesson 128 — World-Space Expansion Preview & Purchase

Implement:
- locked-zone visuals
- hover/select
- side card
- camera frame
- direct purchase
- reveal

Then:
- copy/eyedrop
- repeat placement
- selling/refunds
- variants
- Ambience
- advanced fixture/socket semantics

## Permanent engineering principles

- GitHub main first
- inspect exact current source
- preserve tuned values unless dedicated pass changes them
- exact UI instructions and HEX colors
- current UX research
- playtest feedback drives iteration
- design mode is a central gameplay loop
- one business authority, multiple UI entry points
- do not copy reference games blindly
- preserve Little Market's building-shell identity
- placement/navigation/presentation/persistence remain separate
- avoid god classes
- end each lesson with architecture + continuation docs
