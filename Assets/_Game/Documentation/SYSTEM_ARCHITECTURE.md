# Little Market — System Architecture

Current architecture after **Lesson 110 — Layout Mode Simulation Foundation**.

## Baseline
Designed from GitHub `main` commit `29d4b30e67c593cd741405c3f7a8fee4cae9e837` (`Lesson 109 complete - layout usability rear expansion and furniture snapping`).

## Placement principle
Placement is a major cozy/design gameplay pillar and will be developed across multiple lessons. Responsibilities stay separated instead of growing one giant controller.

Long-term placement responsibilities include layout-mode orchestration, placement lifecycle, validation, grid/precision behavior, magnetic snapping, visualization, and modular asset standards.

## StoreLayoutModeController
A new `StoreLayoutModeController` lives on the existing `StoreSystems` GameObject.

Prototype controls:
- L: enter/exit Layout Mode
- G: select furniture while Layout Mode is active
- existing R/T/E/Q placement controls remain temporarily

Lesson 110 deliberately keeps the existing first-person camera. A dedicated layout camera comes next.

Layout Mode does not use `Time.timeScale = 0`. It explicitly suspends the systems that must stop.

## Availability
Layout Mode works whether the store is open or closed and does not require an empty store.

The player must put down any carried item before entering Layout Mode.

## StoreClock
`StoreClock` has a layout-suspended flag. While suspended, game time and schedule transitions do not advance. Existing time state resumes unchanged.

## CustomerFlow
`CustomerFlow` pauses its spawn timer while Layout Mode is active and coordinates suspension/resumption of all active customers.

## CustomerController
Customer state and visit data stay in memory during Layout Mode.

While suspended:
- customer state Update stops
- NavMesh movement stops
- renderers are hidden
- child colliders are disabled
- shopping list, carried products, basket state, satisfaction, checkout state and targets are preserved

On Layout Mode exit:
1. NavMesh is rebuilt if furniture changed
2. invalid customer positions are sampled/warped to nearby usable NavMesh
3. the existing activity is reissued from the preserved CustomerState
4. customer visuals/colliders return
5. simulation resumes

Moved-shelf targets therefore route to the shelf's new CustomerStandPoint.

## Stocked furniture
ShelfSlot and StorageBay no longer use stock quantity as an `IFurnitureMoveConstraint`.

Shelves and storage racks may be moved while stocked. `IFurnitureMoveConstraint` remains available for future genuinely meaningful restrictions.

## FurniturePlacementController
FurniturePlacementController no longer decides whether the store is open or empty and no longer rebuilds NavMesh directly.

It requires Layout Mode to be active, owns the current placement preview lifecycle, and calls `StoreLayoutModeController.MarkNavigationDirty()` after a confirmed move/purchase.

## Deferred NavMesh rebuilding
A layout session can contain many furniture edits. NavMesh rebuild occurs once when Layout Mode exits, followed by customer recovery.

## FurnitureShop
Furniture purchasing no longer requires a closed/empty store.

Buying furniture from the Store Computer closes the terminal, enters Layout Mode, suspends customers, creates the preview, and charges only after placement confirmation. Canceling an unconfirmed purchase remains free.

Expansion purchasing is unchanged in Lesson 110.

## Save-system note
Development save schema remains `2`.

SaveManager still blocks save/load while active customers exist. This is intentional because active customers can hold product instances already removed from shelves, but current save data does not serialize in-progress customer visits. Removing the guard would risk lost/duplicated stock.

Safe save-anytime requires an explicit active-customer persistence/snapshot policy and is deferred rather than faked.

## Current placement baseline values
The current `Market` scene values are preserved:
- grab distance: 4 m
- preview ray distance: 18 m
- grid size: 0.1 m
- furniture snap distance: 0.35 m
- furniture snap gap: 0.01 m
- alignment snap distance: 0.30 m

These remain prototype values.

## Roadmap
### Lesson 111 — Layout Camera & Placement Visual Foundation
Dedicated elevated/free layout camera, floor-grid visualization, body footprint, access footprint, nearby occupancy feedback.

### Lesson 112 — Grid/Precision Controls & Modular Asset Standard
Independent grid/precision controls and formal Little Market measurement/pivot conventions.

### Lesson 113 — Magnetic Socket Snapping
Explicit snap sockets, zero visible gap, sticky attach/detach hysteresis and alignment guides.

### Later — Save-anytime customer persistence
Permit safe save/load with active customers only after their in-progress stock and visit state is defined.

Checkout development remains planned after the placement foundation: physical scanning/bagging, card/cash interaction, cash drawer/change, keypad entry, later PLU codes, multiple checkouts and self-checkouts.
