# Little Market — System Architecture

Current architecture after **Lesson 104**.

## Project baseline

Little Market is a cozy first-person grocery-store management game built with Unity 6.3, URP, the Unity Input System, Unity AI Navigation/NavMesh, C#, Rider, and GitHub `main`.

The player physically orders, receives, stores, carries, restocks, arranges, and sells products. The persistent HUD stays minimal. The existing store computer is the central place for active management; the Manager Book remains primarily read-only.

Lesson 104 was designed from GitHub `main` at commit:

`24c653756ecd4221d33ad11fa5b093b225e087c5`

Commit message:

`Lesson 103 complete - shared chilled storage rules and cold-chain prototype`

## Core authority rules

- `StoreEconomy` owns money.
- `StoreClock` owns day/time/open-state rules.
- `CustomerFlow` owns active customer flow.
- `StoreProgression` owns product unlocks.
- `ProductData` owns individual product data and storage requirements.
- `ShelfSlot` owns displayed product instances and facing assignments.
- `StorageBay` owns backroom quantity storage.
- `StoreFurnitureLayout` owns persistent shelf-display layout and reconstruction.
- `FurnitureShop` owns purchase transaction rules but does not own furniture prices or prefabs.
- `SaveManager` owns serialization and migration orchestration.

## Save system

Current save version after Lesson 104: **20**.

Version history relevant to furniture:

- v18: authored shelf positions and rotations.
- v19: purchased shelf records via `purchasedByPlayer`.
- v20: purchased furniture records also store a stable `furnitureDefinitionID`.

`ShelfPlacementSaveData` contains:

- `shelfID`
- world `position`
- world `rotation`
- `purchasedByPlayer`
- `furnitureDefinitionID` for purchased version-20 entries

Version-19 purchased shelves have no definition ID. `StoreFurnitureLayout` migrates them through an explicit `legacyVersion19ShelfDefinition`, configured to the standard shelf definition. When that migrated game is saved again, the entry becomes a normal version-20 record with `standard_shelf`.

Version-18 saves remain authored-layout-only. Version 17 and older continue to use the existing authored-position fallback.

## Product storage conditions

`ProductData` owns `RequiredStorageCondition` and the shared `CanBeStoredIn(...)` compatibility rule.

Current storage conditions:

- Ambient
- Chilled

Ambient products can use ambient or chilled equipment. Chilled products require chilled equipment.

Cloud Milk (`milk_cloud`) is currently Chilled.

## Sales displays

`SalesShelf` owns:

- allowed merchandising category
- storage condition
- customer stand point
- front-access rule

`ShelfSlot` owns the actual displayed physical stock and delegates storage compatibility to `ProductData`.

Shelf front remains local negative Z.

The authored `Shelf_Dairy` remains the starter chilled display and keeps persistent ID:

`04a7290d9e214a6e8463433175f1a326`

## Backroom storage

`StorageBay` owns backroom assignment, quantity, capacity, transfer operations, save restore, and in-world status.

`StorageSlot_07` remains the starter chilled backroom bay.

Lesson 104 does not yet make backroom racks purchasable. Furniture purchasing in this lesson still covers the current shelf-display family. A later generalization must not pretend `ShelfIdentity` already represents every possible furniture family.

## Furniture definitions — Lesson 104

New `FurnitureDefinition` ScriptableObject is the authoritative data definition for a purchasable shelf-display type.

It owns:

- stable `FurnitureID`
- display name
- player-facing description
- purchase price
- prefab reference
- body placement half-extents and center
- front-access half-extents and center

Prices and prefab selection no longer live as one-off fields in `FurnitureShop` or `StoreFurnitureLayout`.

Current definitions:

### Standard Shelf

- ID: `standard_shelf`
- price: £40
- prefab: `PF_Shelf`
- ambient display
- current purchased prefab starts with the existing Cereal category configuration

### Refrigerated Display

- ID: `refrigerated_display`
- price: £80 provisional test price
- prefab: `PF_RefrigeratedDisplay`
- Dairy category
- Chilled storage condition

Both currently use the same blockout physical footprint because the refrigerated prefab is derived from the existing shelf geometry. The definition stores the footprint so later furniture can have genuinely different dimensions without adding furniture-specific constants to the placement controller.

## Furniture catalog

New `FurnitureCatalog` ScriptableObject owns the ordered list of available `FurnitureDefinition` assets.

`FurnitureShop` and `StoreFurnitureLayout` reference the same catalog asset. This is a shared reference to one data source, not duplicated catalog data.

Current catalog order:

1. Standard Shelf
2. Refrigerated Display

## FurnitureShop

`FurnitureShop` is now a transaction service rather than a catalog-data owner.

It checks:

- configuration
- requested definition belongs to the configured catalog
- shop is closed and the day has not ended
- no active customers
- player holds nothing
- no placement preview already active
- player can afford the selected definition

It does not contain `shelfPrice` or refrigerator price fields.

Money is still deducted only when valid placement is confirmed with E. Q cancels for free.

Furniture spending still does not count as `DailyStats.StockSpending`.

## StoreFurnitureLayout

`StoreFurnitureLayout` continues to own persistent layout/reconstruction for the current shelf-display furniture family.

It now receives a `FurnitureDefinition` when creating a purchased preview. The definition chooses the prefab.

Each purchased `ShelfIdentity` receives:

- a runtime persistent instance ID
- its `FurnitureDefinition`

On version-20 load, `StoreFurnitureLayout` uses `furnitureDefinitionID` to resolve the correct definition through `FurnitureCatalog`, instantiate the correct prefab, restore the persistent shelf ID, register `ShelfSlot`s, and rebuild NavMesh before shelf inventory restoration.

