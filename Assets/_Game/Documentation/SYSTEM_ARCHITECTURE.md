# Little Market — System Architecture

Current architecture after Lesson 101. This is the **complete replacement** for
`Assets/_Game/Documentation/SYSTEM_ARCHITECTURE.md`.

## Project principles

Little Market is a cute first-person Unity 6.3 grocery-store simulator. The
player orders, receives and stores real product objects, arranges retail stock,
serves customers, and gradually grows the shop. Gameplay remains physical and
in-world. Keep the persistent HUD minimal, and locate reports and other
management information in the Manager Book.

For every change, establish which component owns the rule. UI and cosmetic
components must query existing authoritative systems rather than duplicate
accounting, inventory, timing or progression. Avoid large rewrites of working
customer, checkout and save paths. Check actual scene/prefab paths before
issuing Inspector instructions. Unity 6.3 uses the new Input System in this
project; Rider is the C# editor.

## StoreSystems

Scene GameObject: `StoreSystems`. Responsibilities include:

- `StoreClock`: day number, in-game clock, opening/closing schedule,
  supplier cutoff and day-end. Other systems query the clock; they do not
  define competing hours. The active scene starts at 07:00, opens at 08:00,
  closes at 21:00, and ends at 22:00. In the inspected scene, the configured
  simulation speed is 10 game minutes per real second; designers may reduce
  it temporarily for placement testing.
- `StoreEconomy`: authoritative player money. Sales, purchasing and eventual
  furniture costs must use this system, not parallel balances.
- `StoreReputation`: reputation and its influence on customer arrivals.
- `StoreProgression`: currently available products and the requirements to
  unlock others; uses the level/progression system.
- `StoreLevelProgression`: store XP and levels.
- `StoreGoals`: live daily goals and reward logic. The day lifecycle coordinates
  their completion; UI must not award them.
- `DailyStats`: live current-day customer, sales and satisfaction statistics.
- `ShelfRegistry`: registered `ShelfSlot` inventory locations and product-stock
  lookup. Shelf slots register/unregister themselves.
- `StorageRegistry`: registered backroom `StorageBay` locations.
- `WorldItemRegistry`: tracks loose physical product instances.
- `CustomerFlow`: customer prefab spawning, traffic capacity, shopping-list
  generation, and active-customer count. Its Inspector currently allows 1–4
  requested items per visitor, with up to five simultaneous customers in the
  inspected scene. It uses `StoreClock.IsStoreOpen` and `StoreReputation`.
- `StoreDayLifecycle`: authoritative day transition, daily calculations,
  goal rewards, reputation/XP/unlocks, daily resets and advancement. It is
  separate from the end-of-day screen.
- `StoreReportHistory`: immutable snapshots of completed-day results, produced
  at day completion. It does not duplicate live `DailyStats` counters.
- `SaveManager`: coordinates persistence; current save version is 18. It
  serializes the existing store/stock/progression/report data. See the Lesson
  99 furniture-movement save guard below.

`DailyReportData` is a serializable snapshot type, not a MonoBehaviour.
Historical reports are ordered by completed day and persist independently of
live daily-stat resets. Never reconstruct past values from current-day data.

## GameUI

Scene GameObject: `GameUI`. UI controllers include `InteractionUI`,
`OrderingUI`, `EndOfDayUI`, `PauseMenuUI`, `ProductCatalogueUI`,
`ManagerBookUI`, `ReportsUI`, `StockWatchUI`, `StoreGoalsUI`, `StoreLevelUI`,
`StorageCapacityUI`, `BasketContentsHUD` and `BasketLocatorUI`.

### Minimal HUD

Show persistent money, day/time/store status and context interaction feedback.
Contextual indicators such as basket contents/locator and brief money feedback
should only appear when useful. Do not turn every management statistic into a
permanent on-screen widget.

### Manager Book

`ManagerBookUI` owns the navigation/modal state for Today, Stock, Catalogue
and Reports. The M key opens the book in normal gameplay. `ProductCatalogueUI`
and `ReportsUI` own the content of their respective pages, not cursor locking
or time scale. Future books may include orders, furniture catalogue, and
upgrades. A furniture catalogue must not replicate economy ownership.

### End of day

