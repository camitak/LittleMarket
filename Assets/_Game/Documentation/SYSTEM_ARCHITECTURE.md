# Little Market — System Architecture

Current architecture after **Lesson 106**.

## Project baseline

Little Market is a cozy first-person grocery-store management game built with Unity 6.3, URP, the Unity Input System, Unity AI Navigation/NavMesh, C#, Rider, and GitHub `main`.

Lesson 106 was designed from GitHub `main` at commit:

`b5aacbf76a9bfda8784fd79f8cc36fd7bc77313a`

Commit message:

`Lesson 105 complete - flexible display requirements and store script organization`

## Current development priorities

The immediate priorities are the core physical store-management loop:

- products and progression
- ordering and deliveries
- shelf displays
- backroom storage
- furniture purchasing and placement
- store expansion
- customer shopping and checkout
- save/load
- later employee automation after the manual loop is mature

Spoilage is deliberately deferred. It remains a useful future depth feature, especially for chilled products, but it is not required to establish the core management game.

Daily operating expenses also remain deferred to a later economy/balancing phase.

## Script organization

Lesson 105 introduced feature folders beneath `Assets/_Game/Scripts/Store`.

Lesson 106 adds:

- `Store/Expansion`
  - `StoreExpansionDefinition`
  - `StoreExpansionZone`
  - `StoreExpansionSystem`
  - `StoreBuildArea`

`FurniturePlacementController` should also be moved into `Store/Furniture` because it belongs to the furniture feature. The move is performed inside Unity so its `.meta` GUID remains stable.

The project still intentionally uses the global C# namespace. Folder organization and namespace migration remain separate concerns.

## Product classification and display requirements

`ProductCategory` describes what a product is.

`ProductDisplayType` describes what physical sales display it requires.

`ProductStorageCondition` describes environmental storage requirements.

These concerns remain independent.

Examples:

- cereal: StandardShelf + Ambient
- Cloud Milk: StandardShelf + Chilled
- Sunny Loaf: BakeryDisplay + Ambient

## Sales shelves

`SalesShelf` owns display type, storage condition, customer stand point, and front-access rules.

`ShelfSlot` owns one facing assignment and the physical product instances in that facing.

A single furniture item may contain multiple product categories across different facings as long as each product is compatible with the display type and storage condition.

## Furniture system

The Lesson 104 data-driven furniture architecture remains:

- `FurnitureDefinition` owns furniture ID, display name, description, purchase price, prefab and placement footprint.
- `FurnitureCatalog` owns the purchasable furniture list.
- `FurnitureShop` owns furniture transaction rules.
- `StoreFurnitureLayout` owns persistent shelf-display furniture reconstruction.
- `FurniturePlacementController` owns player furniture movement/preview/confirmation.
- `ShelfIdentity` remains the persistent identity for the current shelf-display furniture family.

Furniture purchases still charge only when placement is confirmed. Cancelling placement remains free.

## Store expansion — Lesson 106

Store expansion is introduced as a separate permanent-management system rather than pretending an expansion is furniture.

The first expansion is a fixed rear extension of the existing building.

### StoreExpansionDefinition

`StoreExpansionDefinition` is a ScriptableObject containing:

- stable expansion ID
- display name
- description
- purchase price
- required Store Level

The first prototype definition is:

- ID: `rear_extension_01`
- name: `Rear Store Extension`
- price: £100 provisional implementation/test value
- required Store Level: 1
- description: adds a 5-metre-deep indoor strip across the rear of the store

The price is intentionally provisional. Economy balancing is not the goal of Lesson 106.

### StoreExpansionZone

`StoreExpansionZone` is a scene component mapping one expansion definition to its physical scene geometry.

It owns references to:

- the `StoreExpansionDefinition`
- the geometry root that becomes active after purchase
- the locked barrier that blocks the area before purchase
- the expansion floor collider used as buildable floor

A zone can be locked or unlocked at runtime.

The definition asset contains reusable data; the scene component contains scene-object references. This avoids storing scene references inside ScriptableObjects.

### StoreExpansionSystem

