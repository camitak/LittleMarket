# Little Market — System Architecture

Current architecture after **Lesson 137 — Safe Furniture Selling & Refunds**.

## Verified source of truth

Lesson 137 was designed after verifying GitHub `main` at:

`477355bf9928c31a66dd7a555afefe1e455284df`

Commit:

`Lesson 136 complete - repeat furniture placement`

Re-verify newest `main` before every future lesson.

## Permanent teaching / implementation rules

- Inspect newest GitHub `main` before every lesson.
- Read this architecture file before proposing implementation.
- Inspect complete relevant scripts, prefabs, ScriptableObjects, and `Market.unity`.
- Never guess scene references.
- Use exact GameObject/component paths when verified.
- Every UI instruction must name the Unity object/component type, exact hierarchy, anchors, pivot, signed Anchored Position/offsets, size, font/alignment, Raycast Target, colors, and references where relevant.
- Top-anchored downward movement normally uses negative Y; bottom-anchored upward movement normally uses positive Y.
- Preserve all user-tuned scene values.
- Write conventional compact C#.
- Keep the complete lesson implementation in chat. Only architecture and fresh-chat continuation `.md` files are normally separate.
- Player playtesting may reorder the roadmap.

## Lesson 137 — Furniture selling

Store Design supports safe sale of player-purchased furniture with:

`X`

Selling uses a two-press confirmation:
1. point at player-purchased furniture
2. press X to arm
3. press X again while still pointing at the same object to confirm
4. RMB, Esc, pointer movement, camera manipulation, or another world action cancels the armed sale

## Refund rule

`StoreSystems -> FurnitureShop` owns:

`Sell Refund Fraction = 0.5`

Refund = 50% of current FurnitureDefinition purchase price, rounded to cents.

Current examples:
- Standard Shelf £40 -> £20 refund
- Refrigerated Display £80 -> £40 refund
- Ambient Storage Rack £60 -> £30 refund
- Chilled Storage Rack £100 -> £50 refund
- Potted Plant £25 -> £12.50 refund

This is tunable economy balance, not hard-coded into FurnitureDefinition.

## Sale authority

Business flow:

`Player -> FurniturePlacementController`
-> `StoreSystems -> FurnitureShop`
-> `StoreSystems -> StoreFurnitureLayout`
-> `StoreSystems -> StoreEconomy`

FurniturePlacementController owns pointing/confirmation UX.
FurnitureShop owns refund calculation and economy.
StoreFurnitureLayout owns authored-vs-purchased protection and retirement.
FurnitureIdentity and child sell constraints protect runtime contents.

No object is directly destroyed from FurniturePlacementController.

## Authored starter furniture

The current save contract requires all authored furniture IDs to remain present.

Therefore these authored scene-root objects are protected from sale:
- `Shelf_Cereal`
- `Shelf_Bakery`
- `Shelf_Dairy`
- `StorageRack_Starter`

They remain movable/copyable according to existing rules, but cannot be sold in Lesson 137.

Allowing starter furniture sale later requires a deliberate persistence-contract redesign rather than bypassing `StoreFurnitureLayout.CanRestoreSaveData()`.

## Stock protection

New interface:

`IFurnitureSellConstraint`

`ShelfSlot` implements it:
- any physical shelf stock blocks sale
- an empty assigned facing does not block sale

`StorageBay` implements it:
- any quantity greater than zero blocks sale
- an assigned bay with quantity zero does not block sale

This means physical products are never silently deleted by selling furniture.

## StoreFurnitureLayout

New responsibilities:
- `IsPlayerPurchasedFurniture(FurnitureIdentity furniture)`
- `CanSellFurniture(FurnitureIdentity furniture, out string reason)`
- `TryRemovePurchasedFurniture(FurnitureIdentity furniture, out string reason)`

Sale retirement reuses the existing `RetireFurniture` path:
- runtime participants unregister
- shelf/storage registries remove their slots
- object deactivates
- object is destroyed
- future save no longer includes the purchased furniture or its slots

## FurnitureIdentity

Adds:

`CanSellFurniture(out string reason)`

It gathers child `IFurnitureSellConstraint` implementations, analogous to existing move constraints.

## Store Design presentation

Idle purchased-furniture prompt includes resale information when sellable.

Example:

`LMB Move Standard Shelf  •  C Copy £40.00`
`X Sell +£20.00`

If stocked, the second line explains that stock must be removed.

After first X:

`Sell Standard Shelf for £20.00?`
`X Confirm  •  RMB / Esc Cancel`

No new UI GameObject or component is created.

## Controls UI

Existing:
`GameUI / LayoutModeFrame / LayoutControlsPanel / ControlsText`

remains:
- panel Anchor Min/Max `(1,0)`
- panel Pivot `(1,0)`
- panel Anchored Position `(-28,28)`
- panel Size Delta `(360,214)`
- ControlsText Anchor Min `(0,0)` / Max `(1,1)`
- ControlsText Pivot `(0.5,0.5)`
- ControlsText Anchored Position `(0,0)`
- ControlsText Size Delta `(-32,-32)`
- TMP font size 17
- left/top alignment
- Raycast Target OFF

Control summary line becomes:

`B   Catalog    C   Copy    X   Sell`

No RectTransform changes.

## Navigation

Successful sale calls:
`StoreSystems -> StoreLayoutModeController.MarkNavigationDirty()`

Customers remain suspended during Store Design.
NavMesh rebuild remains deferred until Store Design exit.

## Persistence

No save schema change.

Sold player-purchased furniture disappears from normal furniture placement save data because it is retired/unregistered before saving.
Refund money is already part of existing `StoreEconomy` persistence.

## Roadmap

Next likely:
- furniture variants / colors
- more decor / surface placement
- Ambience
- autosave
- exact carry persistence
- advanced fixture/socket semantics

Player testing may reorder this roadmap.
