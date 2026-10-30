# Little Market — System Architecture

Current architecture after **Lesson 107 — Modular Store Shell & Spatial Baseline Reset**.

## Project baseline

Little Market is a cozy first-person grocery-store management game built with Unity 6.3, URP, the Unity Input System, Unity AI Navigation/NavMesh, C#, Rider, and GitHub `main`.

Lesson 107 was designed from GitHub `main` at commit:

`a1df97a592bee9626b4217f9343de2ee1abc0e48`

Commit message:

`Lesson 106 complete - persistent store expansion foundation`

## Pre-release compatibility policy — Lesson 107

Little Market is still in active prototype/pre-release development.

Compatibility is now treated deliberately:

- released player saves must be migrated carefully;
- prototype saves do not automatically deserve permanent compatibility;
- obsolete prototype assumptions should be removed when they make the architecture worse;
- compatibility code must have a current product reason to exist.

Lesson 107 intentionally starts a new development save baseline instead of migrating the old prototype spatial coordinates.

The old `little_market_save.json` file is not consumed by the new baseline. The new development save filename is:

`little_market_dev_v2_save.json`

The new baseline save schema starts at version `1` and currently requires an exact schema-version match.

## Spatial contract

From Lesson 107 forward:

- 1 Unity unit = 1 metre.
- StoreBuilding root position = `(0, 0, 0)`.
- StoreBuilding root rotation = `(0, 0, 0)`.
- StoreBuilding root scale = `(1, 1, 1)`.
- X = storefront left/right.
- Y = vertical.
- Z = front/back.
- negative Z = street/customer front.
- positive Z = rear/receiving side.
- structural planning grid = 2 m.
- furniture placement grid = 0.5 m.
- floor walking surface = Y 0.
- interior wall height = 3.2 m.
- wall thickness = 0.20 m.
- structural floor/ceiling thickness = 0.20 m.

These coordinates are now the long-term gameplay spatial baseline. Future modeled assets should fit this contract rather than changing gameplay coordinates.

## Starter building

The starter building is 16 m × 10 m.

### Sales floor

Bounds: X -4 to +8, Z -5 to +5.

Dimensions: 12 m × 10 m = 120 m².

### Backroom

Bounds: X -8 to -4, Z -5 to +5.

Dimensions: 4 m × 10 m = 40 m².

The starter store therefore has a 3:1 sales-floor-to-backroom area ratio.

The sales floor and backroom are side by side so they can expand independently.

## StoreAreaType

New enum:

- `SalesFloor`
- `Backroom`

This is gameplay data, not merely a visual label.

Current display furniture uses SalesFloor. Future storage-rack furniture will use Backroom.

## StoreBuildZone

`StoreBuildZone` represents one physical floor collider that is currently available for furniture placement.

It owns area type, floor Collider, available-at-start state, and runtime IsAvailable state.

Base sales-floor and backroom zones are available from the start. Expansion zones become available only after purchase.

## StoreBuildArea refactor

`StoreBuildArea` no longer knows about a special base floor plus expansion floors.

Instead it owns an array of `StoreBuildZone` objects.

It can raycast onto available floor for a requested StoreAreaType, validate a furniture footprint against the union of available zones of that area, and identify floor colliders that should not count as placement obstructions.

## FurnitureDefinition refactor

`FurnitureDefinition` now owns a `StoreAreaType PlacementArea` and a `FurnitureIdentity` prefab in addition to its existing data.

Existing sales-display definitions use SalesFloor.

## FurnitureIdentity

`ShelfIdentity` is renamed/generalized to `FurnitureIdentity`.

It owns FurnitureDefinition and stable persistent instance ID.

This removes a shelf-specific persistence assumption before backroom furniture is introduced.

## FurniturePlacementSaveData

`ShelfPlacementSaveData` is replaced by `FurniturePlacementSaveData`.

Fields:

- furnitureID
- furnitureDefinitionID
- position
- rotation

The old `purchasedByPlayer` field is removed because authored furniture can be recognized from authored persistent IDs.

