# Little Market — System Architecture

**Repository:** `camitak/LittleMarket`  
**Verified committed source:** `main` at `5ffd83f1d5f815b94f5d5b94079f49839325bbed` — **Lesson 151 complete - append - changing delivery box quantity**  
**Inspected:** 13 November 2026  
**Unity:** 6000.6.0f1, from `ProjectSettings/ProjectVersion.txt`.

> **Verification boundary:** These facts describe committed GitHub files inspected at this SHA, not uncommitted Unity work. Lesson 152 code/UI changes have not been applied, compiled, playtested or committed. This replaces a stale architecture file still stamped Lesson 150. Re-read GitHub main before every future lesson.

## 1. Architecture and ownership

| System | Existing owner / path | Responsibility |
|---|---|---|
| First-person player | `Player -> PlayerController`, `Player -> PlayerInteraction`; `Player/Main Camera` | CharacterController motion, mouse look and ray/sphere-assisted contextual `IInteractable` targeting. |
| Comfort | `Player -> PlayerComfortSettings`, `GameUI -> ComfortSettingsUI` | Persist FOV and mouse sensitivity in PlayerPrefs, separate from shop saves. |
| Store design | `StoreSystems -> StoreLayoutModeController`, `Player -> FurniturePlacementController`, `LayoutCameraRig -> LayoutCameraController` | Pause shopping and clock, preview body/customer/staff clearances, edit furniture, rebuild NavMesh after changes and resume customers. |
| Furniture | `FurnitureDefinition`, `FurnitureIdentity`, `StoreFurnitureLayout`, `FurnitureNavigationBlocker`, move/sell constraints | Definition and persistent instance identity; purchase/copy/sell/move validation; body footprint and routing clearance. |
| Customers | `CustomerFlow`, `CustomerController`, `CustomerShoppingBasket`, `CustomerNavigationRecovery`, `CheckoutQueue` | Shopping list, collection of **real** `PickupItem`s, optional customer basket, queuing, checkout and leave. |
| Checkout | `CheckoutRegister`, `CheckoutConveyor`, `CheckoutWorkArea`, `CashierWorkMode`, `CheckoutFeedback`, `ShoppingBag`, `MinimumCheckoutSellConstraint` | Transaction/scan/payment authority, transient real-item presentation on conveyor, staff-position data, dedicated camera work mode, scan feedback and last-checkout selling protection. |
| Ordering/delivery | `OrderingUI`, `OrderingProductRow`, `SupplierDeliveryQueue`, `DeliveryZone`, `DeliveryBox` | Purchase and schedule cartons; arrive now or after supplier cutoff; physically dispense/return units, track capacity and inventory. |
| Persistence | `StoreSystems -> SaveManager` | Version-2 JSON shop save/load, reconcile temporary customer inventory, save delivery-box quantity/capacity/pose, restore furniture, stock, stats and progression. |
| Nav diagnostics | `StoreSystems -> StoreRouteAudit` | After NavMesh rebuilding, check customer queue and cashier access from origins; warn without claiming absolute reachability guarantees. |

## 2. Core scene settings and interaction contracts

- Main scene `Assets/Scenes/Market.unity`. Its `CheckoutCounter` is a prefab instance at world `(0.3, 0, -3.4)`, Y rotation `180°`. Preserve all instance overrides: `CheckoutRegister.storeEconomy`, `.dailyStats` and `CheckoutFeedback.moneyFeedbackUI`.
- `Player -> PlayerController`: authored `moveSpeed=4.5`, `mouseSensitivity=0.09`; `Player/Main Camera` local position `(0,0.7,0)`, authored FOV `75`. Per-installation comfort preferences can override FOV/sensitivity at runtime.
- `Player -> PlayerInteraction`: `interactionDistance=3`, `interactionAssistRadius=0.14`, hold-repeat initial `0.30`, interval `0.18`. `InteractionTargetFinder.Find` uses raycast plus spherecast to resolve a component implementing `IInteractable` by looking at a collider. `E` primary interaction; `R` secondary; `F` carry; `X` management hold; `Q` drop.
- `Player -> CashierWorkMode`: enter camera transition `0.45s`, exit `0.35s`; player capsule stays in place while camera interpolates to the checkout's `CashierViewPoint`. Movement/normal world interaction disabled while working, cursor released, `E` scans/pays, `Esc` returns; Escape consumption coordinates with Pause Menu.
- `CheckoutRegister.GetInteractionPrompt` and `CheckoutRegister.Interact` currently check `CheckoutWorkArea` staff-side position. `CashierWorkMode.TryBegin` also requires `CheckoutWorkArea.CanServeFrom`, so exact stand-point geometry gates the player **three times**. This is a verified current UX regression, not yet fixed.
- `GameUI -> CanvasScaler`: Screen Space Overlay, Scale With Screen Size `(1920,1080)`, Match `0.5`. `GameUI/StoreRouteWarningPanel`: right-centre, anchored `(1,0.5)`, pivot `(1,0.5)`, anchored pos `(-24,0)`, size `(400,126)`, passive timed 12s; no clickable dismiss button.
- Store Design (`L`) suspends customers/clock and rebuilds the surface on dirty exit, then route-audits customer/staff paths. Preserve its tuned camera rotation directions (+45° left, −45° right), zoom and other player-tested settings.

