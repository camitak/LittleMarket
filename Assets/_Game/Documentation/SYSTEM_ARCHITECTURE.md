# Little Market — System Architecture (Lesson 139 update)

**Source-of-truth checkpoint:** GitHub `camitak/LittleMarket`, `main` at `9bc60caeaf2710e16e45b9ceaddcd34b393d6ddc` — `Lesson 138 complete - managed core fixtures and sellable starter furniture` (verified 2026-11-09).

**Important:** This document records the **Lesson 139 implementation to apply and test**; it does not claim these changes have already been pushed. Before the next lesson, recheck the newest GitHub `main` and inspect the files and assets again.

## Permanent instructions

- Inspect newest GitHub `main` before every lesson; read `Assets/_Game/Documentation/SYSTEM_ARCHITECTURE.md`; inspect complete affected scripts, prefabs, ScriptableObjects, and `Assets/Scenes/Market.unity`.
- Never assume scene paths or references; specify verified full paths and exact component names.
- Preserve all authored and user-tuned values except changes explicitly requested by that lesson.
- When creating UI, explicitly specify object/component type, hierarchy, anchors Min/Max, pivot, signed anchored position/offsets, dimensions, TMP size/alignment, raycast target, colors, and Inspector references.
- Conventional compact C#; full implementation and testing instructions in chat; only architecture and continuation files normally attached.
- Player playtesting overrides the planned feature roadmap when material UX issues arise.

## Lesson 138 baseline retained

- `StoreFurnitureLayout` archives authored managed objects instead of destroying them upon sale; runtime furniture is destroyed, authored furniture may be absent from a save.
- `FurnitureDefinition` supplies `Purchasable`, `Sellable`, `MaxOwned` (0 = unlimited), prefab, area, body/access extents.
- `CheckoutCounter`, `BasketStation`, `CustomerBasketStation`, `OrderingTerminal` are managed furniture roots. OrderingTerminal movable but not purchasable or sellable. The other three are currently capped at 1.
- `StoreFurnitureLayout.CountFurnitureByDefinition` counts ownership, while `FurnitureShop.CanBeginPurchase` validates caps, balance, and state.
- `FurnitureIdentity` checks all `IFurnitureSellConstraint` implementations, and `CheckoutRegister` already blocks sale while serving customers.
- `BasketStation` docks the unique global `RestockBasket` in `RegisterFurnitureRuntime`, releases it when retiring, and uses the `DockPoint` transform. Do not instantiate duplicate baskets per purchasable station prefab.
- `CustomerFlow.RefreshOperationalFixtures` rebinds active checkout and customer basket station before spawning / resuming.

## Lesson 139: safety and UI rules

1. **Never sell the last operational checkout.** New `Assets/_Game/Scripts/Store/MinimumCheckoutSellConstraint.cs` implements `IFurnitureSellConstraint` and is added to the `PF_CheckoutCounter` prefab root (inherited by scene `CheckoutCounter`). The constraint checks for another enabled, active `CheckoutRegister` with managed `FurnitureIdentity`; returns a reason if none exists. Do NOT simply turn `SO_Furniture_CheckoutCounter.Sellable` off; preserve it `true`, `Purchasable=true`, `MaxOwned=1` for a later multi-checkout upgrade path. This means the only checkout is effectively unsellable now but movable.
2. **Prevent customer shopping without checkout in historical saves.** In `CustomerFlow.SpawnCustomer()`, call `RefreshOperationalFixtures()` and return if `checkoutQueue == null` before creating shopping lists or spawning customers. Saves made during Lesson 138 without a checkout need repurchasing it to resume arrivals.
3. **Keep capped furniture listed.** Add `FurnitureShop.ShouldListCatalogItem(definition)` using configured, present, purchasable eligibility *without* excluding max-owned entries. Preserve `IsPurchasableCatalogItem(definition)` as the actual owned-cap check: `FurniturePlacementController` uses it for copy prompts. Change the two list filters only in `BuildCatalogUI.RebuildRows` and `StoreComputerManagementUI.RebuildFurnitureRows` to `ShouldListCatalogItem`. `FurnitureCatalogRow` gets a `FurnitureShop`, displays `Owned N / MaxOwned` when limited, and disables the button with text `SOLD OUT` at the cap. `FurnitureShop.CanBeginPurchase` continues to enforce the cap.
4. **Product distinction.** `ProductCatalogueUI`/`OrderingUI` already show unlocked/locked products, and `ProductData` has reputation and store level unlocks but no per-product ownership max. Do not invent product caps; continue showing products and their stock status. Product max-stock policy is a separate future design decision.

