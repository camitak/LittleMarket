# Little Market — System Architecture

Current architecture after **Lesson 109 — Store Layout Usability, Rear Sales Expansion & Furniture Snapping**.

## Baseline

Lesson 109 was designed from GitHub `main` commit `197f48a2fc546b9839afdf09bc5f0b55e3caf966` (`Lesson 108 complete - generic furniture runtime and purchasable storage`).

## Store layout direction

The building shell and expansion boundaries are structural. Operational equipment belongs to the generic furniture layer and should eventually be player-placeable.

The backroom grows only toward negative X. The sales floor can grow independently toward positive X and positive Z.

Starter sales floor: X -4 to +8, Z -5 to +5, 12 m × 10 m.
Starter backroom: X -8 to -4, Z -5 to +5, 4 m × 10 m.

### Sales Floor Side Expansion I

Stable ID remains `sales_floor_01`.

Bounds: X +8 to +12, Z -5 to +5. Adds 40 m².

### Sales Floor Rear Expansion I

New ID: `sales_rear_01`.

Bounds: X -4 to +8, Z +5 to +9. Adds 48 m².

Provisional price: £120. Required Store Level: 2.

The starter `Wall_Rear_Sales` is its locked barrier. Unlocking activates a new floor, ceiling, left/right side walls and rear exterior wall.

### Backroom Expansion I

ID remains `backroom_01`.

Bounds: X -12 to -8, Z -5 to +5. Adds 40 m².

## Build zones

StoreBuildArea references five zones: base SalesFloor, base Backroom, Sales Side 01, Sales Rear 01, Backroom 01.

StoreExpansionSystem exposes three purchase zones in this order: Sales Side 01, Sales Rear 01, Backroom 01.

No new expansion-system code is required.

## Store Computer scrolling

Furniture and Expansion use dynamic rows inside ScrollRect content.

StoreComputerManagementUI now forces Unity layout rebuilds after dynamically repopulating those lists so ScrollRect receives correct content bounds immediately.

Freshly opened tabs reset to the top. Expansion confirmation rebuilds preserve current scroll position.

## Starter shelf layout

Default authored positions are moved away from the middle of the store:

- Shelf_Cereal: (-2.5, 0, 3.8)
- Shelf_Bakery: (0.3, 0, 3.8)
- Shelf_Dairy: (3.1, 0, 3.8)

All retain Y rotation 0.

These are new-game/default positions. Existing saved furniture positions intentionally override them when loading a save.

## Furniture snapping

The general furniture placement grid remains 0.5 m.

FurniturePlacementController adds optional edge snapping based on FurnitureDefinition body footprints.

Provisional values:

- snap activation distance: 0.35 m
- body gap: 0.05 m
- perpendicular alignment distance: 0.30 m

The preview first uses the normal grid. When it approaches another managed FurnitureIdentity in the same StoreAreaType, the nearest compatible body edge may snap to a 5 cm gap.

If perpendicular centers are also close, they align into a clean row.

Normal build-area, body collision, and front-access validation still run after snapping, so snapping never bypasses gameplay clearance rules.

## Save compatibility

Development save schema remains `2`.

Adding `sales_rear_01` is naturally compatible with existing schema-2 saves; older saves simply leave the new expansion locked.

Changing default authored shelf positions does not require a schema bump because loaded saves intentionally preserve the player's stored furniture layout.

## Checkout direction

Current checkout already provides one queue, item-by-item scanning, scan visual/audio feedback, automatic bag creation, final payment, sale reporting and money gain.

The remaining weakness is that cashier activity is mostly repeated E presses.

The preferred future direction is a physical but cozy cashier loop:

customer unloads items -> player scans physically -> player bags -> payment -> customer leaves.

Manual scanning and bagging should provide the core tactile interaction. Payment can later vary between card/tap and occasional cash/change without turning every transaction into a tedious coin-counting simulator.

Checkout counters must eventually become managed furniture discovered through a checkout registry rather than one CheckoutQueue permanently referenced by CustomerFlow.

Additional staffed checkouts and future self-checkouts should become purchasable layout decisions.

## Roadmap

### Lesson 110 — Dynamic checkout foundation & cashier interaction phase 1

Generalize checkout discovery, convert the starter checkout toward managed furniture, and begin richer physical cashier interaction.

### Lesson 111 — Basket/service furniture & multi-checkout behavior

Generalize basket-station discovery and validate multiple service points.

### Lesson 112 — First employee foundation

Begin employees after manual service furniture and cashier gameplay are stable. Restocker remains the preferred first employee.

## Still deferred

Spoilage, operating expenses, production building art, advanced/corner expansion tiers, furniture selling/recovery, and final economy balancing remain deferred.