## 3. Checkout prefab and transaction

**Prefab:** `Assets/_Game/Prefabs/Store/PF_CheckoutCounter.prefab`. **Definition:** `Assets/_Game/ScriptableObjects/Furniture/SO_Furniture_CheckoutCounter.asset`.

```
PF_CheckoutCounter [CheckoutQueue, CheckoutRegister, CheckoutConveyor,
    CheckoutWorkArea, CheckoutFeedback, FurnitureIdentity,
    FurnitureNavigationBlocker, NavMeshModifierVolume, BoxCollider,
    MinimumCheckoutSellConstraint]
  QueuePoints/QueuePoint_0_Front (-1.2 local Z)
  QueuePoints/QueuePoint_1       (-2.1 local Z)
  QueuePoints/QueuePoint_2       (-3.0 local Z)
  CheckoutPoints/ScanPoint      local (0,1.34,-0.1)
  CheckoutPoints/BagPoint       local (0.52,1.03,0)
  CheckoutPoints/CashierStandPoint local (0,0,1)
  CheckoutPoints/CashierViewPoint local (0,1.6,1.03), Euler (24,167,0)
  CheckoutPoints/UnloadingPoint local (-0.7,1.11,-0.08)
  CheckoutPoints/BeltExitPoint  local (0.15,1.11,-0.08)
  Visual/CounterBody            local (0,0.5,0), scale (1.8,1,0.8)
  Visual/Scanner                local (0,1.3,-0.1), scale (0.1,0.05,0.2)  [USER-TUNED]
  Visual/ScannerPost
  Visual/ScannerLight
  Visual/BaggingArea
  Visual/ConveyorAssembly/BeltFrame       local (-0.48,1.037,-0.04), scale (0.82,0.074,0.70)
  Visual/ConveyorAssembly/BeltSurface     local (-0.48,1.083,-0.04), scale (0.73,0.016,0.60)
  Visual/ConveyorAssembly/CustomerSideRail local Z -0.374
  Visual/ConveyorAssembly/StaffSideRail    local Z +0.294
  Visual/ConveyorAssembly/UnloadingGuide   local Z -0.04
```

- `CheckoutConveyor` in Lesson 151 is committed with **2D rectangle packing** using the actual `PickupItem` renderer-local bounds transformed into checkout coordinates. It places real groceries as nonphysical cargo, prioritizes right/scanner edge and available depth. There is no fixed three-item or one-item cap. **It stops at the first unplaceable next item** and does not try later smaller items. A too-large item can be displayed by itself; this is a deliberate visual overflow fallback. Existing `endPadding=0.04`, `sidePadding=0.025`, `productGap=0.035`, `surfaceLift=0.005`. The staged groceries may still show only one at a time; actual runtime dimensions have not been measured by this document.
- `CheckoutRegister` is the only transaction authority: `EnsureCheckoutSession` starts staging, `BeginNextScan` selects one presented real `PickupItem`, scan coroutine moves it to `ScanPoint`, plays scan feedback, waits `0.12s`, counts it and hides scanned item. Payment invokes `CustomerController.CompleteCheckout`, then revenue, daily stats and feedback exactly once. `CheckoutConveyor.EndSession(false)` restores unfinished item parents and state on reset; successful checkout clears temporary slots after completion.
- `CheckoutWorkArea` has `CashierStandPoint`, `CashierViewPoint`, `workingRadius=0.75`, `minimumStaffSideZ=0.45`. These are operational/staff navigation data, and the first two markers must be preserved even if player entry becomes more permissive.
- Furniture definition: purchase price `£120`, max owned `1`, purchasable and sellable subject to constraints; body centre `(0,0.5,0)`, half `(0.9,0.5,0.4)`; customer access centre `(0,0.75,-2)`, half `(0.8,0.75,1.55)`; staff access centre `(0,0.75,0.95)`, half `(0.85,0.75,0.55)`. BoxCollider size `(1.8,1,0.8)`; navigation horizontal padding `0.05`, floor overlap `0.20`. Do not enlarge or change placement/clearance based solely on visual changes.

## 4. Customer basket and shopping list

