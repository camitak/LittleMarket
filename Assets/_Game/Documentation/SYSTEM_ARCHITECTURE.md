# Little Market System Architecture

This document describes the existing owners of Little Market's systems and the rules for adding features without duplicating authority. Current through Lesson 97, based on the `main` repository and the verified Lesson 96 implementation.

**Rule:** Extend or query the existing owner of a rule rather than implementing that rule independently in another system.

## StoreSystems

Scene GameObject: `StoreSystems`.

Store-wide systems include:

- `StoreClock`
- `StoreEconomy`
- `CustomerFlow`
- `ShelfRegistry`
- `StorageRegistry`
- `SaveManager`
- `StoreProgression`
- `StoreReputation`
- `StoreGoals`
- `StoreLevelProgression`
- `StoreStockOverview`
- `WorldItemRegistry`
- `StoreDayLifecycle`
- `StoreReportHistory`

### StoreClock

Owns the current day and game time, opening time, supplier same-day cutoff, closing time, and hard day-end time.

- `CustomerFlow` queries whether the store is open.
- Supplier and ordering systems query supplier cutoff and delivery timing.
- `OrderingTerminal` queries closing conditions.

Other components must not maintain separate authoritative schedule values.

### StoreDayLifecycle

Scene GameObject: `StoreSystems`; component: `StoreDayLifecycle`.

Coordinates day completion **once per day**, after `StoreClock` reports the day ended and `CustomerFlow` reports no active customers.

End-of-day processing order:

1. Apply daily satisfaction through `StoreReputation`.
2. Claim rewards through `StoreGoals`.
3. Apply experience and level progression through `StoreLevelProgression`.
4. Refresh unlocked products through `StoreProgression`.
5. Ask `StoreReportHistory` to snapshot the completed day.

Starting the next day resets daily state and uses the existing clock/customer flow. It does not make `EndOfDayUI` the owner of rewards or progression.

`EndOfDayUI` only observes `StoreDayLifecycle.IsDayComplete` and requests `StartNextDay()`.

### StoreReportHistory

Scene GameObject: `StoreSystems`; component: `StoreReportHistory`.

Stores read-only historical results after end-of-day processing. Each `DailyReportData` entry captures one completed day: finances, customer results, reputation, goal rewards, experience, ending level/cash, and newly unlocked stable ProductIDs.

- `DailyStats` still owns **live current-day** counters.
- `StoreReportHistory` does not reconstruct past days from live statistics.
- The report collection is day-ordered, deduplicated, and persisted by `SaveManager`.
- There is no arbitrary history cap in the current prototype.
- Older saves cannot reconstruct historical reports that were never recorded.

## GameUI

Scene GameObject: `GameUI`.

Screen-space UI controllers include:

- `InteractionUI`
- `OrderingUI`
- `EndOfDayUI`
- `PauseMenuUI`
- `ProductCatalogueUI`
- `ManagerBookUI`
- `ReportsUI`
- `StockWatchUI`
- `StoreGoalsUI`
- `StoreLevelUI`
- `StorageCapacityUI`
- `BasketContentsHUD`
- `BasketLocatorUI`

### HUD information rule

Normal gameplay HUD remains minimal: money, day/time/store status, and contextual interaction feedback. Other information should be available only when useful:

- Restock basket contents while carried.
- Basket locator on request.
- Money-gain feedback when relevant.

Management detail belongs in `ManagerBookUI`, not on a permanently crowded HUD. Customer's world-space patience indicator is not a permanent HUD element.

### ManagerBookUI

Current pages: **Today, Stock, Catalogue, Reports**.

`ManagerBookUI` owns page navigation, pause/modality, cursor unlocking, and disabling/restoring player controls. Individual pages do not own a competing modal state.

- `ProductCatalogueUI` presents catalogue data.
- `ReportsUI` presents completed-day history.

Possible future pages: Orders and store progression/upgrades, only when they serve a demonstrated gameplay need.

### EndOfDayUI

Scene GameObject: `GameUI`; component: `EndOfDayUI`.

Lesson 90's compact transition screen:

- Waits for `StoreDayLifecycle.IsDayComplete`.
- Displays completed day, sales revenue, customers served, and one short achievement or neutral message.
- Uses the existing Start Next Day action through `StoreDayLifecycle.StartNextDay()`.
- Temporarily manages cursor, player controls and paused gameplay for the transition.

