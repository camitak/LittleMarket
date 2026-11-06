# Little Market — System Architecture

Current architecture after **Lesson 135 — Copy / Eyedropper Furniture Purchase**.

## Verified source of truth

Lesson 135 was designed after verifying GitHub `main` at:

`e0f042d7cb1b2b1816a0924ee5460cffdb5176ef`

Commit:

`Lesson 134 complete - expansion hover preview and discoverability`

Re-verify newest `main` before every future lesson.

## Permanent teaching / implementation rules

- Inspect newest GitHub `main` before every lesson.
- Read this architecture file before proposing implementation.
- Inspect complete relevant scripts, prefabs, ScriptableObjects, and `Market.unity`.
- Never guess scene references.
- Use exact GameObject/component paths when verified, for example:
  - `LayoutCameraRig -> LayoutCameraController`
  - `Player -> FurniturePlacementController`
  - `StoreSystems -> FurnitureShop`
  - `GameUI -> BuildCatalogUI`
  - `StoreSystems -> StoreLayoutModeController`
- Every UI instruction must name the Unity object/component type, exact hierarchy, anchors, pivot, signed Anchored Position/offsets, size, font/alignment, Raycast Target, colors, and references where relevant.
- Top-anchored downward movement normally uses negative Y; bottom-anchored upward movement normally uses positive Y.
- Preserve user-tuned scene values.
- Write conventional compact C#. Do not wrap simple declarations, signatures, conditions, or calls into many short lines merely for line wrapping.
- Keep implementation, explanation, setup, tests, and suggested commit together in chat.
- Only architecture and fresh-chat continuation `.md` files are normally separate.
- Player playtesting may reorder roadmap.

## Lesson 135 — Copy / Eyedropper

Store Design now supports copying a pointed furniture type with:

`C`

This is an eyedropper-style purchase, not a runtime object clone.

Flow:

1. point at an existing managed furniture item
2. press `C`
3. read its `FurnitureDefinition`
4. delegate to `FurnitureShop.TryBeginPurchase`
5. create a fresh preview through `StoreFurnitureLayout`
6. preserve the source furniture yaw
7. place using all existing Structured/Free, grid, magnet, body, access, clearance and area rules
8. charge only on confirmed placement
9. cancel spends nothing

## Copy semantics

Copy transfers:
- FurnitureDefinition/type
- source world yaw as the initial preview yaw

Copy does NOT transfer:
- PersistentID
- shelf product assignment
- shelf stock
- storage bay assignment
- storage quantity
- runtime registration/state
- future per-instance operational state

The new instance is created from the definition prefab and receives a fresh runtime persistent ID.

This is intentionally “buy another one like this”, not “clone this live object”.

## FurnitureShop changes

`FurnitureShop` adds:

`IsPurchasableCatalogItem(FurnitureDefinition definition)`

This allows Store Design presentation to know whether `C Copy` is meaningful without duplicating catalog membership/configuration rules.

`TryBeginPurchase` becomes:

`TryBeginPurchase(FurnitureDefinition definition, out string reason, float? initialYaw = null)`

Existing callers remain valid.

Catalog purchases use the default prefab yaw.
Copy passes the source furniture yaw.

## FurniturePlacementController changes

`Player -> FurniturePlacementController` receives a new serialized reference:

`Furniture Shop -> StoreSystems -> FurnitureShop`

When idle in Store Design:
- LMB still moves pointed furniture when movable
- C starts a fresh purchase of the pointed furniture definition when it is a valid catalog item

The pointed-item prompt can show both actions:

`LMB Move Standard Shelf  •  C Copy £40.00`

Copy remains available independently of source moveability. A source could later be temporarily non-movable because of runtime state while still being valid as an eyedropper source.

If a copy purchase fails, `InteractionUI` shows the business-rule reason briefly, such as insufficient money.

## Initial rotation

`FurniturePlacementController.BeginPurchasedFurniturePreview` accepts optional `initialYaw`.

`StartPreview` uses:
- source yaw for copy
- prefab/current yaw for ordinary catalog purchase

This preserves arbitrary Free-mode angles and Structured-mode cardinal orientations without adding a separate rotation system.

## Existing purchase authority

Copy must never:
- instantiate its own permanent object
- spend money directly
- bypass FurnitureShop
- bypass StoreFurnitureLayout
- manually generate/save IDs

All purchase authority remains:

`FurnitureShop -> StoreFurnitureLayout -> FurniturePlacementController`

Payment still happens only in confirmed placement.

## UI / controls

No new UI GameObject is created.

Existing:
`GameUI / LayoutModeFrame / LayoutControlsPanel / ControlsText`

remains:
- panel Anchor Min/Max `(1,0)`
- panel Pivot `(1,0)`
- panel Anchored Position `(-28,28)`
- panel Size Delta `(360,214)`
- ControlsText stretch
- ControlsText Size Delta `(-32,-32)`
- TMP font size 17
- left/top alignment
- Raycast Target OFF

Controls text first line becomes:

`B   Build Catalog      C   Copy Item`

All RectTransform values remain unchanged.

## Current copyable catalog definitions

- Standard Shelf — £40 — SalesFloor
- Refrigerated Display — £80 — SalesFloor
- Ambient Storage Rack — £60 — Backroom
- Chilled Storage Rack — £100 — Backroom
- Potted Plant — £25 — SalesFloor

Only definitions in the current FurnitureCatalog are offered by the copy interaction.

## Persistence

No save schema change.

Copied furniture is already covered by generic furniture placement save data because it receives a new persistent ID and normal FurnitureDefinition ID.

## Roadmap

Next likely:
- repeat placement
- selling / refunds
- variants / colors
- more decor / surface placement
- Ambience
- autosave
- exact carry persistence
- advanced fixture/socket semantics

Player testing may reorder this roadmap.