`EndOfDayUI` observes `StoreDayLifecycle.IsDayComplete` and presents a short
Day Complete transition with day, revenue, customers served and a relevant
achievement message. `StoreDayLifecycle.StartNextDay()` is the only transition
entry point. The detailed completed-day report is available in Manager Book →
Reports rather than being compulsory at close. `EndOfDayUI` does not award XP,
reputation or money or write reports.

### Reports

`ReportsUI` is a read-only presentation of `StoreReportHistory` with latest
completed day as default, previous/next navigation, finances, customers,
goals, reputation, progression and unlocked product names resolved via
`StoreProgression`. It cannot create/modify reports or live counters.

### Interaction feedback

`InteractionUI.ShowPrompt/HidePrompt` is the existing contextual interaction
line. `PlayerInteraction` updates it for ordinary interactions. In Lesson 99,
`FurniturePlacementController` overrides that same prompt *only while in
furniture mode* (and adds the G hint when looking at an eligible shelf). No
new permanent HUD is created.

## Player and physical interaction

Scene GameObject: `Player`. `PlayerController` owns first-person motion/camera
and cursor capture/release. `PlayerInteraction` owns E primary interaction, R
secondary transfer, F carry/bulk, Q drop, hold-X management, and delegation to
`PlayerCarryState`. `InteractionTargetFinder` raycasts through colliders and
resolves `IInteractable` components on the hit object/parents. It is important
to keep the existing interaction priority intact.

`PickupItem` holds a `ProductData` reference and physical instance. It
transitions among shelf, world, player hand, customer's carry point, scanner,
and other supported containers. `PlaceInContainer()`/`PickUpForCustomer()`
disable colliders and physics when items are visual cargo. Its `PlaceOnShelf()`
uses `PlacementUtility` and actual product display transforms. Product
ownership is never inferred from where a mesh happens to appear.

## OrderingTerminal and deliveries

Scene `OrderingTerminal` lets the player open `OrderingUI` with E and, when
`StoreDayLifecycle` permits, end the day via hold-X.

Scene `DeliveryZone` contains `DeliveryZone` and `SupplierDeliveryQueue`:
`SupplierDeliveryQueue` owns pending orders, due dates and placement into
available receiving slots; it queries `StoreClock.IsPastSupplierCutoff`.
`DeliveryZone` owns physical receiving positions and received cartons.
Portable `DeliveryBox` objects remain distinct from loose items and baskets.

## Storage room

`StorageBay` owns backroom stock assignment/quantity, capacity and physical
carton/basket transfers. `StorageRegistry` finds storage locations. It is not
an independent refrigeration model yet: future chilled products should only
be eligible for the correct storage furniture, but adding cold-chain rules
must reuse `ProductData`/storage furniture compatibility rather than create
another unrelated inventory.

The player's `RestockBasket` and scene `BasketStation` are for stock transfers
between backroom and retail shelves. They are **not** customer shopping
baskets. `BasketLocatorUI` lives in `GameUI`.

## Sales shelves and products

`Assets/_Game/Prefabs/Store/PF_Shelf.prefab` has:

- Root `ShelfIdentity`: scene-instance persistent ID, currently created in
  Unity Editor `OnValidate`. Prefab asset IDs remain blank; the identity
  handles duplicate IDs in editor. Runtime-instantiated purchased furniture
  needs an explicit ID-generation/restore path in a later lesson.
- Root `SalesShelf`: allowed `ProductCategory` and `Customer_Point` used for
  customer navigation and browsing.
- Eight child `ShelfSlot` components, each with a distinct `localSlotKey` and
  a `DisplayOrigin` transform. Stable slot IDs combine parent persistent ID
  with local slot key; older legacy IDs are supported for load compatibility.
- Physical shelf meshes (`Shelf_Back`, `Shelf_Top`, `Shelf_Middle`,
  `Shelf_Bottom`) with child colliders, plus a customer stand point.

`ShelfSlot` owns assigned product, local facing, real stored `PickupItem`s,
customer accessibility, shelf capacity, accepting/removing items, and
registering with `ShelfRegistry`. `ShelfRegistry.TryFindStockedSlot` is the
stocked-product lookup used by customer AI.

`ProductData` owns product ID, category, prefab, wholesale/retail price,
merchandising layout (columns/depth/layers/spacing) and unlock requirements.
The current `ProductCategory` enum includes Cereal, Dairy, Bakery, Produce,
Drinks, Snacks and Household. These are not yet cold-chain requirements;
refrigerated vs ambient storage must be a separate, coherent concept when
fridges arrive.