The former expanded report and View Details button were removed. Detailed reporting belongs in the Manager Book. EndOfDayUI does not award money, reputation, goal rewards, XP, unlocks or record report history.

### ReportsUI

Scene GameObject: `GameUI`; component: `ReportsUI`.

Presentation-only read access to `StoreReportHistory`:

- Empty history message.
- Latest completed day when opened.
- Previous/Next controls, disabled at boundaries.
- Historic finances, customers, goals, satisfaction/reputation, experience, and unlocked product names (resolved from saved ProductIDs through `StoreProgression`).

`ReportsUI` does not track live counters, calculate rewards, modify histories, persist data, or manage the Manager Book's modal state.

### Reports direction

The basic historical Reports page works. Optional future improvements: visual hierarchy, limited day-to-day comparisons, and useful trends. Reporting is not a compulsory separate gameplay screen.

## OrderingTerminal

Scene GameObject: `OrderingTerminal`; component: `OrderingTerminal`.

Physical point for opening `OrderingUI` with E and ending the day with Hold X when allowed. It queries existing `StoreClock` and `CustomerFlow` conditions rather than owning them.

## DeliveryZone

Scene GameObject: `DeliveryZone`.

- `DeliveryZone`: physical receiving positions and received cartons.
- `SupplierDeliveryQueue`: pending supplier orders, arrival-day data, and unloading due deliveries when receiving capacity is available.

The supplier queue queries `StoreClock` for timing; it does not define a second schedule.

## CheckoutCounter

Scene GameObject: `CheckoutCounter`.

Components: `CheckoutRegister`, `CheckoutFeedback`.

The checkout owns the active manual scan/payment transaction, scan point, shopping-bag preparation, checkout queue interaction, and physical feedback.

`CheckoutRegister` reads `CustomerController.GetCarriedItemCount()`, `GetCarriedItem(index)`, and `GetCarriedProductData(index)`; it does not use a basket-owned inventory or recalculate a separate basket total.

Each successful scan:

1. Reserves the specific item being scanned.
2. Asks `CustomerController.RestoreBasketItemForCheckout(item)` to restore an item visually resized for the basket (Lesson 97); this is a no-op for hand-carried items.
3. Calls the **existing** `PickupItem.PlaceInContainer(scanPoint)` to show the actual product at the scanner.
4. Waits for the existing scanner display duration and validates the reservation.
5. Counts the scanned product and adds its actual `ProductData.SellPrice` to the total.
6. Hides the scanned `PickupItem` object while it remains in `CustomerController.carriedItems` until successful payment.
7. Updates `ShoppingBag`'s existing scan feedback.

`CustomerController.CompleteCheckout(ShoppingBag)` still removes the actual items and clears the authoritative customer inventory. Checkout revenue and daily stats are still recorded through the existing checkout/economy/stat systems. **Lesson 97 does not change the transaction or payment rules.**

## Customer checkout feedback (Lesson 91)

Customer prefab: `Assets/_Game/Prefabs/Characters/PF_CustomerPrototype.prefab`.

`CustomerController` owns queue states, waiting time, and satisfaction penalty. It exposes read-only `IsWaitingAtCheckout` and `CurrentCheckoutWaitPenalty` properties.

`CustomerQueuePatienceVisual` reads those properties. After the existing grace period, an amber `!` appears; at its configurable visual critical penalty (default 15), a coral `!!` appears. The 3D TextMeshPro label bobs and faces the camera. Its font size 20 is a verified readable prototype setting.

Visual feedback does not alter customer satisfaction, queue positions, payments or timing rules.

## Customer shelf browsing (Lesson 92)

Customer prefab root component: `CustomerController`.

- The established `SalesShelf.CustomerStandPoint` remains the shopper destination.
- On arrival, the existing `TakingProduct` state lasts for a configurable browse duration.
- The customer's root rotates toward the selected `ShelfSlot`; its child `Visual` briefly leans and returns upright.
- NavMeshAgent automatic rotation is disabled only for browsing, then restored.
- `ShelfSlot.TryTakeItemForCustomer()` still controls stock removal, and the customer retries searching if another customer took the last item.

