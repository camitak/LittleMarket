# Little Market — System Architecture

**Repository:** `camitak/LittleMarket`  
**Verified GitHub source of truth:** `main` at `b00aad14faa1f22d90ef26d6dae1b8e624497fa1` — *Lesson 149 complete - checkout conveyor visual prototype*  
**Inspection date:** 12 November 2026  
**Unity version:** `6000.6.0f1` (`ProjectSettings/ProjectVersion.txt`)

> **Status and provenance:** This document describes systems verified in committed GitHub source through Lesson 149. It replaces an outdated Lesson 142 candidate document. Local uncommitted Unity changes are **not verified**. Lesson 149's conveyor visuals are present in the committed prefab. Lesson 150's dynamic unloading implementation is a **proposal awaiting local implementation and playtest**, not a verified system. Do not assume a proposed system exists without checking the next GitHub `main` baseline.

## 1. Core project responsibilities

| Area | Verified scripts/components | Responsibility |
|---|---|---|
| First-person player | `Player -> PlayerController`, `Player -> PlayerInteraction`, `Player -> PlayerComfortSettings`; `Player/Main Camera` | `CharacterController` movement and mouse-look; focused `IInteractable` interactions and carrying; persistent per-installation field-of-view and sensitivity choices. |
| Store systems | `StoreSystems -> StoreClock`, `CustomerFlow`, `StoreEconomy`, `DailyStats`, `StoreReputation`, `StoreProgression`, `StoreDayLifecycle`, `SaveManager` | Shop time/day, customers, money/statistics, progression, day transitions and save/load. These are distinct owners; checkout transactions should not duplicate their work. |
| Store Design | `StoreSystems -> StoreLayoutModeController`, `Player -> FurniturePlacementController`, `Player -> FurniturePlacementVisualizer`, `StoreSystems -> LayoutFurnitureAccessVisualizer`, `LayoutCameraRig -> LayoutCameraController` | Suspend normal simulation, arrange fixtures, preview placement with footprints, rebuild navigation on changed layouts, then resume normal gameplay. |
| Furniture ownership | `FurnitureDefinition`, `FurnitureIdentity`, `StoreFurnitureLayout`, `FurnitureShop`, `FurnitureNavigationBlocker`, `IFurnitureMoveConstraint`, `IFurnitureSellConstraint` | Definitions, instances/persistent IDs, purchases/copy/move/sell constraints, placement records, navigation blocking. |
| Customer shopping | `CustomerFlow`, `CustomerController`, `CustomerShoppingBasket`, `CustomerNavigationRecovery`, `ShelfRegistry`, `ShelfSlot` | Shopping lists, navigation, obtaining real products, optional carried basket, queuing and visit completion/recovery. |
| Checkout | `CheckoutQueue`, `CheckoutRegister`, `CheckoutFeedback`, `CheckoutWorkArea`, `CashierWorkMode`, `MinimumCheckoutSellConstraint` | Queue management, transaction authority, audio/visual scan feedback, cashier-side validation, player workstation camera/input, minimum-checkout protection. |
| Navigation diagnosis | `StoreSystems -> StoreRouteAudit`, existing NavMeshSurface | After the layout's NavMesh rebuild, check configured checkout queue and cashier destinations and report disconnected routes. This is a warning, **not** a hard global connectivity placement guarantee. |
| UI | `GameUI` Screen Space Overlay Canvas, `PauseMenuUI`, `ComfortSettingsUI`, `InteractionUI`, `ManagerBookUI`, `OrderingUI`, `EndOfDayUI` | Explicit modes control their cursor/input ownership; gameplay prompts are not cursor-operated buttons. |

## 2. Scene and input conventions

