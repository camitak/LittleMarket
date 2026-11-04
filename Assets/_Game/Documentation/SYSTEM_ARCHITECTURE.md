# Little Market — System Architecture

Current architecture after **Lesson 125 — Idempotent End-of-Day Saves & Natural Save Access**.

## Verified source of truth

Lesson 125 was designed after verifying GitHub `main` at:

`0e772107d0c47e1aab49c0dfaf311821c4743304`

Commit:

`Lesson 124 complete - anytime management and safe live saves`

Before every future lesson, re-verify the newest `main`.

## Permanent lesson-format rule

The user explicitly requested that lesson implementation must never be split across chat plus separate code/setup files.

From now on:
- the complete lesson, code changes, explanations, Inspector steps and tests stay together in the chat
- only the required handoff `.md` files are generated separately for download
- do not create separate lesson-code ZIPs or code-edit documents unless the user explicitly asks

## Core principles

> The building provides space; the player decides how the grocery operates inside that space.

> The clock describes the store's day; it does not grant permission to manage the store.

Time-of-day and active customers must not artificially gate:
- Store Design/Layout Mode
- moving furniture
- buying furniture/decor
- buying expansions
- saving

Real restrictions remain:
- money
- Store Level
- ownership/unlocks
- valid placement
- atomic uncommitted interactions that cannot yet be serialized safely

## Lesson 124 verification

Verified in current GitHub source and scene:
- StoreExpansionSystem no longer references StoreClock or CustomerFlow
- expansion eligibility is configuration + ownership + Store Level + money
- SaveManager has CheckoutRegister assigned to the real CheckoutCounter
- customer transient-inventory reconciliation is present
- CustomerController.DiscardForLoad exists
- CustomerFlow.ClearForLoad exists
- CheckoutQueue.ClearForLoad exists
- CheckoutRegister.ResetForLoad exists
- RestockBasket.GetProductDataAt exists
- SaveManager uses CanSaveNow / CanLoadNow
- schema remains version 2

### Correctness gap found

End-of-day saves were not fully idempotent.

`StoreDayLifecycle.completedDay` is runtime-only and is not serialized.

A save made after `CompleteDay()` could load with:
- StoreClock.HasDayEnded = true
- StoreReportHistory already containing that day's report
- StoreDayLifecycle.completedDay reset to -1

Without protection, StoreDayLifecycle could process the same day again, duplicating:
- daily reputation processing
- goal rewards
- store-level experience/progression
- unlock refresh work

The report itself already de-duplicates by day, but earlier side effects could still run twice.

## Lesson 125 fix — report history is the durable completion marker

`StoreReportHistory` gains:

`HasReportForDay(int day)`

A completed-day report is written only after all day-completion side effects have run, so its presence is a durable marker that the day was already processed.

`StoreDayLifecycle.Update()` now:
1. checks required references
2. resets completion state while the day is active
3. returns if already complete
4. if the clock is ended AND StoreReportHistory already contains the current day, restores `completedDay` to the current day without re-running completion side effects
5. otherwise waits for customers to finish if necessary
6. processes the day exactly once

This keeps save schema version 2 and makes end-of-day load idempotent.

### Important edge case

If the clock has ended but the day had NOT yet been processed because customers were still present:
- no report exists
- Load clears transient customers
- StoreDayLifecycle sees no completed report
- CompleteDay runs once
- correct behavior

## Natural save access on end-of-day summary

The backend already permits saving after day end, but PauseMenuUI intentionally refuses to open over EndOfDayUI.

Rather than stacking Pause over a modal summary and risking conflicting cursor/time/player state, Lesson 125 adds a dedicated SAVE GAME button directly to the EndOfDay summary.

New EndOfDayUI references:
- saveGameButton
- saveStatusText
- saveManager

End-of-day summary layout:
- SummaryCard remains `620 x 360`
- SaveGameButton bottom-center-left at `(-120, 30)`, size `220 x 62`
- StartNextDayButton bottom-center-right at `(120, 30)`, size `220 x 62`
- EndOfDaySaveStatusText at `(0, 100)`, size `500 x 30`

Save button palette:
- normal `#E6D8C8FF`
- highlighted `#F2E8DDFF`
- pressed `#CDBAA6FF`
- selected `#F2E8DDFF`
- disabled `#A69A9080`
- text `#594640FF`

Save status:
- neutral `#7A6860FF`
- success `#6FCF97FF`
- failure `#C46B6BFF`

This keeps START DAY as the stronger primary action while still making Save obvious and available naturally.

## Save behavior after Lesson 125

SAVE can occur:
- before opening
- while open
- with active customers
- after closing
- after the day has ended
- from the end-of-day summary

Current SAVE blocker:
- active unconfirmed furniture placement

LOAD:
- ignores time and active customers
- clears transient customer/checkout state
- still requires player carry state to be settled in schema 2

## Store Design Mode direction

Player-facing concept:
- Arrange
- Build Catalog
- Expansions
- later decoration/finish tools

Do not turn Little Market into free room-shell construction.

## Planned placement model — NEXT

### Structured
- `.5 m` grid
- magnetic sockets
- structured rotation

### Free
- raw position
- no magnetic assistance
- continuous/fine rotation

Planned input:
- G toggles persistent Structured/Free default
- Ctrl held temporarily inverts the current default
- remove Alt-only magnet bypass
- structured R/T moves to the next/previous 90-degree world orientation
- free R/T rotates continuously, initial test speed about `90°/sec`

Important nuance:
after free-angle rotation, returning to Structured does not forcibly destroy the chosen angle; the next structured R/T press rejoins the nearest next/previous cardinal orientation.

## Current Store Design/Layout controls before Lesson 126

- LMB select/place
- R/T rotate 90°
- G Grid Snap
- Alt bypass magnet
- MMB orbit
- Wheel zoom
- WASD pan
- Shift faster
- RMB/Esc/Q cancel
- L exit

## Camera values — scene source of truth

Preserve until dedicated camera pass:
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

Note: source-code initializers differ for some camera values; the scene Inspector overrides are authoritative.

## Existing UI colors

Layout grid:
`#CFF7FA26`

Placement/Grid status:
`#CFF7FADC`

Layout controls:
- background `#FFF9EEDC`
- text `#594640FF`

Context prompt:
- background `#2C2523F2`
- text `#FFF9EEFF`

Magnet:
- valid `#5FE3F0E6`
- invalid `#C46B6BE6`

## Revised roadmap

### Lesson 126 — Structured vs Free Placement + Rotation Feel
Implement the Two Point / Sims-inspired unified placement assistance model.

### Lesson 127 — Layout Camera Feel
Pan/orbit/zoom smoothing and working-region focus.

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
- inspect actual complete source and scene
- preserve scene-authored tuning
- exact UI instructions and HEX colors
- research meaningful player-facing UX
- player playtesting drives iteration
- time is simulation, not permission
- saving protects player progress
- completed simulation transitions must be idempotent across save/load
- one business authority, multiple UI entry points
- no color-only critical information
- placement/navigation/presentation/persistence remain separate
- avoid god classes
- end each lesson with updated architecture + continuation `.md`
- lesson implementation must remain together in chat unless user explicitly requests another format