The browsing animation adds pacing/presentation without changing the shopping list, inventory accounting, prices or satisfaction.

## Customer walking and idle animation (Lesson 93)

Customer prefab root component: `CustomerWalkVisual`; script: `Assets/_Game/Scripts/Customers/CustomerWalkVisual.cs`.

- Reads `NavMeshAgent.velocity` rather than creating a second movement state.
- Animates only `Visual.localPosition`: light walking bounce and sway, transitioning to subtle idle breathing.
- Leaves `Visual.localRotation` for shelf browsing and `Visual.localScale` for the existing happy checkout reaction.
- Does not move the actual NavMeshAgent, carry slots, basket, patience indicator or world items.
- The `Eye_left` and `Eye_right` children of `Visual` move with this presentation.

## Customer shopping baskets (Lessons 94–97)

Customer prefab: `Assets/_Game/Prefabs/Characters/PF_CustomerPrototype.prefab`.

Basket prefab: `Assets/_Game/Prefabs/Store/PF_CustomerBasket.prefab`.

Scripts: `CustomerController`, `CustomerShoppingBasket`, `CustomerBasketStation`.

### Responsibilities

- `CustomerFlow` generates configurable shopping lists (current scene playtest range 1–4) and controls visitor spawning/capacity.
- Customers requesting at least three items (configurable) visit the basket rack. Shorter trips continue with fixed hand-carry slots.
- `CustomerBasketStation.DispenseBasket()` instantiates the separate `PF_CustomerBasket` asset beneath the customer root's `BasketCarryPoint`, after pickup. The rack is an effectively unlimited source at this stage.
- Customer prefab includes a direct child `BasketCarryPoint`, not embedded basket meshes. Current local position is approximately `(0.127, 0.78, 0.6)`; preserve the player's front-facing arrangement.
- `PF_CustomerBasket` holds `BasketVisual` mesh pieces and `ItemSlotsRoot`, both directly beneath the basket root. The basket does not contain AI, inventory state, purchase data, Rigidbody, or checkout payment behavior.
- `CustomerShoppingBasket` creates position anchors only as products are collected. `CustomerController.carriedItems` alone owns the authoritative list of actual acquired `PickupItem` objects.
- `ShelfSlot.TryTakeItemForCustomer()` uses `PickupItem.PickUpForCustomer()` to move the actual product from shelf to the requested anchor, with collisions and gravity disabled.

### Basket presentation (Lesson 97)

The basket's product arrangement is **only visual**. It must not affect prices, availability, item IDs, acquired-item order, scan indexes, customer satisfaction, or daily statistics.

- The layout uses **two columns and two rows per layer**, instead of the old three-row first layer; additional quantities continue on higher layers.
- Basket component initial values: `columnSpacing = 0.30`, `rowSpacing = 0.21`, `layerSpacing = 0.16` (Unity local units).
- In the basket prefab, `ItemSlotsRoot` starts at local `(0, 0.04, 0)` instead of `(0, 0.16, 0)`, putting the center of the first layer *inside* the basket rather than above its rim.
- After a successful pickup, `CustomerController` calls `CustomerShoppingBasket.FitCollectedItem(item)` once. This stores the actual item instance's original local scale, uses its enabled visible renderers' world bounds, scales **down but never up** to fit maximum presentation dimensions, then visually centers the bounds on that item's allocated carry point.
- Initial maximum presentation dimensions: width `0.24`, height `0.20`, depth `0.16` world units. Fit dimensions are an art/presentation constraint, not an inventory-capacity rule.
- Because this simple bounds approach uses world-space axis-aligned bounding boxes, product shapes and rotations can still need later art review. Renderer bounds may include visible labels. The goal is better prototype packing, not a final physically accurate stacking simulation.
- Original scales are held transiently in a dictionary keyed by the **same `PickupItem` object**, not a second product or payment list.
- Immediately before the existing scanner transfer, `CheckoutRegister` calls `CustomerController.RestoreBasketItemForCheckout(item)`, which delegates to `CustomerShoppingBasket.RestoreItemForCheckout(item)` and restores the product's original scale.
- The scanner then moves that same real item to `scanPoint` and continues its pre-existing scan/reservation/payment workflow. Scanned products visibly leave the basket and do not remain duplicated inside it.
- `CustomerShoppingBasket.ClearAfterCheckout()` destroys the temporary basket immediately after successful payment. `DiscardBasket()` handles abandoned shopping/cleanup, including clearing temporary scale bookkeeping.