Old version-19/version-20 furniture migration logic is removed.

## StoreFurnitureLayout

`StoreFurnitureLayout` now works with `FurnitureIdentity`.

It captures authored furniture IDs/poses, creates purchased previews, saves poses, reconstructs purchased furniture from furnitureDefinitionID, restores authored furniture by stable ID, retires runtime furniture not present in a loaded save, and rebuilds NavMesh.

Prototype save-version migration branches and `legacyVersion19ShelfDefinition` are removed.

## Furniture placement

`FurniturePlacementController` now works with `FurnitureIdentity` and reads the required StoreAreaType from FurnitureDefinition.

Controls remain G move, R rotate right, T rotate left, E confirm, Q cancel.

Empty checks understand both ShelfSlot stock and StorageBay quantities in preparation for backroom furniture.

## Front-of-store layout

Customer front wall is at Z approximately -5.1.

Two 2 m openings are reserved:

- exit centered around X -1.8;
- entrance centered around X +5.8.

The front ~3 m of the sales floor is reserved as service/circulation space for entrance, baskets, checkout, queue, and exit.

## Backroom circulation

The sales/backroom partition is at X -4.

A 1.4 m staff opening is placed in the rear half of the partition.

The backroom rear wall has a 2.4 m receiving opening centered around X -6.

Future flow:

receiving exterior → receiving door → DeliveryZone → storage → staff opening → sales floor.

The Store Computer and player restock BasketStation belong in the backroom.

## Expansion layout

### Sales Floor Expansion I

Bounds: X +8 to +12, Z -5 to +5.

Adds 4 m × 10 m = 40 m².

Area type: SalesFloor.

Expansion ID: `sales_floor_01`.

### Backroom Expansion I

Bounds: X -12 to -8, Z -5 to +5.

Adds 4 m × 10 m = 40 m².

Area type: Backroom.

Expansion ID: `backroom_01`.

The old Lesson-106 `rear_extension_01` geometry and asset are removed.

## StoreExpansionZone refactor

`StoreExpansionZone` references a StoreBuildZone rather than a raw build-floor collider.

Unlocking disables its boundary barrier, activates its geometry root, and marks its StoreBuildZone available.

## StoreExpansionSystem compatibility cleanup

`RestoreUnlockedExpansionIDs(List<string>)` now restores only the current development schema.

Old save-version branches are intentionally removed.

## Save system — development baseline v2

New save filename: `little_market_dev_v2_save.json`.

Current schema version: `1`.

The schema uses `List<FurniturePlacementSaveData> furniturePlacements`.

Load requires an exact schema-version match.

Historical migration branches for prototype versions 2–21 are removed.

The old save remains on disk unless manually deleted, but the game no longer reads it.

## Restore order

1. economy
2. reputation
3. unlocked products
4. unlocked expansions / geometry
5. furniture layout
6. shelf inventory
7. deliveries
8. loose products
9. daily stats
10. goal reward state
11. Store Level
12. progression refresh
13. clock
14. pending supplier deliveries
15. backroom storage inventory
16. report history
17. customer flow reset

## Modeling contract

Lesson-107 cubes are structural blockout proxies, not final art.

Future production models should use metres, fit the fixed gameplay dimensions, keep prefab roots at scale 1, replace visual meshes without changing room bounds, and preserve door/opening locations unless a deliberate gameplay redesign is made.

## Deferred

Still deferred: spoilage, operating expenses, polished building art, additional expansion tiers, employee automation, and purchasable storage racks.

## Roadmap

### Lesson 108 — general furniture persistence and purchasable backroom storage

Extend the generic FurnitureIdentity/StoreFurnitureLayout path to StorageBay furniture and add the first purchasable ambient/chilled backroom rack definitions.

### Lesson 109 — expansion progression and space-management review

Validate independent SalesFloor and Backroom expansion purchases, area restrictions, customer flow, storage capacity, navigation, and save/load.

### Lesson 110 — first employee foundation

Begin the first employee system after the manual workflow and spatial architecture are stable. A restocker remains the preferred first employee.