- `Assets/_Game/Prefabs/Characters/PF_CustomerPrototype.prefab -> CustomerShoppingBasket`: `minimumItemsForBasket=3`, `columnSpacing=0.30`, `rowSpacing=0.21`, `layerSpacing=0.16`, `shrinkOversizedItems=false`. The three max-size Inspector values are currently `10` but irrelevant because shrinking is off. **Do not silently enable shrinking.**
- `Assets/_Game/Prefabs/Store/PF_CustomerBasket.prefab`: basket visual scaled `(0.85,1,0.9)` and has base, four short walls and `ItemSlotsRoot` at `(0,0.04,0)`. It is an open basket, not the checkout shopping bag. Whole real product models in the stacking slots currently overlap/clipped basket walls/base according to user playtesting.
- `CustomerController.carriedItems` is an authoritative list of actual PickupItem references. The player-facing basket arrangement and the number of groceries sold **must not** determine inventory or transaction count. `CustomerShoppingBasket.GetCarryPoint` creates slots dynamically in 2 columns × 2 rows with successive layers; it does not itself enforce a maximum item count. `CustomerController.GetNextCarrySlot` uses dynamic basket slots once acquired, otherwise falls back to three handheld carry slots in the committed customer prototype.
- `StoreSystems -> CustomerFlow`: `maxCustomersInStore=5`, `minItemsPerCustomer=1`, `maxItemsPerCustomer=4` from `Market.unity`. The four-item shopping trip ceiling is a **CustomerFlow tuning setting**, not a basket capacity check. Broader shopping lists should be balanced with shelf stock and missing-product penalties rather than an arbitrary basket-sized limit.
- The appropriate future basket design will keep the **open basket**, separate its visible representative grocery arrangement from the real collected items, and reveal actual full-size groceries only at checkout. No such presentation system has been committed yet.

## 5. Ordering and deliveries — verified source-of-truth issue

- Scene `GameUI -> OrderingUI` has `quantityPerBox=12` (user-tuned), and the generic `Assets/_Game/Prefabs/Store/PF_DeliveryBox.prefab -> DeliveryBox` has `boxCapacity=12` (user-tuned). **Preserve both until a proper migration exists.**
- `OrderingUI` currently uses its own `quantityPerBox` to calculate displayed row price, purchasing cost, scheduled `SupplierDeliveryQueue` quantity, and immediate `DeliveryZone.TryCreateDelivery` quantity. This UI field is an incorrect owner for supplier packaging.
- `DeliveryBox.Configure(ProductData,int)` currently sets capacity to `max(prefabCapacity, incomingQuantity)`, so prefab capacity and order size can disagree silently. `SupplierDeliveryQueue` and `SaveManager.RestoreDeliveryZone` also hold a single generic `deliveryBoxPrefab` reference; future per-product carton prefabs require updating **all** immediate, queued and restored delivery paths.
- `SaveManager` v2 JSON captures existing `DeliveryBoxSaveData` values including `productID`, `quantity`, `capacity`, `isOpen`, slot index and world transform. Keep backward-compatible restoration of existing saved cartons when migrating to per-product packaging. Pending orders store productID, quantity and arrivalDay.
- `ProductData` currently has world prefab, display/storage conditions, shelf layout, economy and progression, **no delivery packaging fields**. Product-specific carton prefab and units per carton should belong to product definitions; ordering UI should read them, not decide them. That is a future lesson, not implemented.
- `PF_DeliveryBox_Strawberry.prefab` exists as a separate legacy-looking carton prefab. Do not switch packaging to it without verifying intended usage and backward compatibility.

## 6. Known issues and next work (not implemented here)

1. The player must stand within `CheckoutWorkArea.CanServeFrom()` to enter cashier mode even after directly aiming at the checkout. Lesson 152 proposes removing this player-only gate; preserve work-area data for staff and audit.
2. Only one product was visible in a user's multi-item checkout playtest; investigate **actual carried count and measured item footprints** with opt-in diagnostics before altering dimensions or algorithm.
3. Full-size groceries displayed inside the open customer basket overlap and clip. Recommend separate attractive basket presentation from real item ownership rather than shrinking all item prefabs or limiting shopping to four.
4. `OrderingUI.quantityPerBox` versus `DeliveryBox.boxCapacity` duplicate packaging authority. Move exact units/carton and carton prefab to ProductData with safe save/queued-order migration.
5. Later: deliberate unloading animation, tactile individual-item scanning, POS/cash/card and receipt, employee cashiers/restockers, and safe checkout task interruption/recovery.

**Implementation status:** All above existing code and Inspector values are verified from GitHub main at the SHA stated above. Lesson 152 edits and any subsequent tests remain **proposed and unverified**. Never report compilation or playtesting success without running Unity. Before the next lesson, fetch main and inspect full relevant source again.
