# Little Market — System Architecture

Current architecture after **Lesson 103**.

This is the complete replacement for:

`Assets/_Game/Documentation/SYSTEM_ARCHITECTURE.md`

## Project principles

Little Market is a cozy first-person Unity 6.3 grocery-store management game.
The player physically orders, receives, stores, carries, restocks and sells real
product objects while gradually arranging and expanding a neighborhood grocery store.

The persistent HUD remains minimal. Management actions are centralized in the
existing store computer, while the Manager Book remains a portable, mostly read-only
source of information. New systems must query the authoritative owner of existing
state rather than duplicating balances, stock, time, progression, transactions or
customer state.

Unity 6.3, URP, the new Input System, Unity AI Navigation/NavMesh, C#, Rider and
GitHub `main` remain the project baseline.

## Current verified source state

Lesson 103 was designed from repository `main` at commit:

`d66c97f3d2aea275df0c2ebd5044dcb8012a3742`

Commit message:

`Lesson 102 complete - furniture purchases through store computer`

That commit remains the verified Lesson 102 baseline. Lesson 103 does not change the
save schema and therefore keeps save version **19**.

## Core gameplay loop

Order products → receive deliveries → organize storage → restock displays → serve
customers → earn money → buy furniture → arrange/expand the store → unlock new
possibilities.

Physical interaction remains the heart of the game. Systems should make logical sense
without becoming a tedious simulation.

## StoreSystems

Scene GameObject: `StoreSystems`.

Major authoritative components include:

- `StoreClock`: current day/time, opening/closing schedule, supplier cutoff and day end.
- `StoreEconomy`: authoritative player money.
- `StoreReputation`: reputation and its customer-arrival influence.
- `StoreProgression`: product catalogue/unlocks.
- `StoreLevelProgression`: store XP and level.
- `StoreGoals`: daily goals and reward state.
- `DailyStats`: current-day sales/customer/satisfaction statistics.
- `ShelfRegistry`: live customer-accessible `ShelfSlot` registration and stock lookup.
- `StorageRegistry`: live backroom `StorageBay` registration.
- `WorldItemRegistry`: loose physical products.
- `CustomerFlow`: spawning, shopping-list generation, traffic capacity and active count.
- `StoreDayLifecycle`: day transition and day-end calculations.
- `StoreReportHistory`: immutable completed-day reports.
- `StoreFurnitureLayout`: persistent shelf layout and purchased-shelf reconstruction.
- `FurnitureShop`: store-computer furniture transaction rules.
- `SaveManager`: serialization and migration orchestration.

`SaveManager` remains the owner of file serialization. It must not become the owner of
furniture mechanics, storage compatibility or product rules.

## Save system

Current save version: **19**.

The save contains day/time, money, reputation, unlocked products, shelf inventory,
storage inventory, supplier/delivery state, loose products, daily statistics, goals,
store level, reports and shelf layout/purchased-shelf records.

Version history relevant to current work:

- v17: historical reports.
- v18: shelf positions/rotations.
- v19: purchased-shelf layout records.

### Lesson 103 save compatibility

Lesson 103 adds no serialized save fields and does not increment the version.

`ProductStorageCondition` is static product/furniture configuration stored in Unity
assets/scenes/prefabs, not per-save state.

This matters because a v19 save may already contain Cloud Milk in an ordinary ambient
backroom bay. `StorageBay.RestoreState` deliberately remains permissive during load:
existing stock is preserved, shown as needing chilled storage, can be removed, and
cannot receive additional units while it remains in an incompatible bay.

This migration rule prevents silent inventory deletion.

Likewise, shelf restore preserves legacy stock rather than deleting it solely because
a storage-condition rule changed. New stocking actions enforce compatibility.

## Player and physical interaction

Scene GameObject: `Player`.

`PlayerController` owns first-person movement/camera/cursor behavior.

`PlayerInteraction` owns the normal physical control language:

- E: primary interaction.
- R: contextual reverse/secondary transfer.
- F: carrying/bulk action.
- Q: drop/cancel.
- Hold X: selected management actions.

`PlayerCarryState` supports held-object state.

`InteractionTargetFinder` performs crosshair target resolution.

Furniture placement uses:

- G: move eligible existing empty shelf.
- R: rotate right 90°.
- T: rotate left 90°.
- E: confirm valid placement.
- Q: cancel.

These controls remain unchanged in Lesson 103.

## ProductData and storage conditions — Lesson 103

`ProductData` remains the authoritative definition of an individual product.

It owns:

- product name/ID;
- `ProductCategory`;
- icon/description;
- physical `PickupItem` prefab;
- sales-display layout/capacity;
- buy/sell prices;
- progression requirements;
- **required storage condition**.

New enum:

`ProductStorageCondition`

Current values:

- `Ambient`
- `Chilled`

New field on each product:

`ProductData.RequiredStorageCondition`

The field is per-product. Product category does **not** imply refrigeration.

For example, Cloud Milk is configured as `Chilled`, but future Dairy products may be
Ambient if that makes sense for that specific product.

`ProductData.CanBeStoredIn(ProductStorageCondition)` is the single compatibility rule used by both retail display and backroom storage. Keeping the rule beside the product requirement avoids duplicating cold-chain logic in `ShelfSlot` and `StorageBay`.

Current rule:

- Ambient products can use Ambient or Chilled equipment.
- Chilled products require Chilled equipment.

This models chilled equipment as a capability rather than an exact-match container. No temperature simulation is introduced.

## Sales shelves and refrigerated display compatibility

Prefab:

`Assets/_Game/Prefabs/Store/PF_Shelf.prefab`

The normal shelf front remains local negative Z.

`SalesShelf` owns shelf-level merchandising rules and now includes:

- `AllowedCategory`
- `StorageCondition`
- `CanDisplayProduct(ProductData)`

Default storage condition is `Ambient`, preserving every existing shelf until a
specific instance is deliberately configured otherwise.

`ShelfSlot` remains the authoritative owner of actual displayed product instances,
assignment, capacity, transfers, customer taking, price tags and registry behavior.

Lesson 103 changes only localized compatibility/restore/prompt sections inside the very large existing `ShelfSlot` and `StorageBay` scripts. Their unrelated stock-transfer, checkout/customer, registry and save behavior is preserved rather than rewritten.

Lesson 103 adds storage-condition enforcement to new stocking actions:

- a chilled product cannot be newly stocked into an Ambient display;
- a Chilled display can hold both Ambient and Chilled products when category rules
  also match;
- category and facing-assignment rules continue to apply independently.

`ShelfSlot` restore is intentionally migration-friendly. If saved stock is now
incompatible because a rule/configuration changed, the stock is restored with a
warning so it can be removed instead of being silently destroyed. New units cannot
be added to that incompatible facing.

The existing `Shelf_Dairy` scene instance is the Lesson 103 prototype refrigerated
display. It retains its current persistent ID and position; only its `SalesShelf`
storage condition changes to `Chilled`. This avoids adding a new authored shelf and
therefore avoids breaking the strict v19 authored-shelf save preflight.

Verified existing scene identity:

- GameObject: `Shelf_Dairy`
- position: approximately `(-3, 0, 4)`
- persistent ID: `04a7290d9e214a6e8463433175f1a326`

The physical shelf model remains placeholder geometry. A refrigerator model is later.

## Backroom storage and cold storage

Prefab:

`Assets/_Game/Prefabs/Store/PF_StorageRack_Backroom.prefab`

`StorageBay` remains authoritative for:

- stable slot ID;
- assigned product;
- quantity/capacity;
- loose-item storage/take;
- delivery-carton merge/fill;
- player restock-basket transfers;
- portable carton creation;
- save restore;
- in-world status display.

Lesson 103 adds a `StorageCondition` field to each bay.

