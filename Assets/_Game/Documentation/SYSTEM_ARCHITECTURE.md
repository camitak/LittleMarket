# Little Market — System Architecture

Current architecture after **Lesson 136 — Repeat Purchase Placement**.

## Verified source of truth

Lesson 136 was designed after verifying GitHub `main` at:

`4b582d4b06d72da110dfdb8447bc739f3ec6bc7d`

Commit:

`Lesson 135 complete - furniture copy eyedropper`

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
- Keep the whole lesson implementation in chat. Only this architecture file and the fresh-chat continuation file are normally separate.
- Player playtesting may reorder the roadmap.

## Lesson 136 — Repeat purchase placement

Furniture purchases now behave as a placement session.

Starting a purchase from either:
- `B` Build Catalog
- `C` Copy / Eyedropper

creates a normal purchase preview.

Each valid LMB confirmation:
1. charges exactly one item through `FurnitureShop.TryCompletePurchase`
2. activates that placed furniture
3. completes its preview
4. marks navigation dirty
5. immediately asks `FurnitureShop.TryBeginPurchase` for another fresh preview of the same `FurnitureDefinition`
6. carries forward the last preview yaw

The next preview remains unconfirmed and costs nothing until the next valid placement.

## Stopping repeat placement

RMB, Q, Esc, or leaving Store Design cancels only the current unconfirmed preview.

Already confirmed items remain purchased and placed.

No refund is involved because no money was spent on the discarded preview.

## Purchase authority

Repeat placement does not:
- clone the just-placed runtime object
- directly instantiate permanent furniture
- spend money outside FurnitureShop
- manually generate IDs
- bypass StoreFurnitureLayout
- bypass normal placement validation

Each repeated item is a fresh prefab instance created through:

`FurnitureShop -> StoreFurnitureLayout -> FurniturePlacementController`

and receives its own persistent ID.

## Rotation continuity

Before completing each purchase, `FurniturePlacementController` captures the current `previewYaw`.

The next fresh preview starts with that yaw.

This preserves:
- Structured cardinal rotation
- arbitrary Free-mode rotation
- rotation changes made during the repeat sequence

## Money behavior

After one item is paid for, repeat continuation calls normal `FurnitureShop.TryBeginPurchase`.

If the player can no longer afford another:
- the confirmed item remains
- no extra preview is created
- no extra money is spent
- the repeat sequence ends
- the normal reason is shown transiently in world interaction when appropriate

## Build Catalog coordination

No new BuildCatalogUI code is required.

For catalog-origin purchases, its existing `reopenAfterPlacement` flag remains true while the catalog is hidden.

Because the next purchase preview is created synchronously after each confirmation, `Player -> FurniturePlacementController.IsPlacementActive` remains true between repeated items, so the catalog does not flash open.

When repetition actually stops or cannot continue, placement becomes inactive and the existing BuildCatalogUI behavior reopens the catalog.

Copy-origin repetition does not set `reopenAfterPlacement`, so stopping a copy sequence returns to normal Store Design world interaction.

## Existing furniture movement

Moving an already-owned furniture instance with LMB remains one-shot.

Repeat placement applies only when `isNewFurniturePurchase` is true.

## Dynamic prompt

While a valid purchase preview is active:

`LMB Buy & place £X.XX  •  RMB Stop`

Invalid placement still shows the existing placement-invalid explanation.

No new UI GameObject or component is created.

## Scene / Inspector

No new serialized field and no new scene wiring.

Preserve current `Player -> FurniturePlacementController` values, including:
- Furniture Shop -> `StoreSystems -> FurnitureShop`
- Selection Ray Distance 35
- Preview Ray Distance 30
- Body Contact Tolerance 0.01
- Structured Placement By Default ON
- Grid Size 0.5
- Free Rotation Degrees Per Second 90
- Magnetic Snap Enabled ON
- Socket Capture Distance 0.30
- Socket Release Distance 0.55
- Maximum Socket Facing Angle 15
- Outline Width 0.01

No RectTransform changes.

## Persistence

No save schema change.

Confirmed repeated furniture already uses normal generic furniture placement persistence.

An active unconfirmed repeat preview remains a transient placement and must be cancelled/stopped before a save that requires no active placement.

## Roadmap

Next likely:
- selling / refunds
- variants / colors
- more decor / surface placement
- Ambience
- autosave
- exact carry persistence
- advanced fixture/socket semantics

Player testing may reorder this roadmap.
