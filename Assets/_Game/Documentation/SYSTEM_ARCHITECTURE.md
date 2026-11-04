# Little Market — System Architecture

Current proposed architecture after **Lesson 124 — Natural Anytime Management & Safe Saves**.

## Verified source of truth

Lesson 124 was designed from GitHub `main`:

`9fac2774b58dce0ee4414f968cf9e4182351693e`

Commit:

`Lesson 123 complete - store design mode mechanics blueprint`

Re-verify latest `main` before every future lesson.

## Core design principles

> The building provides space; the player decides how the grocery operates inside that space.

New permanent principle:

> **The clock describes the store's day; it does not grant permission to manage the store.**

Time-of-day must not gate:
- entering Store Design/Layout Mode
- moving furniture
- buying furniture/decor
- buying expansions
- saving

Progression/economic requirements can still gate purchases:
- Store Level
- money
- ownership
- unlock state
- configuration
- genuine spatial validity

Atomic uncommitted interactions may require resolution when they cannot yet be serialized safely.

## Why this changed

Little Market is a cozy management/building game.

Creative inspiration should not be answered with:
- "wait until closing"
- "wait until customers leave"
- "wait until tomorrow"

The simulation should absorb management changes safely.

This also matches relevant genre patterns:
- Two Point Museum's developers explicitly rejected fully closing the museum as a normal construction requirement because it could disrupt game flow and reactive management.
- The Sims supports manual saves from both Live Mode and Build Mode and now supports autosave during play/build.

## Store Design Mode while customers exist

Already-supported architecture:

1. player enters Layout/Store Design Mode
2. StoreClock is suspended
3. CustomerFlow suspends active customers
4. CustomerController stops NavMeshAgent
5. CustomerLayoutPresentation hides renderers/disables customer colliders
6. player changes layout
7. navigation rebuilds
8. CustomerController recovers to usable NavMesh
9. customers resume their activity

Therefore active customers are not a reason to deny furniture/design access.

## Expansion rules after Lesson 124

`StoreExpansionSystem.CanPurchase()` no longer references:
- StoreClock
- CustomerFlow

It requires only:

1. system configured
2. valid registered zone
3. zone not already owned
4. Store Level requirement met
5. enough money

`TryPurchase()` still:
- spends money
- unlocks zone
- syncs physics
- rebuilds NavMesh
- reports success

So expansions can be purchased:
- before opening
- while open
- after closing
- during any ordinary game hour

The future world-space expansion UI should reflect only real requirements such as level/money.

Remove all planned copy like:
`Available when the store is closed and empty.`

That rule is obsolete.

## Saving philosophy

Saving is a player-protection system, not a challenge mechanic.

### SAVE

`SaveManager.SaveGame()` now ignores:
- store open/closed
- day ended
- active customers
- player held product
- restock-basket contents
- held delivery box

The only current SAVE blocker is an active unconfirmed furniture placement.

Reason:
an active placement preview is an atomic uncommitted transaction whose intended final transform/purchase does not yet exist.

### Transient inventory normalization

Customers physically remove real PickupItems from shelf stock.

These active customer visits are transient and are intentionally NOT persisted as NPC simulations in schema 2.

Before serializing, SaveManager creates a durable snapshot:

For each unpaid customer-carried product:
1. try to re-credit it into an already-assigned matching saved shelf slot with capacity
2. otherwise serialize it as a loose product near the customer's current position

The live world is not mutated.

The same snapshot normalization applies to:
- player's held single PickupItem
- RestockBasket contents

Held DeliveryBoxes already participate in existing delivery serialization.

This ensures saving does not delete economic inventory.

### LOAD

Loading may occur regardless of:
- store hour
- day ended
- active customers

Before applying save data:
1. CheckoutRegister resets any unfinished scan/payment session
2. each CustomerController explicitly destroys its transient carried products, including items temporarily reparented to the checkout scan point
3. CustomerFlow clears current transient customers
4. CheckoutQueue clears transient queue state
5. durable saved state is restored
6. customer spawning resumes naturally afterward