Default: `Ambient`.

Compatibility calls the same `ProductData.CanBeStoredIn(...)` rule used by sales displays.

One existing bay, `StorageSlot_07`, is configured as the first `Chilled` bay.
No new storage-slot ID is introduced, so save restoration remains stable.

The scene contains the existing `PF_StorageRack_Backroom` instance at approximately:

`(-11.754, 0, 4)`

A chilled bay can still hold an ambient product. Doing so consumes cold-storage
capacity and is therefore a player choice rather than a technical prohibition.

If a v19 save restores a chilled product into an Ambient bay, the stock is preserved,
the bay reports that chilled storage is needed, and new units cannot be added until
the old stock is moved.

Spoilage is not active yet.

## Cloud Milk — first chilled product

Asset:

`Assets/_Game/ScriptableObjects/Products/SO_Milk.asset`

Product:

- name: `Cloud Milk`
- ID: `milk_cloud`
- category: Dairy
- buy price: £0.95
- sell price: £2.10
- required reputation: 55
- required store level: 1

Lesson 103 configures:

`Required Storage Condition = Chilled`

This is an individual product rule, not a Dairy-category rule.

## Player restocking basket

`RestockBasket` remains the player-only stock-transfer basket and must not be confused
with customer shopping baskets.

Lesson 103 does not change basket ownership or capacity.

When a basket product is presented to a shelf/bay, the target's existing acceptance
path enforces storage compatibility before transfer. The current empty-container
candidate selection remains the existing first matching product behavior; future
catalogue growth may justify a more sophisticated compatible-item selector.

## Deliveries

`OrderingTerminal` opens `OrderingUI`.

`SupplierDeliveryQueue` owns pending orders and supplier timing.

`DeliveryZone` owns physical receiving slots.

`DeliveryBox` remains the portable carton representation.

Lesson 103 does not block ordering chilled products. The player may still order Cloud
Milk even without enough cold capacity. This is deliberate for the first rules lesson:
equipment planning becomes visible through storage/restocking restrictions. A later
UX pass may show equipment requirements in the catalogue before purchase.

## Customers

`CustomerFlow` and `CustomerController` remain unchanged.

Customers:

- spawn through the established traffic flow;
- use baskets when appropriate;
- browse registered stocked shelf slots;
- take real physical products;
- queue at checkout;
- lose patience/satisfaction according to existing rules;
- pay through `CheckoutRegister`;
- leave with the existing shopping bag.

Customers do not independently evaluate refrigeration. If a product is validly stocked
on the correct display, customer behavior continues normally.

A migrated incompatible shelf can still sell its already-restored legacy stock so it
can naturally leave the system, but players cannot add more there.

## Customer baskets

`PF_CustomerBasket`, `CustomerBasketStation` and `CustomerShoppingBasket` remain visual
carrying mechanisms.

`CustomerController.carriedItems` remains the authoritative customer inventory.

Lesson 103 does not modify customer-basket behavior.

## Checkout

`CheckoutCounter`, `CheckoutRegister`, `CheckoutFeedback` and `CheckoutQueue` remain
the authoritative checkout pipeline.

No refrigeration logic is added to checkout.

Successful payment remains the point at which money/sales are awarded.

## Shelf front/access behavior

Normal shelf front remains local negative Z.

`SalesShelf.IsWorldPositionInFront` remains the shared front-access authority.

Player shelf interactions and customer taking continue to require front-side access.

No Lesson 103 change alters this rule.

## Furniture placement

`FurniturePlacementController` remains on `Player`.

It owns preview positioning, grid snapping, R/T rotation, E/Q confirmation/cancel,
body collision validation, front-access footprint validation and NavMesh rebuild.

Current shelf body/access dimensions remain the Lesson 100 prototype dimensions.

Lesson 103 does not generalize these dimensions because the refrigerated display
prototype reuses the existing shelf geometry.

Different future furniture models will need per-type footprints.