`StoreExpansionSystem` is the authoritative runtime owner of expansion purchase state.

It validates that the expansion is configured, not already owned, the store is closed, the day has not ended, there are no active customers, Store Level requirements are met, and sufficient money is available.

A confirmed expansion purchase spends money through `StoreEconomy`, unlocks the zone, disables its old barrier, activates its new geometry, synchronizes physics, and rebuilds NavMesh.

Expansion spending does not count as stock spending.

### StoreBuildArea

Furniture placement no longer treats one `Floor` collider as the entire build authority.

`StoreBuildArea` owns the original base floor collider and configured expansion zones.

It answers whether a ray hit belongs to unlocked buildable floor, whether a furniture footprint is completely inside unlocked build area, and whether a collider should be ignored as floor during collision validation.

This allows future expansions to add buildable floor without resizing one global floor collider.

### FurniturePlacementController integration

`FurniturePlacementController` references `StoreBuildArea` instead of one `storeFloor` collider.

Existing controls remain:

- G: move existing empty furniture
- R: rotate right
- T: rotate left
- E: confirm
- Q: cancel

Existing placement rules remain: grid snapping, body collision, front-access clearance, store-closed requirement, no active customers, and NavMesh rebuild after placement.

## First expansion geometry

The existing store model has an original floor approximately 30 m × 10 m.

Lesson 106 adds a 5 m rear extension beyond the existing `Wall_back`.

Before purchase, the original `Wall_back` remains active, expansion geometry is inactive, and expansion floor is not buildable.

After purchase, the original rear barrier is disabled and the new rear floor, roof extension, side-wall extensions, and new rear wall are activated.

## Store computer management UI

The store computer remains the centralized place for active management.

The existing two-tab interface expands to:

1. PRODUCTS
2. FURNITURE
3. EXPANSION

The former `StoreComputerFurnitureUI` script is renamed to `StoreComputerManagementUI` because it now coordinates multiple management sections.

The Expansion tab uses the same row/scroll-list visual language as Products and Furniture.

Permanent expansion purchases require two clicks:

1. `BUY EXPANSION`
2. `CONFIRM`

Switching tabs or closing/reopening the terminal cancels pending confirmation.

## Save system

Current save version after Lesson 106: **21**.

Version 21 adds `unlockedExpansionIDs` to `StoreSaveData`.

On load, expansion state is restored before furniture layout reconstruction. This is important because saved furniture may physically occupy expansion space.

Version 20 and older saves contain no expansion IDs and therefore load with optional expansions locked.

## First expansion UI palette

The Expansion tab reuses the established store-computer palette:

- primary text: `#594640`
- secondary text: `#7A6860`
- cream row/card: `#FFF9EE64`
- action normal: `#BFE3CF`
- highlighted: `#D2EEE0`
- pressed: `#9FC8B1`

## Backroom cold storage

The existing authored backroom rack and chilled StorageSlot_07 remain unchanged.

Purchasable backroom racks are still deferred. Before implementing them, the project should review whether shelf-display furniture and storage furniture should share a broader persistent furniture identity.

## Spoilage

Spoilage is deliberately deferred until the major management systems are established.

The existing storage-condition architecture remains ready for it, so it can be added later without redesigning product/display fundamentals.

## Immediate roadmap after Lesson 106

### Lesson 107 — broader furniture identity / purchasable backroom storage

Start with a regression pass on the new expansion, then review a broader furniture identity and add purchasable StorageBay furniture without forcing storage racks through shelf-specific persistence.

### Lesson 108 — employee foundation

Begin the first employee system after the manual ordering, receiving, storage, restocking, furniture, expansion, customer and checkout loops are reliable.

A simple restocker is the likely first employee because the player already understands that workflow manually.

### Lesson 109 — integrated progression and space-management review

Review furniture capacity, backroom capacity, expansion pacing, product progression, customer navigation, and save/load before adding another major management subsystem.

### Later development

- spoilage/perishables
- deeper specialized displays
- additional expansion tiers
- furniture selling/recovery
- operating expenses and economy balancing
- art and polish passes
