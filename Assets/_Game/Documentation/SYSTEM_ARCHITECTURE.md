# Little Market — System Architecture

Current architecture after **Lesson 138 — Managed Core Fixtures & Sellable Starter Furniture**.

## Verified source of truth

Lesson 138 was designed after verifying GitHub `main` at:

`69fdb49de652e4babfe3a509170c58f20e223dd9`

Commit:

`Lesson 137 complete - safe furniture selling and refunds`

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

## Design direction confirmed by Lesson 138

Starter furniture is no longer a permanent special ownership class.

“Starter” means:
- present in a fresh store scene/save baseline

It does NOT mean:
- permanently unsellable
- outside normal furniture ownership

Authored furniture can now be absent from a save. StoreFurnitureLayout archives authored scene objects instead of destroying them so a same-session load can restore the exact authored object and its scene overrides.

## StoreFurnitureLayout persistence model

At Awake:
- capture authored FurnitureIdentity objects
- capture their authored Pose
- retain exact authored object references

Retirement:
- player-purchased furniture: unregister -> deactivate -> destroy
- authored furniture: unregister -> deactivate -> keep archived

Save:
- only active managed furniture is written
- zero active sellable furniture is valid

Load:
- retire every current active managed furniture
- for a saved authored ID: reactivate the exact archived scene object and restore pose
- for a saved runtime ID: instantiate from FurnitureDefinition prefab

This preserves authored scene overrides such as the bakery shelf configuration while allowing authored objects to be sold.

No save schema version change.

## FurnitureDefinition availability

FurnitureDefinition now has:
- `Purchasable`
- `Sellable`
- `Max Owned` (0 = unlimited)

Existing shelf/storage/decor:
- Purchasable ON
- Sellable ON
- Max Owned 0

Managed core fixture definitions:
- Checkout Counter: Purchasable ON, Sellable ON, Max Owned 1
- Restock Basket Station: Purchasable ON, Sellable ON, Max Owned 1
- Customer Basket Rack: Purchasable ON, Sellable ON, Max Owned 1
- Ordering Terminal: Purchasable OFF, Sellable OFF, Max Owned 1

All four definitions remain in SO_FurnitureCatalog because StoreFurnitureLayout persistence resolves definitions through that catalog.

Build Catalog and Store Computer Furniture list hide definitions that are not currently purchasable. A max-owned fixture disappears while owned and becomes available again after sale.

## Core fixture scene roots

Exact current root objects:
- `CheckoutCounter`
- `BasketStation`
- `CustomerBasketStation`
- `OrderingTerminal`

All become managed FurnitureIdentity roots.

### CheckoutCounter

Current authored transform:
- Position `(0.3, 0, -3.4)`
- Rotation Y `180`

Managed components:
- BoxCollider
- CheckoutQueue
- CheckoutRegister
- CheckoutFeedback
- FurnitureIdentity
- NavMeshModifierVolume
- FurnitureNavigationBlocker

Remove:
- FixedNavigationBlocker
- LayoutClearanceZone
- NavMeshModifier

Checkout operational clearance moves into the FurnitureDefinition:
- Body center `(0, 0.5, 0)`
- Body half extents `(0.9, 0.5, 0.4)`
- Access center `(0, 0.75, -2)`
- Access half extents `(0.8, 0.75, 1.55)`
- Sales Floor
- FrontOperational

Queue points, scan point and bag point are children of CheckoutCounter, so they move with the fixture.

Checkout sale is blocked while a customer is queued or a checkout transaction is active.

### BasketStation

Current authored transform:
- Position `(-6.2, 0, -2.8)`
- Rotation Y `0`

Managed components:
- existing trigger BoxCollider
- BasketStation
- FurnitureIdentity
- NavMeshModifierVolume
- FurnitureNavigationBlocker

Definition:
- Backroom
- Body center `(0, 0.6, 0)`
- Body half extents `(0.65, 0.6, 0.5)`
- Access center `(0, 0.75, -0.9)`
- Access half extents `(0.65, 0.75, 0.4)`
- FrontOperational

The unique RestockBasket is never owned by the furniture root.
When the station retires while the basket is docked, the basket is released into the world before the station is deactivated/destroyed.
A confirmed/active station may dock an available empty basket; an unconfirmed preview never steals the basket.

### CustomerBasketStation

