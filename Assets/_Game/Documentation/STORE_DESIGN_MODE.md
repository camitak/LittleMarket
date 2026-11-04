# Lesson 123 — Store Design Mode Mechanics Blueprint

Verified GitHub source:

`e21894f5006d5c6f9d5753e00071b0bbfea20369`

`Lesson 122 complete - readable contextual prompt card`

This lesson intentionally makes no Unity code changes. It locks the mechanical direction before implementation.

# Core decision

Rename the conceptual role of Layout Mode to **Store Design Mode**.

The code/class names do not need to be renamed immediately. The player-facing concept becomes a complete design/build state containing:

1. Arrange
2. Build Catalog
3. Expansions
4. Later: finishes/themes and richer decoration tools

Core principle remains:

> The building provides space; the player decides how the grocery operates inside that space.

Little Market should borrow the *interaction quality* of games such as Two Point Museum and The Sims without copying their room-construction loop.

# Placement modes

Use one coherent placement-assistance model rather than several unrelated modifiers.

## Structured placement

Default initially.

- `.5 m` Grid Snap
- magnetic compatible furniture sockets
- 90-degree rotation
- predictable production/layout workflow

## Free placement

Temporary or persistent alternative.

- raw floor position
- magnetic assistance bypassed
- fine/continuous rotation
- intended especially for decoration and expressive layouts

## Control proposal

`G`
- toggles the default Structured/Free state persistently during the current Layout session

`Ctrl` held
- temporarily inverts the current default
- Structured default -> hold Ctrl for Free
- Free default -> hold Ctrl for Structured

This adapts the Two Point Museum pattern.

Current `Alt` magnetic bypass should eventually be retired if Ctrl becomes a complete free-placement modifier. One modifier is easier to learn than separate grid and magnet bypass modifiers.

Do not implement the change until the dedicated control pass.

# Rotation proposal

Structured:
- R = +90°
- T = -90°

Free:
- hold R/T = continuous fine rotation

Initial target rate for playtesting:
- 90 degrees per second

Do not use delayed tap-vs-hold detection. The behavior changes based on current placement mode, which is immediately visible.

Potential later enhancement:
- mouse-drag fine rotation

# Store Design Mode tools

The mode should have three primary player-facing tools initially.

## Arrange

The default tool.

Capabilities:
- select
- move
- rotate
- cancel
- later sell
- later copy/eyedrop

No catalog panel needs to be open.

## Build Catalog

Accessible directly from Store Design Mode.

Future shortcut proposal:
- `B` toggles Build Catalog

Catalog uses the same authoritative `FurnitureCatalog` and `FurnitureShop` business logic already used by the store computer.

Recommended first categories:
- All
- Fixtures
- Storage
- Refrigeration
- Checkout
- Décor
- Lighting & Signs

Do not create separate purchase systems for decorations.

FurnitureDefinition should gain catalog metadata later rather than creating a parallel DecorationDefinition.

Suggested future fields:
- CatalogCategory
- icon/thumbnail
- sort order
- unlock metadata
- search keywords/tags
- whether repeat placement is allowed

Decoration prefabs may:
- omit FurnitureNavigationBlocker
- have zero/no operational access requirement where appropriate
- still use body placement validation
- participate in save/load like furniture

Purchase flow:
- click catalog item
- preview appears immediately
- money is spent only when placement is confirmed
- RMB/Esc cancels without cost
- catalog stays available after placement

Future repeat-placement:
- holding Shift while confirming keeps another preview of the same item ready
- default remains one purchase per normal confirmation to prevent accidental repeated spending

Future copy/eyedrop tool is high priority for decoration-heavy play:
- point at an existing object
- copy its FurnitureDefinition and future visual variant
- begin a new purchase preview directly

# Store computer integration

Do not duplicate systems.

Current computer remains the diegetic management hub.

Products:
- remain a computer workflow because stock ordering is operational management

Furniture:
- computer Furniture tab becomes an alternate doorway into Store Design Mode
- selecting/building furniture should use the same in-mode Build Catalog

Expansions:
- computer Expansion tab becomes an overview/alternate doorway into Store Design Mode's expansion view

Long-term flow:

Normal play -> Store computer -> Furniture -> opens Store Design Mode + Build Catalog

Normal play -> press Layout shortcut -> Store Design Mode -> open Build Catalog directly

Both routes use the same data/business logic.

# Expansion mechanic

The current StoreExpansionSystem already owns:
- price
- required store level
- owned state
- closed/empty-store restriction
- save restoration
- geometry activation
- build-zone activation
- NavMesh rebuild

Keep that authority.

The new mechanic is world-space discovery and purchase.

## Locked expansion visibility

Do not rely on "lights off" alone.

