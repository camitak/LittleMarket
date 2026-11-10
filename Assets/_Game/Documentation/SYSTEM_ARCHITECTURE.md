# Little Market — System Architecture (Lesson 140 proposed implementation)

**Verified source-of-truth:** GitHub `camitak/LittleMarket` `main` at `d56bb8033c7e0e8d3a16d0a75767519312bed23e`, commit "Lesson 139 complete - checkout safety, basket station alignment, comfort and catalog limits" (checked November 9, 2026).

**Implementation status:** This document describes the revised Lesson 140 to implement and playtest, **not** a change already pushed. It **supersedes** the discarded Lesson 140 proposal that required click-to-select + M for moving. Read the latest GitHub `main` and this document before Lesson 141.

## Permanent development rules

- Before each lesson, inspect the newest `main`, read this architecture file, examine complete affected scripts, prefabs, ScriptableObjects, and `Assets/Scenes/Market.unity`.
- Provide verified exact GameObject paths, component names, and complete UI Inspector anchors/pivots/positions/dimensions/colours/raycast settings/references. No invented paths or vague 'existing controller' references.
- Preserve all player-tuned camera, grid, placement and scene parameters, unless specifically identified as lesson changes.
- Give normal C# formatting, complete implementation + Unity setup + tests in chat, plus architecture and continuation `.md` downloads.
- Prioritise playtesting and regressions, especially motion comfort and UX, over roadmap.

## Core baseline verified from Lesson 139

- `Player -> FurniturePlacementController` manages direct LMB movement, placement checks, snap sockets, purchase previews, body/front-access footprints, and a legacy double-X sale timer.
- `StoreSystems -> FurnitureShop` owns purchase authorization, ownership caps and sale refunds (50%). `FurnitureIdentity` delegates move/sale rules to components implementing `IFurnitureMoveConstraint` / `IFurnitureSellConstraint`.
- `StoreSystems -> StoreFurnitureLayout` manages authored vs runtime furniture persistence; authorized sale calls `TryRemoveFurniture` and updates navigation via `StoreLayoutModeController.MarkNavigationDirty()`.
- `PF_CheckoutCounter.prefab` has `MinimumCheckoutSellConstraint`: no sale if it is the last active managed checkout. `SO_Furniture_CheckoutCounter` still Purchasable=true, Sellable=true, MaxOwned=1, £120.
- `PF_RestockBasketStation.prefab` owns station; the scene has one unique basket docked under its `DockPoint`, not an extra basket inside each purchasable station prefab. `SO_Furniture_RestockBasketStation` access centre `(0,0.75,+0.9)`, MaxOwned=1, price £40.
- `FurnitureShop.ShouldListCatalogItem` preserves capped products in furniture listings with owned cap and SOLD OUT state; `FurnitureShop.CanBeginPurchase` enforces actual purchase limits.
- Scene authored `Player -> PlayerController` speed 4.5 and sensitivity .09, `Player/Main Camera -> Camera` FOV 75. `LayoutCameraRig -> LayoutCameraController` working distance 9, overview pitch 62°, yaw 0°, MMB free orbit enabled, yaw sensitivity .12, pitch .10, scroll sensitivity **2**, smoothing .06/.10, max distance 32. Motion blur remains disabled.
- `GameUI` Canvas is Screen Space Overlay, reference resolution 1920×1080, match .5. `GameUI/LayoutModeFrame` full-stretch anchors (0,0)-(1,1), pivot (.5,.5), transparent and raycast off. Controls panel `GameUI/LayoutModeFrame/LayoutControlsPanel` anchored bottom-right, position (-28,+28), 360×214; `ControlsText` stretches with SizeDelta (-32,-32), font17 pre-lesson. `BuildCatalogPanel` is left-centre, `ExpansionDetailsPanel` right-centre.

## Revised Lesson 140 — agreed UX

**Not** a click-to-select / Move-button panel. The user rejected the extra step. Do NOT apply old `StoreDesignSelectionUI` instructions or create its UI hierarchy.