Current authored transform:
- Position `(4.2, 0, -3.173)`
- Rotation Y `0`

Current pickup point:
- local position `(0.9, 0, 0.3)`

Managed components:
- CustomerBasketStation
- BoxCollider
- FurnitureIdentity
- NavMeshModifierVolume
- FurnitureNavigationBlocker

Remove:
- FixedNavigationBlocker
- NavMeshModifier

Definition:
- Sales Floor
- Body center `(0, 0.2, 0)`
- Body half extents `(0.475, 0.325, 0.36)`
- Access center `(0.9, 0.75, 0.3)`
- Access half extents `(0.55, 0.75, 0.55)`
- FrontOperational

CustomerFlow refreshes the active checkout/customer basket fixtures after Layout Mode and before spawning customers.

### OrderingTerminal

Current authored transform before pivot normalization:
- Position `(-5, 1, 0)`
- Rotation Y `90`

Normalize the furniture root to floor:
- OrderingTerminal root Position `(-5, 0, 0)`
- root rotation unchanged Y `90`
- root BoxCollider Center `(0, 1, 0)`
- root size `(0.9, 1.3, 0.5)`
- `OrderingTerminal/Visual` local Position `(0, 1, 0)`

This preserves the existing world visual position while giving the managed furniture a floor pivot.

Managed components:
- OrderingTerminal
- BoxCollider
- FurnitureIdentity
- NavMeshModifierVolume
- FurnitureNavigationBlocker

Definition:
- Backroom
- Body center `(0, 1, 0)`
- Body half extents `(0.45, 0.65, 0.25)`
- Access center `(0, 0.75, -0.65)`
- Access half extents `(0.55, 0.75, 0.4)`
- FrontOperational
- Purchasable OFF
- Sellable OFF

OrderingTerminal resolves its external OrderingUI / StoreClock / CustomerFlow references at runtime when prefab-created references are null.

## Runtime fixture rebinding

Because repurchasing destroys/recreates fixture components, global systems may not keep one authored object reference forever.

CustomerFlow:
- refreshes active CheckoutQueue and CustomerBasketStation before spawn
- refreshes them before customers resume after Layout Mode
- active customers receive updated fixture references

CustomerController:
- if its checkout queue changed during Layout Mode, it clears the stale queue point
- when resuming checkout with no assigned queue point, it attempts to join the current checkout instead of immediately leaving

SaveManager:
- checkout is no longer required as a permanent serialized configuration dependency
- ResetForLoad finds the active CheckoutRegister at runtime if one exists
- saving/loading remains valid even when no checkout currently exists

CheckoutRegister:
- runtime-created checkout resolves StoreEconomy / DailyStats / internal queue/feedback when missing
- implements IFurnitureSellConstraint
- cannot be sold while a customer is queued or checkout transaction is active

CheckoutFeedback:
- finds MoneyFeedbackUI at runtime when prefab reference is null

BasketStation:
- resolves the unique RestockBasket and BasketLocatorUI at runtime
- implements IFurnitureRuntimeParticipant so docking/release follows furniture activation

RestockBasket:
- adds ReleaseFromDock() so a station can be retired without destroying the unique basket

## Store Design UX direction

Lesson 138 keeps the prototype C/X interaction so the ownership mechanics can stabilize.

The next recommended lesson after playtesting Lesson 138 is a dedicated Store Design interaction UX pass BEFORE variants/colors.

That UX pass should likely move toward:
- explicit selected-object state
- compact contextual action panel
- Move / Copy / Sell actions presented together
- keyboard shortcuts as accelerators, not the only discoverability
- immediate reason text for unavailable actions
- cleaner hover vs selection behavior

Do not add more major Store Design object actions before revisiting this UX.

## Existing tuned values to preserve

- `LayoutCameraRig -> LayoutCameraController -> Scroll Zoom Sensitivity = 2`
- `Player -> FurniturePlacementController -> Outline Width = 0.01`
- Grid Size `0.5`
- Socket Capture Distance `0.30`
- Socket Release Distance `0.55`
- Maximum Socket Facing Angle `15`

## Roadmap

Recommended next:
1. Store Design interaction UX overhaul
2. furniture variants/colors
3. more decor / surface placement
4. Ambience
5. autosave
6. exact carry persistence
7. advanced fixture/socket semantics
8. multi-checkout / multiple infrastructure models when needed

Player testing may reorder this roadmap.