- Main scene: `Assets/Scenes/Market.unity`. The verified authored checkout is a prefab instance named `CheckoutCounter`, at **world `(0.3, 0, -3.4)`**, Y rotation **180°**. Its prefab source is `Assets/_Game/Prefabs/Store/PF_CheckoutCounter.prefab`. Preserve instance overrides for `CheckoutRegister.storeEconomy`, `.dailyStats`, and `CheckoutFeedback.moneyFeedbackUI`.
- `Player -> PlayerController` authored `moveSpeed = 4.5`, `mouseSensitivity = 0.09`. `Player/Main Camera -> Camera` authored FOV **75**. `Player/Main Camera -> Transform` local `(0,0.7,0)`. `PlayerComfortSettings` may override FOV/sensitivity in runtime via `PlayerPrefs`; store slot save/load should not override those preferences.
- `Player -> PlayerInteraction`: interaction distance **3**, assist radius **0.14**, hold repeat initial **0.30**, interval **0.18**. `E` primary, `R` secondary, `F` carry, `X` hold-management, `Q` drop. Checkout-specific validation is independent of general interaction range.
- `StoreSystems -> StoreLayoutModeController`: `L` toggles Store Design; Store Design owns Escape and tracks `ConsumedEscapeThisFrame` to avoid accidental Pause activation. Exiting with dirty navigation calls `Physics.SyncTransforms()` and `NavMeshSurface.BuildNavMesh()` before running `StoreRouteAudit.CheckRoutes()` and resuming customers.
- `GameUI -> PauseMenuUI` owns the Pause Overlay; while paused it disables player movement/interaction, unlocks cursor and uses `Time.timeScale = 0`. Escape routing respects Store Design and cashier mode. `ManagerBookUI`, `OrderingUI`, `EndOfDayUI` also manage modal cursor and movement state.
- `LayoutCameraRig -> LayoutCameraController` is separate from first-person camera. Preserve committed view rotation Left **+45°**, Right **-45°** and tuned zoom/movement behaviour; do not change based on a checkout lesson.
- `GameUI -> CanvasScaler`: Scale With Screen Size, reference resolution **1920 × 1080**, Match **0.5**. `GameUI/InteractionPromptCard` is bottom-centred at anchored position `(0,50)` and size `(900,76)`.
- `GameUI/StoreRouteWarningPanel` (Lesson 148) is a passive right-centre notification, anchored `(1,0.5)` with pivot `(1,0.5)`, anchored position `(-24,0)`, size `(400,126)`. `WarningText` is a child and not raycast-targeted. Warning duration is **12 seconds** in `StoreSystems -> StoreRouteAudit`. No mouse-operated dismiss button is present.

## 3. Checkout data and current runtime transaction flow

**Prefab:** `Assets/_Game/Prefabs/Store/PF_CheckoutCounter.prefab`.

Existing child hierarchy relevant to service:

```text
PF_CheckoutCounter [CheckoutQueue, CheckoutRegister, CheckoutFeedback,
  FurnitureIdentity, FurnitureNavigationBlocker, NavMeshModifierVolume,
  MinimumCheckoutSellConstraint, CheckoutWorkArea, BoxCollider]
  QueuePoints
    QueuePoint_0_Front          local (0, 0, -1.2)
    QueuePoint_1                local (0, 0, -2.1)
    QueuePoint_2                local (0, 0, -3)
  CheckoutPoints
    ScanPoint                   local (0, 1.34, -0.1)
    BagPoint                    local (0.52, 1.03, 0)
    CashierStandPoint           local (0, 0, 1)
    CashierViewPoint            local (0, 1.6, 1.03), Euler (24, 167, 0)
    UnloadingPoint              local (-0.7, 1.11, -0.08)
    BeltExitPoint               local (0.15, 1.11, -0.08) [committed value; beyond belt top]
  Visual
    CounterBody                 local (0, 0.5, 0); scale (1.8, 1, 0.8)
    Scanner                     local (0, 1.3, -0.1)
    ScannerLight                local (0, 1.329, -0.125)
    BaggingArea                 local (0.52, 1.015, 0)
    ScannerPost                 local (0, 1.14, -0.1)
    ConveyorAssembly
      BeltFrame                 local (-0.48, 1.037, -0.08), scale (0.82, 0.074, 0.60)
      BeltSurface               local (-0.48, 1.083, -0.08), scale (0.73, 0.016, 0.48)
      CustomerSideRail          local (-0.48, 1.115, -0.363)
      StaffSideRail             local (-0.48, 1.115, 0.203)
      UnloadingGuide            local (-0.7, 1.094, -0.08)
  Audio Source
```

