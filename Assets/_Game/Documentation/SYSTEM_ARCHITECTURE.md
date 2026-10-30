# Little Market — System Architecture

Current architecture after **Lesson 105**.

## Project baseline

Little Market is a cozy first-person grocery-store management game built with Unity 6.3, URP, the Unity Input System, Unity AI Navigation/NavMesh, C#, Rider, and GitHub `main`.

Lesson 105 was designed from GitHub `main` at commit:

`975425777a2d2431626e005f8a6ebfc172df441b`

Commit message:

`Lesson 104 complete - data-driven furniture catalog and refrigerated display`

## Current design priorities

The immediate development priorities are physical store management, product/display logic, furniture, storage, expansion, customer flow, and the manual gameplay loop.

Daily operating expenses are deliberately deferred to a later balancing/economy phase. They are not required for the current core-gameplay foundation.

## Script organization — Lesson 105

`Assets/_Game/Scripts/Store` is no longer treated as one flat bucket. Existing scripts are grouped into feature folders while retaining the project's current global C# namespace.

Current organization introduced in Lesson 105:

- `Store/Core`
    - StoreClock
    - StoreEconomy
    - StoreReputation
    - DailyStats
    - StoreDayLifecycle
    - StoreReportHistory
    - StoreStockOverview
- `Store/Displays`
    - SalesShelf
    - ShelfSlot
    - ShelfRegistry
- `Store/Storage`
    - StorageBay
    - StorageRegistry
- `Store/Furniture`
    - FurnitureCatalog
    - FurnitureDefinition
    - FurnitureShop
    - StoreFurnitureLayout
    - ShelfIdentity
    - ShelfPlacementSaveData
- `Store/Progression`
    - StoreProgression
    - StoreProductCatalog
    - StoreLevelProgression
    - StoreGoals
- `Store/Persistence`
    - SaveManager

This is a folder-organization refactor only. Files are moved from inside Unity so their `.meta` GUIDs remain stable and scene/prefab references are preserved.

Namespaces are intentionally not introduced in the same lesson. A namespace migration would touch many cross-feature references and deserves a separate, deliberate refactor if the project later benefits from it.

## Product category versus display requirement

`ProductCategory` remains product classification data:

- Cereal
- Dairy
- Bakery
- Produce
- Drinks
- Snacks
- Household

Category no longer determines whether a normal sales display may accept the product.

This separates two different questions:

1. What kind of product is this?
2. What kind of physical display does this product require?

## ProductDisplayType — Lesson 105

New enum:

`ProductDisplayType`

Current values:

- `StandardShelf`
- `BakeryDisplay`

`ProductData` now owns `RequiredDisplayType` in addition to `RequiredStorageCondition`.

Default display requirement is `StandardShelf`, preserving existing products unless explicitly configured otherwise.

Current special configuration:

- Sunny Loaf → `BakeryDisplay`
- Cloud Milk → `StandardShelf` + `Chilled`

Cereal products remain `StandardShelf` through the default value.

The design intentionally allows products from different `ProductCategory` values to share a display when their physical display requirement and storage requirement are compatible.

## ProductData compatibility

`ProductData` owns two separate compatibility rules:

- `CanBeDisplayedOn(ProductDisplayType)`
- `CanBeStoredIn(ProductStorageCondition)`

This keeps display form and refrigeration independent.

Examples:

- cereal: StandardShelf + Ambient
- Cloud Milk: StandardShelf + Chilled
- Sunny Loaf: BakeryDisplay + Ambient

A future chilled drink can therefore use StandardShelf + Chilled without being classified as Dairy.

A future bakery product can require BakeryDisplay regardless of whether every product in the Bakery category needs that special presentation.

## SalesShelf

`SalesShelf` now owns:

- `DisplayType`
- `StorageCondition`
- customer stand point
- front-access rule

It no longer owns one `AllowedCategory` restriction.

`CanDisplayProduct(ProductData)` checks both the product's display requirement and storage requirement.

Current configurations:

- normal `PF_Shelf`: StandardShelf + Ambient
- `PF_RefrigeratedDisplay`: StandardShelf + Chilled
- authored `Shelf_Cereal`: StandardShelf + Ambient
- authored `Shelf_Dairy`: StandardShelf + Chilled
- authored `Shelf_Bakery`: BakeryDisplay + Ambient