- LMB over stationary furniture immediately starts move (retain).
- RMB over stationary furniture opens a **small contextual Copy/Sell panel next to pointer**, clamped to viewport.
- RMB while previewing/moving furniture cancels as before; `Q`/Esc also cancel. Avoid RMB opening a new menu in the cancellation frame (`CancelledPlacementThisFrame` guard).
- A transparent full-frame `ContextMenuDismissLayer` UI Image with Raycast Target on blocks world clicks while menu is open. On outside **left-button release**, dismiss the menu; mouse-down occurred while overlay blocked world interactions. Esc dismisses using centralized layout-mode handling. Right-click again dismisses or goes back from confirmation.
- Copy and Sell buttons remain visible but disabled with reasons if unavailable (cap, insufficient funds, last checkout, non-sellable ordering terminal, live checkout, etc.). No direct X sell; no timed double-X. `C` stays an optional quick-copy shortcut, now **one-shot**.
- Sell opens explicit name/refund confirmation; Confirm executes `FurnitureShop.TrySellFurniture`, marks nav dirty and shows money feedback; Cancel/Esc returns without selling.
- Context Copy yields one Unity UI frame after closing overlay then calls `FurnitureShop.TryBeginPurchase(definition, out reason, sourceYaw, repeatPlacement:false)` to preserve pointer UI correctness. Only one new preview; normal Build Catalog purchase remains repeatable.
- `FurniturePlacementController.TryFindPointedAtFurniture` made public; raycast uses `QueryTriggerInteraction.Collide` so `PF_RestockBasketStation` trigger collider is selectable. Keep validation otherwise unchanged.
- `StoreLayoutModeController` references `GameUI -> StoreDesignFurnitureContextMenuUI` to dismiss on Esc and exit. Retire the obsolete double-X confirmation state and calls.
- `LayoutCameraRig -> LayoutCameraController` already has MMB smooth yaw/pitch orbit. Add public `RotateViewLeft/RotateViewRight` yaw steps of 45° without rotating world contents. Bind keyboard Left/Right Arrow and visible top-right buttons. Do not change serialized camera tuning.

## New scene UI and references

Under `GameUI/LayoutModeFrame` create:

```
ContextMenuDismissLayer (UI > Image, full stretch, rgba white 0, Raycast Target ON, inactive initially)
└── FurnitureContextMenuPanel (UI > Image, anchor centre, pivot (0,1), 360×310, positioned at pointer by script)
    ├── FurnitureNameText (UI > Text - TextMeshPro)
    ├── OwnershipText (UI > Text - TextMeshPro)
    ├── CopyButton (UI > Button - TextMeshPro)
    ├── SellButton (UI > Button - TextMeshPro)
    ├── StatusText (UI > Text - TextMeshPro)
    ├── CloseButton (UI > Button - TextMeshPro)
    └── SaleConfirmationPanel (UI > Image, full stretch, opaque, inactive initially)
        ├── ConfirmationText (UI > Text - TextMeshPro)
        ├── CancelSaleButton (UI > Button - TextMeshPro)
        └── ConfirmSaleButton (UI > Button - TextMeshPro)
CameraRotatePanel (UI > Image, upper-right, 280×58)
├── RotateViewLeftButton (UI > Button - TextMeshPro)
└── RotateViewRightButton (UI > Button - TextMeshPro)
```

Add `StoreDesignFurnitureContextMenuUI` component on `GameUI`, assign `StoreSystems -> StoreLayoutModeController`, `Player -> FurniturePlacementController`, `StoreSystems -> FurnitureShop`, `GameUI -> BuildCatalogUI`, `StoreSystems -> StoreExpansionLayoutController`, and all exact UI references. Assign same component on `StoreSystems -> StoreLayoutModeController`. Bind CameraRotatePanel buttons to `LayoutCameraRig -> LayoutCameraController.RotateViewLeft/Right`, Dynamic single-runtime listeners or OnClick inspector as lesson instructs.

Use full exact component/RectTransform instructions from the Lesson 140 chat, not the older discarded Lesson 140 design.

## Required regression checks

1. LMB immediately moves shelf (NO intermediate selection panel); RMB opens options only while idle; RMB during move cancels and does NOT reopen options that same frame.
2. Click outside dismisses without moving shelf/expansion or placing a purchase; hover over UI blocks world input.
3. Menu Copy one-shot (no automatic repeat); normal Build Catalog can repeat purchases; purchase charged only when placed; cancel costs nothing.
4. Last checkout and Ordering Terminal protected, disabled Sell shows reason; cap and insufficient money disabled Copy show reason; sale confirmation cancel and Esc safe; X never sells.
5. Middle-mouse orbit works; Left/Right Arrow and camera buttons turn view 45° around current pivot, not rotate store objects. Rotation works during layout but not in first-person mode; zoom scroll speed stays 2.
6. Layout exit clears menu, restores gameplay camera, preserves save/load, basket station, customer pathing, expansion selection, and money behaviour.

## Roadmap after Lesson 140

- Playtest action-popup discoverability and comfort with fresh players.
- Prototype optional third-person local player camera/visible character, separate input, interaction and camera concerns before any networking.
- Persistent comfort options and performance testing; future multiplayer store-visit/helper role needs explicit host/server-authoritative commerce and permissions.