## Persistent furniture

`ShelfIdentity` owns stable shelf identity.

`StoreFurnitureLayout` owns runtime shelf-layout persistence.

Current save records use `ShelfPlacementSaveData` with:

- `shelfID`
- world position
- world rotation
- `purchasedByPlayer`

Version 19 recreates purchased shelves using the single configured `shelfPrefab`.

This is the key reason Lesson 103 does **not** add a purchasable refrigerator yet:
the current persistence layer cannot distinguish purchased regular shelves from
purchased refrigerated displays.

## Store computer and furniture purchasing

The existing `OrderingTerminal` is the central management computer.

`OrderingUI` remains the existing ordering interface.

`StoreComputerFurnitureUI` currently exposes Products/Furniture tabs and one regular
shelf purchase.

`FurnitureShop` currently owns the £40 regular shelf test price and transaction rule.

Money is deducted only when valid placement is confirmed.

Cancelling placement costs nothing.

Furniture spending does not count as `DailyStats.StockSpending`.

Lesson 103 deliberately leaves this transaction untouched.

## Manager Book

The Manager Book remains a portable, mostly read-only information surface.

It owns no product-storage compatibility or spending rules.

No new permanent HUD is introduced for refrigeration.

## Lesson 103 architecture decision

The refrigerator/cold-chain feature is split into two safe layers.

Lesson 103 implements and validates:

1. per-product storage requirement;
2. one shared compatibility rule;
3. retail-display enforcement;
4. backroom-storage enforcement;
5. migration-safe existing-stock behavior;
6. a prototype chilled Dairy display using the existing authored shelf;
7. a prototype chilled backroom bay using an existing slot;
8. no save-version change.

It deliberately does NOT implement:

- a new refrigerator model;
- refrigerator purchase UI;
- refrigerator-specific furniture persistence;
- per-type placement footprints;
- spoilage timers;
- simulated temperature;
- electricity usage;
- automatic prevention of chilled-product ordering.

## Planned dependency order after Lesson 103

### Lesson 104 — purchasable refrigerator and furniture-type persistence

Generalize the Lesson 102 furniture catalogue just enough to support at least:

- Regular Shelf
- Refrigerated Display

Add an explicit furniture-type discriminator to purchased furniture persistence,
migrate version-19 saves safely, restore the correct prefab on load, add the second
computer purchase option and preserve charge-on-confirm.

This will likely require save version 20.

The refrigerator prefab may still use blockout art, but it must carry `Chilled`
`SalesShelf.StorageCondition`.

### Lesson 105 — simple chilled-product spoilage

After correct equipment works:

- introduce an understandable grace period;
- avoid detailed temperature simulation;
- preserve customer fairness;
- make wrong-storage feedback clear;
- carefully define which ownership states count toward spoilage.

### Lesson 106 — combined daily operating expenses

Add one understandable daily operating cost representing rent/electricity and expose
it through the existing financial/reporting ownership.

### Lesson 107 — store expansion foundation

Purchase floor-space expansion through the central management computer, update valid
placement area/walls/navigation and preserve save compatibility.

### Lesson 108 — integrated gameplay review

Review layout, costs, ordering, cold capacity, stock flow, customer flow, progression
and overall pacing before employees or art-heavy work.

## Engineering invariants

- Do not duplicate authoritative inventory.
- Do not derive stock ownership from mesh position.
- Do not silently delete saved stock because a new compatibility rule was introduced.
- Do not infer refrigeration from ProductCategory.
- Do not let UI spend money directly.
- Do not charge furniture until valid placement confirmation.
- Do not count furniture investment as stock spending.
- Do not change authored shelf IDs casually.
- Do not add new authored shelf identities without considering strict v19 layout preflight.
- Do not save/load during active furniture preview.
- Do not rewrite customer/checkout systems for a storage-only feature.
- Do not claim Unity runtime testing unless it actually occurred.

End of complete Lesson 103 architecture document.