The historical scene names such as `Shelf_Cereal` and `Shelf_Dairy` do not imply category restrictions anymore.

## ShelfSlot

`ShelfSlot` remains authoritative for one product-facing assignment and its physical item instances.

A shelf can mix categories **between facings**, but a single facing still represents one assigned product at a time.

New stocking validation order:

1. target is customer-accessible;
2. product's required display type matches the parent display;
3. product's storage requirement is satisfied;
4. facing is empty or already assigned to the same product;
5. facing has capacity.

This allows, for example, multiple product categories on a normal shelf without allowing a BakeryDisplay product onto a StandardShelf.

Legacy/incompatible saved stock continues to be restored rather than silently deleted. New incompatible units cannot be added, and prompts explain the required display/storage type.

## RestockBasket

`RestockBasket` keeps its existing physical-item ownership.

Lesson 105 adds a predicate-based product lookup so an empty shelf facing can find the first product in the basket that is actually compatible with that display, rather than searching only by one category.

The old category lookup overload is retained for compatibility with any existing callers, but the sales-shelf flow no longer depends on it.

## Bakery display

Sunny Loaf is the first product configured with:

`Required Display Type = Bakery Display`

The authored `Shelf_Bakery` is configured as:

- Display Type = Bakery Display
- Storage Condition = Ambient

Its current physical model remains the existing shelf blockout. A future bakery-case prefab can expose the same display type without changing Sunny Loaf's product logic.

## Refrigerated display

The Lesson 104 refrigerated display changes conceptually from “Dairy-only fridge” to a general chilled standard-shelf display:

- Display Type = StandardShelf
- Storage Condition = Chilled

This means it can hold any product whose physical display requirement is StandardShelf and whose storage requirement is satisfied.

Because Chilled storage also satisfies Ambient products, putting an ambient StandardShelf product in the refrigerator is technically allowed. That is treated as a player space-management choice rather than an artificial prohibition.

The furniture definition description should describe refrigeration rather than Dairy category restriction.

## Furniture system

Lesson 104's data-driven furniture architecture remains unchanged:

- FurnitureDefinition owns furniture ID, player-facing name/description, price, prefab, and placement footprint.
- FurnitureCatalog owns available definitions.
- FurnitureShop owns purchase transaction rules.
- StoreFurnitureLayout owns persistent reconstruction for the current shelf-display furniture family.
- FurniturePlacementController reads per-definition placement footprints.

No save schema changes are required in Lesson 105.

## Save system

Current save version remains **20**.

Product display requirements and display capabilities are static Unity asset/scene/prefab configuration rather than per-save state.

No save-version increment is required.

Shelf inventory restore remains migration-friendly: if historical stock becomes incompatible due to a rule/configuration change, it is restored with a warning and can be removed rather than being deleted.

## Store computer furniture descriptions

The data-driven furniture catalog remains the central purchasing UI.

Descriptions should now reflect physical capability rather than category:

- Standard Shelf: `Ambient shelf • General packaged products.`
- Refrigerated Display: `Chilled shelf • For products that need refrigeration.`

The store computer remains the single active-management location.

## Manager Book

The Manager Book remains primarily read-only and gains no controls from Lesson 105.

## Immediate roadmap after Lesson 105

### Lesson 106 — simple spoilage foundation

Introduce a clear grace-period spoilage rule for chilled products stored incorrectly. Avoid detailed temperature simulation and preserve existing saves carefully.

### Lesson 107 — purchasable backroom cold storage / furniture identity review

Review whether shelf-display and storage furniture should share a broader persistent `FurnitureIdentity` before making storage racks purchasable. Do not force `StorageBay` furniture through `ShelfIdentity` merely for code reuse.

### Lesson 108 — store expansion foundation

Introduce purchasable floor-space expansion and ensure placement boundaries, floor/walls, navigation, and save behavior agree.

### Lesson 109 — integrated gameplay review

Play through ordering, delivery, storage, refrigeration, display requirements, furniture purchasing, save/load, customers, checkout, and progression before another major subsystem.

### Later development

- employees after the manual gameplay loop is mature;
- deeper furniture variety and specialized displays;
- economic balancing and daily operating expenses;
- art/polish passes after the relevant gameplay foundations are stable.