LOAD still temporarily requires player carry state to be settled:
- restock basket put down/empty
- held delivery box put down
- held item put away

Exact player-carry restoration is not yet part of schema 2.

This is an interaction-state limitation, not a time-of-day gate.

## Save schema

Still:

- version `2`
- `little_market_dev_v2_save.json`

No version bump is needed.

Lesson 124 normalizes transient product value into existing:
- shelfSlots
- looseProducts
- deliveries

No serialized schema fields were added.

## Store Design Mode direction from Lesson 123

Conceptual player-facing system:

- Arrange
- Build Catalog
- Expansions
- later decoration/finish tools

Do not turn the project into free building-shell construction.

## Planned placement model

### Structured

- `.5m` grid
- magnetic sockets
- 90° rotation

### Free

- raw position
- no magnetic assistance
- continuous/fine rotation

### Planned control

G:
- persistent Structured/Free default

Ctrl held:
- temporarily invert current mode

When implemented, retire Alt as a separate magnet bypass.

## Build Catalog

Future Store Design Mode directly accesses the same:
- FurnitureCatalog
- FurnitureShop

No duplicate purchasing system.

Furniture/decor share FurnitureDefinition with future catalog metadata.

## Expansion world interaction

Locked adjacent expansion spaces will be visible/selectable directly from Store Design Mode.

Future visual proposal:

Base locked:
- fill `#625D7040`
- border `#B9AFCE99`

Available/hover:
- fill `#6FCF9738`
- border `#A9E6D5E6`

Level locked:
- fill `#6B657A33`
- border `#8A7F96A6`

No time-locked visual state.

Click:
- camera frame
- side details card
- purchase when level/money allow

## Decoration philosophy

Decoration is core cozy gameplay.

Future Ambience:
- soft guest mood/reputation benefits
- diminishing returns
- variety over spam
- creativity rather than hard decoration chores

## Current Layout controls before the next mechanics pass

- LMB select/place
- R/T 90° rotate
- G Grid Snap
- Alt bypass magnet
- MMB orbit
- Wheel zoom
- WASD pan
- Shift faster
- RMB/Esc/Q cancel
- L exit

## Current visual/UI colors

Grid:
`#CFF7FA26`

Grid status:
`#CFF7FADC`

Controls:
- `#FFF9EEDC`
- text `#594640FF`

Context prompt:
- `#2C2523F2`
- text `#FFF9EEFF`

Magnet:
- valid `#5FE3F0E6`
- invalid `#C46B6BE6`

## Placement validation

Unchanged:

1. body inside build area
2. access inside build area
3. body vs fixed clearance
4. access vs fixed clearance
5. body vs other furniture access
6. body vs colliders
7. access vs colliders

Body vs access forbidden.
Access vs access allowed.

## Camera values currently preserved

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

## Revised roadmap

### Lesson 125 — Structured vs Free Placement + Rotation Feel

Implement:
- explicit Structured/Free state
- G persistent default
- Ctrl temporary inversion
- Structured = grid + magnet + 90°
- Free = raw + no magnet + continuous R/T rotation
- remove Alt-only magnetic-bypass model
- update HUD and control legend

### Lesson 126 — Layout Camera Feel

- pan acceleration/deceleration
- orbit smoothing
- zoom easing
- useful working-region focus
- frame-target API for expansions

### Lesson 127 — In-Mode Build Catalog

- direct catalog access
- same FurnitureShop authority
- categories
- computer deep-link

### Lesson 128 — Decorations Foundation

### Lesson 129 — World-Space Expansion Preview & Purchase

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
- exact current source before coding
- preserve user tuning
- exact UI setup + HEX colors
- browse current UX/game patterns for important player-facing decisions
- player playtesting drives iteration
- time is simulation, not permission
- saving should protect player progress rather than create friction
- one business authority, multiple UI entry points
- Store Design Mode is core gameplay
- no color-only important state
- placement/navigation/presentation/persistence remain separate
- avoid god classes
- end lessons with updated architecture + continuation docs