## Customer AI and checkout

Customer prefab: `Assets/_Game/Prefabs/Characters/PF_CustomerPrototype.prefab`.
`CustomerController` owns the shopping list, state progression, movement
navigation destinations, actual `carriedItems` list, missed-product penalties,
checkout wait penalties, purchase outcome and visit reporting. Never duplicate
customer-owned item or satisfaction accounting in a basket/visual component.

Customers use the entrance and exit inside points under `CustomerTraffic`,
plus the external spawn/exit points. Customer states include walking inside,
visiting the basket station, taking basket, searching, walking to a stocked
shelf, browsing/taking a product, waiting/walking at checkout, and leaving.
Shelf browsing (Lesson 92) uses the existing TakingProduct state: stop at the
shelf, rotate to the slot, animate `Visual.localRotation` forward and back for
~0.85 sec, then call the existing `ShelfSlot.TryTakeItemForCustomer`.

`CheckoutCounter` holds `CheckoutRegister` and `CheckoutFeedback` and is the
single authoritative transaction/scanning interaction. `CheckoutQueue`
coordinates assignment of front/back queue positions and advancing customers.
The checkout reserves a real `PickupItem` while scanning, moves it to the
scanner, increases scanned total/count, hides its GameObject after the scan,
adds a bag visual count, and only awards sales/money on successful payment.
A completed purchase transfers the `ShoppingBag` to the customer for the exit.
No visual lesson may bypass the reservation or payment ownership.

### Customer visual feedback (Lessons 91–93, 98)

- `CustomerQueuePatienceVisual`: a 3D TMP `!` / `!!` overhead based on the
  existing `CustomerController.IsWaitingAtCheckout` and
  `CurrentCheckoutWaitPenalty`; no new patience timer or satisfaction rule.
  The user's readable font size is 20, with amber `#F6D77A` and coral
  `#C46B6B`. The grace-period/penalty remain in `CustomerController`.
- `CustomerWalkVisual`: reads actual `NavMeshAgent.velocity`, applies walking
  bounce/sway or breathing to `Visual.localPosition` only. It does not control
  navigation, visual local rotation or local scale. The customer's two eyes
  are children of `Visual` so they follow these animations naturally.
- `CustomerController` itself animates `Visual.localRotation` when browsing
  and `Visual.localScale` for the happy-payment reaction.
- `CustomerMissingProductVisual`: presentation-only brief `:(` world TMP
  reaction (about 1.3 sec) triggered from the existing `RecordMissingProduct`.
  It does not change the existing 20-point missed-product satisfaction penalty.

## Customer baskets (Lessons 94–99)

Customer prefab contains a root-level `CustomerShoppingBasket` component and
an empty direct child `BasketCarryPoint`, currently near `(0.127,0.78,0.60)`
so the basket appears in front of the body. Basket geometry is separate at
`Assets/_Game/Prefabs/Store/PF_CustomerBasket.prefab`.

Scene `CustomerBasketStation` holds a visible placeholder basket rack and
`PickupPoint`; `CustomerBasketStation.DispenseBasket` instantiates the separate
basket prefab under `BasketCarryPoint`. Customers requesting three or more
products visit the rack and pause briefly before receiving the basket; small
purchases use the existing fixed hand-carry slots. The source is unlimited
for now and has no player restocking chore.

`PF_CustomerBasket` consists of `BasketVisual` non-colliding decorative meshes
and a direct child `ItemSlotsRoot`. Dynamic anchors appear under that root as
actual products are taken; the first four anchors use a two-by-two pattern
and further products use additional layers. `CustomerController.carriedItems`
is still the only authoritative list of purchased physical product instances.

`CustomerShoppingBasket.FitCollectedItem` centres renderer bounds on each
anchor and keeps track of original scales for checkout restoration. **Lesson
99 changes the basket's default to preserve original product size:** the
new `shrinkOversizedItems` Inspector switch is **false**. This is based on the
user's Lesson 98 test: setting the size limits to 10 made the products look
normal again. When `shrinkOversizedItems` is false, *no visual shrinking* is
applied, but centring remains. The width/height/depth limits are only used if
the optional shrink switch is deliberately enabled in a later art pass.
Product meshes may stick out slightly, which is preferable to illegibly tiny
miniature products in this cozy presentation.