Do not use the basket to hold a second purchased-item collection. Hand-carried products never enter the basket-fitting path.

### What is deliberately not implemented

- No fixed three-item basket maximum, new basket stock system, or player basket-restocking chore.
- No independent customer basket save data; customer visits remain runtime-only.
- No packing physics or reparenting of decorative clones.
- No replacement of checkout payment/scanning, shelf stock removal, or store day lifecycle.

The customer basket is distinct from the player's `RestockBasket` and the checkout's `ShoppingBag`.

## Customer basket pickup station (Lessons 95–96)

Scene GameObject: `CustomerBasketStation` near the entrance.

Component: `CustomerBasketStation`; script: `Assets/_Game/Scripts/Customers/CustomerBasketStation.cs`.

- `pickupPoint` is `CustomerBasketStation/PickupPoint` and must remain reachable by NavMesh.
- `basketPrefab` points at the separate `PF_CustomerBasket` asset.
- `WalkingToBasketStation` and `TakingBasket` are existing `CustomerState` values used before basket shopping. There is a short pause at the rack.
- The simple stack of cube meshes on `RackVisual` is placeholder art only; its visual form may improve independently of basket AI.
- The former returned-basket presentation, `CheckoutReturnPoint`, and five-second cleanup were removed in Lesson 96. The basket disappears immediately after payment; the customer exits with the existing purchased `ShoppingBag`.

No finite stock, player collection requirement or second store resource is created by this station. Basket restocking should be considered only if it produces meaningful player decisions during day playtests.

## BasketStation

Scene GameObject: `BasketStation`; component: `BasketStation`.

This is the **player's restock basket** home/recovery point, unrelated to the customer basket rack. Temporary locator UI is on `GameUI`.

## Player

Scene GameObject: `Player`.

`PlayerController` and `PlayerInteraction` own player movement and action coordination. Carry-state and target interpretation are delegated to the existing specialized components. Established controls include E interact, R reverse, F carry, Q drop, Tab restock basket, M Manager Book, and Hold X end-day at the terminal.

## Sales Shelves

Prefab: `Assets/_Game/Prefabs/Store/PF_Shelf.prefab`.

- Root `ShelfIdentity` owns persistent furniture identity.
- Root `SalesShelf` owns allowed category and customer stand point.
- Child `ShelfSlot`s own product-facing assignment and stored products.
- ProductData owns item-specific merchandising arrangement; do not duplicate that in every category-specific shelf transform.

## Persistence

`SaveManager` is on `StoreSystems`, coordinating serialization, not gameplay authority.

**Current save version: 17.**

Version 17 added historical `DailyReportData` entries to `StoreSaveData`. SaveManager exports/restores reports through `StoreReportHistory`; older saves load with empty historical reports rather than invented backfilled records. Existing save restrictions remain unchanged.

- Lesson 91: patience visuals and read-only customer penalty access; no extra saved fields.
- Lesson 92: runtime browsing timing and lean; no extra saved fields.
- Lesson 93: runtime walking/idle body motion; no extra saved fields.
- Lesson 94: basket carry points and configurable shopping-list size; no extra saved fields.
- Lesson 95: basket pickup/return presentation; no extra saved fields.
- Lesson 96: separate basket prefab and immediate basket cleanup; no extra saved fields.
- Lesson 97: per-item basket fit and restoration of runtime visual scale during scanning; **no new persistence fields or save-version change**.

## Ownership rule

Before adding a serialized gameplay rule:

1. Does another system already own this rule?
2. Which component is its authoritative owner?
3. Can the new behavior query that component rather than duplicate its data?

Example: `SupplierDeliveryQueue` queries `StoreClock.IsPastSupplierCutoff`; it must not define a second conflicting same-day cutoff.

## Scene reference rule

Before documenting an Inspector assignment, check the actual owning GameObject in `Market.unity` and the actual prefab hierarchy in GitHub. Keep field names and paths exact; do not write generic instructions when ownership is known.