## Lesson 139: restocking equipment

- Verified authored scene `BasketStation` position `(-6.2, 0, -2.8)` at Y=0°; separate `PF_RestockBasket` scene prefab instance currently at `(-9.738, 0.172, -3.5179799)`. It is an independent gameplay object but docks on start.
- In **Market.unity Scene**, reparent the unique `PF_RestockBasket` instance under `BasketStation/DockPoint` and reset basket local position/rotation to zero and scale one. This makes one visually assembled station in the editor without creating duplicate baskets upon future purchases. Verify `BasketStation -> BasketStation` fields `basket` and `dockPoint` still reference the unique basket and child `DockPoint`.
- `PF_RestockBasketStation.prefab` local `DockPoint` position is `(0,0.16,0)`, backboard local Z `-0.41`, base z `0`. In `SO_Furniture_RestockBasketStation` correct **accessCenter from `(0,0.75,-0.9)` to `(0,0.75,+0.9)`**. Do not change existing body/access extents, station world pose, nav modifier settings, or player placement tunings.
- Do not embed a fresh `PF_RestockBasket` in the purchasable station prefab; runtime station prefab reuses the unique basket through `BasketStation` resolution and docking.

## Lesson 139: first-person comfort baseline

`Assets/Scenes/Market.unity` currently has `Player -> PlayerController: moveSpeed=6, mouseSensitivity=0.12`; `Player/Main Camera -> Camera: field of view=60`. In Lesson 139 try `moveSpeed=4.5`, `mouseSensitivity=0.09`, `field of view=75`. These are explicit prototype changes intended for a comfort playtest, not a universal prescription.

The game `Global Volume` references `Assets/Settings/SampleSceneProfile.asset`, where Motion Blur is inactive; keep it inactive. Current `PlayerController` has no camera head bob or roll. Test separate gameplay and layout camera symptoms with steady FPS and multiple people, and later implement persistent Comfort options (FOV, sensitivity, optional camera effects) in Pause Menu.

## Existing values to preserve

- `LayoutCameraRig -> LayoutCameraController -> Scroll Zoom Sensitivity = 2`, working distance 9, overview pitch 62, overview yaw 0 and all other existing user-tuned layout camera values.
- `Player -> FurniturePlacementController -> Outline Width = 0.01`, Grid 0.5, Socket Capture 0.30, Socket Release 0.55, Max Facing Angle 15.
- `PF_RestockBasket` capacity 4; basket physics and local item points; original station prefab `DockPoint` and front-access half-extents.
- `SO_Furniture_CheckoutCounter` price £120, Purchasable ON, Sellable ON, MaxOwned 1; `SO_Furniture_RestockBasketStation` price £40, MaxOwned 1.
- Maintain all other existing prefab and scene overrides unless explicitly changed.

## Playtests before proceeding

- Standalone checkout cannot be sold when empty or during a checkout; moving it still works; menu explains why. Historical no-checkout saves spawn no customers; placing checkout resumes arrivals.
- Station with basket appears as a visually single object in edit/play. Taking basket detaches, returning redocks, moving station preserves dock relationship; retiring and repurchasing it never produces duplicate baskets.
- Station access footprint is visually in front (+Z), rotates with station, and rejects blocked access; placement/navigation work.
- Build catalog and computer furniture tab show capped fixtures as `SOLD OUT` + `Owned 1/1`, disable buy; repurchase availability returns after sale. Copy shortcut does not promise copy while capped.
- Product catalog still shows locked/unlocked products and ordering stock counts.
- Comfort retest at 75 FOV, 4.5 movement and .09 mouse sensitivity without motion blur. Compare first person vs Layout Mode separately.

## Roadmap after Lesson 139 verification

1. Full Store Design UX overhaul: selected object, compact action panel Move/Copy/Sell, clearer disabled reasons, keyboard shortcuts as accelerators.
2. Dedicated comfort settings menu, saved preferences and focused player testing if discomfort persists.
3. Furniture variants/colors, surface placement and decoration; ambience; autosave; exact carry persistence; advanced fixtures; eventual multi-checkout model.