- `CheckoutQueue` assigns waiting customers to the three queue transforms; the front customer is the one served. `CustomerController` retains the actual collected `PickupItem` references in its carried-items list, possibly presented in `CustomerShoppingBasket` (basket threshold **3** items).
- `CheckoutWorkArea` checks the player's position relative to the checkout: must be on positive local Z (`minimumStaffSideZ = 0.45`) and within `workingRadius = 0.75` of `CashierStandPoint`. It exposes `CashierViewPoint`.
- `Player -> CashierWorkMode` temporarily disables `PlayerController` and `PlayerInteraction`, unlocks the cursor, smoothly moves the *camera* to the prefab's `CashierViewPoint` (**enter 0.45 s**), and restores the original local camera transform (**exit 0.35 s**) and normal controls on exit. Does **not** relocate the player capsule. `E` continues triggering the existing checkout scan/payment steps while working; Escape exits. The component coordinates Escape with `GameUI -> PauseMenuUI`.
- `CheckoutRegister` remains authoritative for session state, scanning and payment. It reserves one real `PickupItem`, moves it to `ScanPoint`, plays `CheckoutFeedback`, waits authored `scanDisplaySeconds = 0.12`, hides the scanned item, increments transaction total/count and creates/registers a shopping bag at `BagPoint`. When the full order is scanned, another `E` calls `CustomerController.CompleteCheckout()`, adds revenue through `StoreEconomy`, records a sale in `DailyStats` and emits payment feedback.
- **Important limitation at Lesson 149:** Existing scanning is an `E` action, **not** picking up or clicking physically unloaded groceries. The conveyor geometry exists but has **no product staging or movement logic yet**. Physical item unloading/motion, bag-handling interaction, banknotes/coins, card reader, POS and receipts remain future work.
- `CheckoutRegister.CanSellFurniture()` prevents sale while there is an active session or front customer. `MinimumCheckoutSellConstraint` prevents selling the last remaining checkout. Safe mid-service moving/selling with transaction rollback/requeue is **not implemented**.

## 4. Furniture placement, navigation and checkout footprint

**Definition:** `Assets/_Game/ScriptableObjects/Furniture/SO_Furniture_CheckoutCounter.asset`.

| Field | Verified authored value |
|---|---|
| Furniture ID / price / limit | `checkout_counter` / **£120** / Max Owned **1** |
| Purchasable / Sellable | true / true |
| Placement Area | Sales Floor |
| Body Centre / Half Extents | `(0,0.5,0)` / `(0.9,0.5,0.4)` |
| Customer Front Access Centre / Half Extents | `(0,0.75,-2)` / `(0.8,0.75,1.55)` |
| Requires Staff Access | true |
| Staff Centre / Half Extents | `(0,0.75,0.95)` / `(0.85,0.75,0.55)` |
| Prefab root BoxCollider | Centre `(0,0.5,0)`, Size `(1.8,1,0.8)` |
| `FurnitureNavigationBlocker` | Horizontal Padding `0.05`; Floor Overlap `0.20`; maintains `NavMeshModifierVolume` from definition |

- `FurniturePlacementController` checks available floor, body colliders, fixed protected clearance, front operational access, optional staff access and overlaps with other furniture's reserved areas. `LayoutFurnitureAccessVisualizer` shows existing yellow customer and blue staff reservation areas; placement previews also show them.
- The staff rectangle is a **local reserved area**, not proof of a globally navigable route. `StoreRouteAudit` checks checkout queue positions from the customer entrance and cashier position from the staff-door sales marker after a changed NavMesh build; currently warns rather than rejecting/undoing a layout.
- Other storage-rack definitions already have front operational access. Future restocker navigation can use the same principle without inventing a second duplicate clearance rectangle now.
- Lesson 149 added visual-only conveyor cubes (without colliders) and two markers. The physical body/collider and navigation footprints were not enlarged; existing placement geometry remains preserved. Three shared URP/Lit materials live under `Assets/_Game/Art/Materials/Furniture/Checkout/` (frame, surface and guide).
- The existing checkout's body, queue, clearance and placement settings are tuned and must not be enlarged, shifted, rotated or reset without an explicit lesson, local playtest and save/restore checks.