`CheckoutRegister` still asks `CustomerController.RestoreBasketItemForCheckout`
before calling `PickupItem.PlaceInContainer(scanPoint)`. That restores the
original item's scale (if changed), and all normal scanning/payment behavior
continues unchanged. After successful checkout the reusable customer basket
instance is destroyed immediately, not displayed as a floating returned item.
The customer exits with the separate checkout `ShoppingBag`.

`CustomerBasketStation` is distinct from the player's `BasketStation` and the
checkout `ShoppingBag`. Basket supply and return management is postponed until
it is actually a meaningful shopkeeper decision. The rack's geometry is
placeholder art and is explicitly deferred to the modelling/art phase.

## Lesson 99: first furniture placement (closed-store testing)

New script: `Assets/_Game/Scripts/Store/FurniturePlacementController.cs`.
Component lives on scene `Player`, alongside `PlayerController` and
`PlayerInteraction`. This is a first-person interaction tool, **not** an
owner of furniture prices, inventory or persistence. It queries:

- The Player's `Main Camera`, `PlayerInteraction` and `GameUI/InteractionUI`.
- `StoreSystems/StoreClock` for whether the shop is open/ended.
- `StoreSystems/CustomerFlow.ActiveCustomerCount` to require zero visitors.
- The existing `store_model/Floor` BoxCollider as a valid store floor surface.
- The existing `Navigation` `NavMeshSurface` to synchronously rebuild paths
  after a confirmed placement.

Controls while the store is closed with no customers:

- Look at any empty `PF_Shelf` collider within ~4m, press **G** to enter
  placement preview. Shelves with any `ShelfSlot.StoredItemCount > 0` cannot
  move, even if another facing is empty.
- Aim the camera at the shop floor: shelf follows the ray intersection on a
  0.5m grid; it remains a real scene shelf, not a purchased clone.
- **R** rotates 90°, **E** confirms only when valid, **Q** cancels and restores
  the exact original position and rotation. `PlayerInteraction` is temporarily
  disabled during placement to avoid E/R/Q conflicting with stock interactions.
- Shelf colliders are temporarily disabled in preview and restored on exit;
  `Physics.OverlapBox` checks a generously sized shelf volume against other
  solid scene objects and player; all four corners must remain inside floor
  bounds. Green preview means valid, coral red means invalid. Visual tint is
  restored without altering shared materials.
- The shelf must have eight existing slots with zero stored items; stock
  ownership and persistent shelf IDs remain unchanged through movement.
- `NavMeshSurface.BuildNavMesh()` refreshes customer navigation **after** a
  valid placement with colliders reenabled. This POC synchronous rebuild can
  be reconsidered for large stores later.
- Furniture mode cancels if the store opens, customers appear, time is paused,
  or the player leaves normal locked-cursor gameplay.

### Lesson 99 saving limitation and guard

At Lessons 99 and 100, the version-17 `StoreSaveData` stored shelf-slot stock
by persistent ID, but **did not store furniture world transforms**. The
`HasUnpersistedFurnitureChanges` guard therefore blocked saving after a
confirmed move. **Lesson 101 supersedes that restriction** with version-18
shelf-layout persistence. Saving and loading while still dragging furniture
remain blocked; confirmed moves are included in the next Save Game.
The source scene and prefab assets still do not move on disk in Play Mode.

**This is a placement prototype, not yet the final furniture purchasing loop.**
No furniture catalogue, spending, selling furniture, room boundary editing,
refrigeration, rent or spoilage is implemented in Lesson 99.

## Lesson 100: shelf fronts, both-way rotation and access footprints

### Consistent orientation (shelf local -Z)

`PF_Shelf` is the current canonical example. Its eight `ShelfSlot`s are
located on local negative Z (`z = -0.3`, facing 180 degrees) and its
`Customer_Point` is `(0, 0, -0.6)`. **Front means local -Z**, including after
the shelf root rotates in the world. Do not infer front from world Z or the
player's camera. A different furniture type will define its own front and
access footprint before joining the placement system.

`SalesShelf.IsWorldPositionInFront(worldPosition)` projects the direction from
shelf root to actor onto the horizontal plane and takes a dot product with
`-transform.forward`. A configurable `frontSideTolerance` default 0.10 avoids
ambiguous access directly beside the shelf's centre line. The method is the
shared authority for front access, not a separate test in every interaction.