Lights-off/dimmed geometry is a good aesthetic layer, but it does not communicate:
- clickable
- purchasable
- locked by level
- price
- ownership state

Instead use a layered presentation.

### Base locked preview

In Store Design Mode, show only adjacent/relevant locked expansion zones.

Suggested footprint:
- fill `#625D7040`
- border `#B9AFCE99`

The area should feel physically dormant/dim.

If future geometry is shown, desaturate/dim it and keep lights off.

### Available-to-buy hover

- fill `#6FCF9738`
- border `#A9E6D5E6`

### Level/condition locked

- fill `#6B657A33`
- border `#8A7F96A6`

Use text/icon state too; color is not sufficient.

## Interaction

Hover:
- expansion boundary brightens
- small world label appears:
  - expansion name
  - price or requirement

Click:
- select expansion
- camera smoothly frames that zone
- open a side details card rather than a center modal

Card content:
- name
- description
- added floor area later if useful
- price
- required store level
- current blocking requirement
- BUY EXPANSION / unavailable state

Expensive permanent purchase remains two-step confirmation.

## Purchase timing

Allow expansion browsing at any time in Store Design Mode.

For now preserve actual purchase restriction:
- store must be closed
- day not ended
- no active customers

If unavailable during opening hours, the card should explicitly say:

`Available when the store is closed and empty.`

Do not silently disable the button.

Later evaluate a "Close store early" action separately. Do not hide that consequence inside expansion purchase.

## Purchase reveal

On successful purchase:

1. spend money
2. barrier opens/retracts
3. expansion geometry becomes active
4. lights come on / dim treatment disappears
5. build grid extends into the new area
6. camera remains focused on the new space briefly
7. NavMesh rebuild
8. expansion is now immediately usable

This should feel like a progression reward, not a spreadsheet transaction.

# Camera implications

The expansion mechanic solves part of the earlier exterior-camera question.

Normal Store Design Mode:
- focus current working region rather than the entire store

Expansion selection:
- temporarily allow camera bounds/focus to include the locked adjacent expansion
- smoothly frame that zone

Purchased expansion:
- it becomes part of the normal available build area

This avoids requiring the camera to always zoom far enough to see every future expansion.

# Decoration as a core cozy mechanic

Decoration should not be treated as cosmetic leftovers.

Two Point Museum explicitly positions decoration/customisation as part of guest happiness and provides hundreds of decorative items.

Little Market should eventually have a soft **Ambience** layer.

Recommended philosophy:
- decoration improves guest mood/reputation softly
- diminishing returns prevent spam
- variety can matter more than raw object count
- no harsh "you must place 10 plants" gating
- functional success should remain possible without perfect decoration
- decorating should reward expression rather than become a chore

Do not implement Ambience until catalog/placement interaction feels excellent.

# Selling and experimentation

Recommended future policy:

- full 100% refund for movable furniture/decor when sold
- functional objects can require empty/unoccupied state before sale
- expansion purchases remain permanent/non-refundable

Reason:
- furniture/decor is part of a cozy creative loop
- punishing redesign discourages experimentation
- permanent expansions are meaningful progression choices and are already confirmed separately

# Mechanics we explicitly reject for now

Do not add:
- player-drawn rooms/walls
- arbitrary building-shell construction
- front/back magnetic sockets before access semantics are designed
- large catalog search/filter complexity before catalog size needs it
- decoration stats before placement/catalog feel is finished
- multiple independent snap modifiers

# Implementation roadmap

Phase A — Store Design controls
- Structured vs Free model
- Ctrl temporary inversion
- structured 90-degree rotation
- free continuous rotation
- camera pan/orbit/zoom smoothing
- update control legend

Phase B — In-mode Build Catalog
- catalog drawer/panel
- categories
- same FurnitureShop authority
- buy-and-place from Layout Mode
- computer deep-link into catalog
- first Decoration category architecture

Phase C — World-space Expansions
- LayoutExpansionVisualizer
- locked footprint/ghost treatment
- hover/select
- expansion details card
- camera frame expansion
- direct purchase using StoreExpansionSystem
- reveal feedback

Phase D — Cozy builder quality
- copy/eyedrop tool
- repeat placement
- sell/refund
- variants/colors
- richer decorations
- Ambience system

Phase E — advanced fixtures
- back-to-back gondola policy
- endcaps
- wall-mounted fixtures
- checkout expansion/multiple checkouts
- exterior-specific placeables if needed

# Key product principle

The intended loop becomes:

Run the grocery -> earn money -> enter Store Design Mode -> rearrange / decorate / expand -> enjoy the changed space -> reopen and watch customers use what you created.

The design mode is not a side utility. It is one of Little Market's central reward loops.