## 5. Player and customer recovery / saving

- `CustomerFlow` maintains active customers, temporarily suspends them for Store Design, refreshes operational fixture references and resumes navigation afterward. `CustomerController` attempts to recover to usable NavMesh locations after layout changes through `CustomerNavigationRecovery`.
- Customers can wait in the checkout queue and accrue satisfaction penalties over waiting time. The game does **not** yet universally prevent every unreachable task or provide employee task routing. Future NPC recovery should detect and explain blocked routes, not leave NPCs permanently frozen.
- `StoreFurnitureLayout` handles furniture creation, sell checks, persistent instance IDs and layout restore. The existing checkout prefab's authored scene-instance references and persistent identity must not be replaced by adding unrelated visual geometry.
- Save system: `StoreSystems -> SaveManager` uses JSON under `Application.persistentDataPath` for shop state, separate from `PlayerComfortSettings` preferences in `PlayerPrefs`. New prefab visuals that are part of a reusable prefab do not need independent save fields unless they acquire persistent state.

## 6. Current UX decisions and explicit non-implemented features

- Keep the checkout's cashier position and staff clearance understandable to players; the dedicated camera shot is a working prototype subject to playtest, not final art direction.
- Route notices are passive, timed and placed on the right rather than using a mouse-only close button during first-person play.
- Established future design decisions **not implemented at this GitHub baseline**: dynamic staging of the *actual* product instances on the already visible conveyor; controlled conveyor motion; individual-item click scanning; bagging feedback; POS; cash with banknotes **and** coins and exact change; card payment; receipt; customer transaction recovery/requeue when checkout is moved or sold; hiring cashier and restocking employees, their workspaces and routes; any mood-based abandonment.
- Playtest bugs and regressions take priority over implementing more features. Do not describe local Unity compilation/playtests as verified by this document.

## 7. Continuity instructions

Before every new lesson: fetch GitHub `main` commit and `Assets/_Game/Documentation/SYSTEM_ARCHITECTURE.md`, inspect the complete relevant C# scripts, prefab, ScriptableObject and `Market.unity` hierarchy/references. Treat committed GitHub as the baseline, note that local edits may differ, preserve tuned values, and state verified vs proposed details. Keep all code and Inspector steps in the conversation; use a separate continuation document only for cross-chat continuity.

## 8. Lesson 150 proposal (not implemented in the verified source)

- Add `Assets/_Game/Scripts/Store/CheckoutConveyor.cs` to `PF_CheckoutCounter` as a transient presentation/ownership helper. It must **not** take over sales calculation, product identity, save-schema or world item ownership.
- Use the verified `Visual/ConveyorAssembly/BeltSurface` geometry, not fixed item counts, as the physical capacity. Belt items may vary in width; items beyond available space remain with the front customer until scanned items clear space. `CustomerFlow.maxItemsPerCustomer = 4` at this baseline is a separate tuning limit, not a conveyor hard cap.
- Preserve **real** `PickupItem` instances and `CustomerController.carriedItems`; never clone products to fake the belt or double count items.
- Add integration in `CheckoutRegister`: stage only when the front customer is ready and checkout session begins, verify scanned item is presented, refill on completed scan, return unpurchased items to their prior carry parent on interrupted/reset sessions, and clear staging records only on successful payment. Validate recovery using SaveManager's existing transient-inventory reconciliation.
- **Status:** instructions pending Unity local application, compilation, playtest and commit. Do not call Lesson 150 implemented until verified in a subsequent `main` checkout.

**Document status:** Updated to reflect the **verified committed** Lesson 149 architecture. The proposed Lesson 150 conveyor-product staging is deliberately not included in the implemented system description.