- `ShelfSlot.IsActorAtShelfFront(worldPosition)` delegates to `SalesShelf`.
- All player-facing ShelfSlot interactions (normal E, take/reverse R, repeated
  transfer, and Hold X management) reject actors approaching from behind.
  The regular interaction prompt instead says to use the shelf's front.
- Customers already walk toward the shelf's established front
  `CustomerStandPoint`. The final product transfer also checks customer-root
  position against the same front rule, independently of which side the player
  currently sees. A mispositioned/rear-side customer skips the inaccessible
  list entry with a warning rather than looping forever on the same stock.
  This is a defensive fallback, not an excuse to leave unreachable navigation.
- The shelf continues to own actual stock transfer and registration. No stock
  quantities, category restrictions, product assignments or sales values change.

### Closed-store placement improvements

`FurniturePlacementController` remains on `Player` with the same existing
camera, interaction, clock, customer-flow, `storeFloor`, `NavMeshSurface` and
UI references. `G` starts moving an empty shelf; `R` rotates +90 degrees
(right), `T` rotates -90 degrees (left), `E` confirms, `Q` cancels.

In addition to its physical shelf `footprintHalfExtents`/`footprintCenter`, it
now checks a reserved **front access region**:

- `accessCenter` local `(0, 0.80, -1.02)`
- `accessHalfExtents` `(1.25, 0.75, 0.50)`
- `outlineWidth` `0.05`

The access region is approximately 2.5 units wide and 1.0 unit deep in front
of this shelf. It is **not a solid object or blocking collider**: customers
and the player must be able to stand there. The position and dimensions are
prototyping constraints, not finalized aisle regulations. In placement preview,
its outline is displayed in the Game view using a transient LineRenderer that
never blocks raycasts or navigation. Green means valid shelf + access area,
coral means blocked. It is destroyed when the preview exits; associated
runtime Material is also cleaned up.

Both the shelf body and front access corners must lie within the scene's
`store_model/Floor` bounds. `Physics.OverlapBox` rejects solid objects in
either volume. For the access zone only, the player's collider is ignored so
standing in front while placing furniture is not a false obstruction. Other
fixtures and walls still block placement. The old body clearance continues to
check physical collisions. The whole shelf's NavMesh is rebuilt on confirm
with its colliders reenabled, not during preview.

These changes do **not** save furniture positions or change any persistent
layout identity. `SaveManager` now saves confirmed shelf moves in version 18. Active placement
previews remain unsavable. Old saves restore scene-authored shelf positions.
Furniture purchase and sale remain out of scope.

The clearance box is a first working constraint; later furniture types
(fridges, racks, produce) will need distinct body/access dimensions and the
ability to place opposing displays across genuinely usable aisles. Testing
must confirm customers can navigate around placed furniture in practice.

## Planned gameplay systems and dependency order

The planned features should connect to existing authority rather than create
parallel data:

1. **Completed in Lesson 101:** save and restore the positions/rotations of
   existing scene shelves by persistent ID. Next: runtime IDs for purchased
   furniture, then buying a second shelf using `StoreEconomy` and a small
   furniture catalogue/placement workflow.
2. Refrigerated display furniture and **appropriate cold backroom storage**.
   Chilled product requirements should be declared on `ProductData`; both
   sales furniture and storage compatibility must enforce the same rule.
3. Simple spoilage for improperly stored perishables, tied to `StoreClock`
   and clearly signalled to the player. Initially prefer a grace period and
   product-level elapsed storage time, not an elaborate temperature simulation.
   Customers should not be punished for invisible rules.
4. Modest aggregated end-of-day operating cost for rent/electricity. It should
   be charged once by the authoritative day/economy system and shown in the
   Manager Book/report, not as multiple detailed utilities.
5. Store expansion as a **purchasable layout upgrade** with a bigger allowed
   placement zone, updated walls/floor and navigation. Avoid arbitrary
   expansions unrelated to furniture movement and store progression.
6. Employees only after the manual shopkeeper activities feel rewarding;
   defer rosters, wages and schedules until their role is understood.
7. Art pass/modeling for shelves, basket station and store environment later.
   Geometry may improve without rewriting the underlying mechanics.

## Lesson 101: persistent shelf layout (version 18)