Authored shelves remain scene objects and are restored by their existing persistent IDs.

## ShelfIdentity

`ShelfIdentity` still owns the stable instance ID used by shelf slots.

Lesson 104 adds a `FurnitureDefinition` reference so placement can obtain the correct footprint for both authored and purchased shelf-display furniture.

Authored scene assignments:

- `Shelf_Cereal` → Standard Shelf definition
- `Shelf_Bakery` → Standard Shelf definition
- `Shelf_Dairy` → Refrigerated Display definition

Purchased instances receive the definition at runtime from `StoreFurnitureLayout`.

The prefab assets themselves do not need to reference the ScriptableObject definition, avoiding a prefab ↔ definition circular asset dependency.

## Furniture placement

`FurniturePlacementController` remains on Player and preserves:

- G move existing empty shelf-display furniture
- R rotate right
- T rotate left
- E confirm
- Q cancel
- grid snapping
- store-floor bounds checks
- physical collision checks
- visible front-access footprint
- NavMesh rebuild after confirmation

Lesson 104 changes footprint ownership. The controller now reads body/access dimensions from `ShelfIdentity.FurnitureDefinition` when available. Existing serialized placement dimensions remain fallback values for compatibility.

Preview colors remain:

- valid: `#6FCF97`
- invalid: `#C46B6B`

## Refrigerated display prefab

New prefab:

`Assets/_Game/Prefabs/Store/PF_RefrigeratedDisplay.prefab`

It is derived from the current `PF_Shelf` blockout structure so all eight existing `ShelfSlot` mechanics, customer access, price tags, and placement behavior remain compatible.

Configuration:

- `SalesShelf.AllowedCategory = Dairy`
- `SalesShelf.StorageCondition = Chilled`

A small non-colliding blue blockout accent differentiates it visually from a normal shelf until the art pass.

## Store computer UI

`OrderingTerminal` remains the single active-management computer.

`OrderingPanel` remains 760 × 720 on the existing GameUI Canvas, whose CanvasScaler reference resolution is 1920 × 1080 with Match 0.5.

Lesson 104 cleans the layout instead of adding more manually positioned furniture controls.

The terminal uses:

- title area
- status area
- shared two-tab row
- one content rectangle used alternately by Products or Furniture
- bottom Close button

Products retain the existing `PF_OrderingProductRow` scroll-list pattern.

Furniture now uses a matching scroll list with `PF_FurnitureCatalogRow`. `StoreComputerFurnitureUI` dynamically instantiates one row per catalog definition. There is no dedicated `buyShelfButton` or `shelfPriceText` field anymore.

## Store computer palette

The Lesson 104 terminal deliberately reuses colors already present in the existing product-row UI:

- primary text: `#594640`
- secondary text: `#7A6860`
- main panel: `#FFF4DEAE`
- cream row/card: `#FFF9EE64`
- action normal: `#BFE3CF`
- action highlighted: `#D2EEE0`
- action pressed: `#9FC8B1`
- standard disabled: `#D8D4CC80`

Active tabs use the same mint family instead of introducing a new visual language.

## Furniture UI row

`FurnitureCatalogRow` binds one `FurnitureDefinition` to:

- furniture name
- description
- price
- Buy & Place button

The row never spends money directly. It raises a request back to `StoreComputerFurnitureUI`, which delegates purchase validation and transaction handling to `FurnitureShop`.

## OrderingUI integration

`OrderingUI` still owns product ordering and the terminal open/close/cursor lifecycle.

A public `RefreshTerminalStatus()` method exposes the existing supplier-delivery status refresh so switching back from Furniture to Products restores the correct product-ordering status instead of leaving furniture text in the shared status area.

## Manager Book

The Manager Book remains mostly read-only and receives no furniture purchasing controls.

## Lesson 104 deliberate scope boundary

Implemented:

- data-driven furniture definitions
- shared furniture catalog
- price/prefab removal from `FurnitureShop`
- correct prefab selection for purchased displays
- per-definition placement footprints
- version-20 furniture-type persistence
- version-19 purchased-shelf migration
- purchasable refrigerated Dairy display
- structured furniture catalog UI
- exact terminal layout and palette cleanup

Not implemented yet:

- player category configuration for a purchased standard shelf
- purchasable backroom cold-storage rack
- selling furniture
- furniture delivery shipments
- spoilage
- electricity simulation
- store expansion
- employees

The current purchased Standard Shelf therefore retains the existing `PF_Shelf` default Cereal category. The UI states this rather than hiding the limitation. Category configuration should be addressed intentionally rather than slipped into the purchase system.

## Next planned lessons

### Lesson 105 — simple spoilage foundation

Add a clear grace-period spoilage rule for chilled products stored incorrectly. Avoid detailed temperature simulation and preserve old saves carefully.

### Lesson 106 — purchasable backroom cold storage / furniture-family review

Before extending the catalog to storage racks, review whether shelf-specific persistence should remain separate or be generalized with a broader furniture identity. Do not force StorageBay furniture through `ShelfIdentity` merely to reuse code.

### Lesson 107 — combined daily operating expenses

Add one understandable operating-cost entry through the authoritative day/economy/report systems.

### Lesson 108 — store expansion foundation

Add a purchasable layout expansion with updated valid placement area, walls/floor, and navigation.

### Lesson 109 — integrated gameplay review

Play through ordering, deliveries, ambient/chilled storage, furniture purchasing, save migration, customers, checkout, and progression before starting another large subsystem.