The existing `PF_Shelf` already has a `ShelfIdentity.PersistentID` per scene
instance (generated by editor `OnValidate`; the prefab asset itself has a blank
ID). As of the inspected scene, three placed shelves have distinct IDs. Slots
use `<shelfPersistentID>:<localSlotKey>` for their existing stable IDs.

New scene component: `StoreFurnitureLayout` attached to `StoreSystems`, with a
reference to `Navigation/NavMeshSurface`. It is the authoritative runtime
layout service; `FurniturePlacementController` only previews/moves empty
shelves and does not own furniture persistence. `ShelfIdentity` continues to
own individual furniture identity. `SaveManager` remains the single owner of
file serialization.

New data class: `ShelfPlacementSaveData` contains `shelfID`, world `position`
(Vector3), and world `rotation` (Quaternion). `StoreSaveData` includes a new
`List<ShelfPlacementSaveData> shelfPlacements`. **This does not duplicate
inventory**: `shelfSlots` still owns product IDs, assignments and quantities.

On Awake, `StoreFurnitureLayout` captures the original scene-authored world
position/rotation of all existing `ShelfIdentity` objects in the active scene.
It rejects missing or duplicate IDs. `SaveManager.SaveGame` obtains the shelf
placements and writes them with its existing save fields as **version 18**.

On load, `SaveManager.LoadGame` preflights furniture IDs **before** changing
money, inventory, clock or goals. A version-18 save requires every existing
scene shelf ID once and rejects missing/unknown/duplicated entries to avoid
silent loss of stock. Within `ApplySaveData`, layout positions are applied
**before** `RestoreShelfInventory`, followed by `Physics.SyncTransforms` and
`NavMeshSurface.BuildNavMesh()`. Existing shelf IDs are unchanged; stock
restores to those same shelves after their physical position is restored.

When loading **version 17 or older**, no `shelfPlacements` entry is required.
`StoreFurnitureLayout` restores the scene-authored world positions/rotations
captured at Awake and rebuilds navigation; it then allows the older inventory
migration to proceed as before. This also allows loading an old save after a
runtime shelf move without leaving stale positions behind.

`FurniturePlacementController.IsPlacementActive` now exposes only whether an
unconfirmed furniture preview is active. `SaveManager.CanUseSaveSystemNow()`
prevents saving/loading mid-preview but **does not block after confirming a
move**. The old `HasUnpersistedFurnitureChanges` flag and warning are removed.
Existing guardrails (no active customers, no held items/basket/delivery box,
not after day end) are preserved. Pause menu and HUD are unchanged.

This version only supports **scene-authored existing shelves**. It does not
create furniture from save records, sell furniture, or persist player-bought
instances; that's the next lesson. Extending the layout service to instantiate
furniture safely is required before allowing any purchase. The current strict
ID roster check is intentional for version-18 safety, and must be extended
when runtime purchases are implemented.

## Save system and historical compatibility

`SaveManager` on `StoreSystems` coordinates `StoreSaveData` **version 18**. It
still saves day/time, money, reputation, unlocked product IDs, shelf-slot
inventory, storage inventory, supplier orders/deliveries, loose products,
rewards, level and immutable daily report history. Version 17 introduced
`DailyReportData` history; version 18 adds only a per-shelf layout list.
All existing older-save handling remains intact, including historical reports
and the restrictions on saving/loading while customers or held physical
products are present.

Lessons 91–98 added runtime customer visuals and basket presentation; Lessons
99–100 introduced furniture placement and front clearance without changing
schema. Lesson 101 adds per-shelf positions/rotations and an explicit
backward-compatible version-17 fallback. It does not modify the stock list
format, shelf IDs, economy accounting or day lifecycle.

## Ownership and scene-reference rules

Before adding a persistent rule ask: is this represented already, who owns it,
and can the new component query that owner? Examples: the supplier queue must
query `StoreClock.IsPastSupplierCutoff`; it must not define its own cutoff.
Furniture buying must use `StoreEconomy` and shelf identities, not hide stock
in placement scripts. Chilled storage and retail display must share product
requirements. Furniture upgrades must coordinate wall/floor/Navigation.

Before documenting Inspector wiring, verify the actual scene owner and prefab
hierarchy. Avoid vague phrasing such as “object containing the component.”

End of complete Lesson 101 architecture document.
