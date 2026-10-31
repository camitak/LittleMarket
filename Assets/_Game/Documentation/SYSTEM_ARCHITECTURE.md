# Little Market — System Architecture

Current architecture after **Lesson 108 — Generic Furniture Runtime & Purchasable Backroom Storage**.

## Project baseline

Lesson 108 was designed from GitHub `main` at commit:

`4e763f340cbf1ec837069f968046887b2aea93c7`

Commit message:

`Lesson 107 complete - modular store shell and spatial baseline reset`

Little Market uses Unity 6.3, URP, the Unity Input System, Unity AI Navigation/NavMesh, C#, Rider, and GitHub `main`.

## Long-term store-layout principle

Operational equipment should not be assumed to be permanently authored into the scene.

The player should eventually be able to personalize the grocery layout by placing and later purchasing additional equipment such as:

- sales shelves and refrigerated displays
- ambient and chilled backroom storage
- checkout counters
- customer basket racks
- ordering/store-management terminals
- future self-checkouts
- other operational furniture added later

The fixed spatial layer is the building shell, structural openings, build areas, and expansion boundaries. Operational equipment belongs to the generic furniture layer.

Lesson 108 does not make checkout, basket racks, or the terminal movable yet. It makes the furniture runtime architecture capable of supporting those families without making StoreFurnitureLayout shelf-specific or storage-specific.

## Generic furniture runtime participation

Lesson 108 adds `IFurnitureRuntimeParticipant`.

Furniture child components that must register with a gameplay registry implement:

- `RegisterFurnitureRuntime()`
- `UnregisterFurnitureRuntime()`

Current participants:

- ShelfSlot
- StorageBay

Future checkout, basket, terminal, or self-checkout components can participate through the same lifecycle when their systems are generalized.

`FurnitureIdentity` owns activation/deactivation of its runtime participants.

Purchased preview furniture is runtime-inactive while the player is positioning it. It becomes operational only after purchase placement is confirmed.

This prevents preview furniture from entering registries before the purchase is committed.

## Generic furniture movement constraints

Lesson 108 adds `IFurnitureMoveConstraint`.

A furniture child can decide whether the whole furniture item may currently be moved.

Current constraints:

- ShelfSlot blocks movement while it contains product units.
- StorageBay blocks movement while it contains stored units.

`FurniturePlacementController` no longer knows about ShelfSlot or StorageBay directly. It asks `FurnitureIdentity.CanMoveFurniture(...)`.

Future checkout counters, basket racks, terminals, and other equipment can provide their own movement rules without adding type-specific branches to FurniturePlacementController.

## FurnitureIdentity

FurnitureIdentity continues to own:

- stable persistent furniture instance ID
- FurnitureDefinition
- generic runtime participant lifecycle
- generic movement validation

The identity remains the persistence root for operational equipment.

## StoreFurnitureLayout

StoreFurnitureLayout is now furniture-family agnostic.

It no longer owns a ShelfRegistry reference and no longer directly registers ShelfSlot children.

It:

- captures authored FurnitureIdentity objects
- creates purchase previews
- keeps purchase previews runtime-inactive
- saves all furniture transforms
- restores authored furniture transforms
- reconstructs purchased furniture from FurnitureDefinition
- activates runtime participants after reconstruction
- deactivates runtime participants before retiring furniture
- rebuilds NavMesh

This is the persistence foundation intended for future checkout, basket rack, terminal, and self-checkout furniture.

## StorageBay identity

StorageBay no longer owns a scene-global authored `slotID`.

Each StorageBay owns a `localBayKey`, such as:

- 01
- 02
- 03
- ...
- 08

Its persistent SlotID is computed as:

`FurnitureIdentity.PersistentID + ":" + localBayKey`

Example:

`4a9...f2:03`

This allows any number of purchased storage racks without StorageRegistry ID collisions.

The pattern now matches ShelfSlot identity.

## Storage furniture

The old `PF_StorageRack_Backroom` prototype is replaced by two managed furniture prefabs.

### PF_StorageRack_Ambient

- Furniture ID: `storage_rack_ambient`
- Display name: `Ambient Storage Rack`
- placement area: Backroom
- 8 StorageBay children
- all bays Ambient
- capacity 12 per bay
- provisional price: £60

### PF_StorageRack_Chilled

- Furniture ID: `storage_rack_chilled`
- Display name: `Chilled Storage Rack`
- placement area: Backroom
- 8 StorageBay children
- all bays Chilled
- capacity 12 per bay
- provisional price: £100

Ambient products may still be stored in chilled storage according to the existing ProductStorageCondition rules.

The starter scene contains one authored Ambient Storage Rack. Because it has FurnitureIdentity and a catalog FurnitureDefinition, it is movable when empty and additional copies are purchasable.

## Storage placement footprint

Both storage-rack definitions use the same provisional footprint.

Body:

- center `(0, 0.80, 0)`
- half extents `(1.22, 0.82, 0.30)`

Front access:

- center `(0, 0.75, -0.80)`
- half extents `(1.15, 0.75, 0.50)`

The storage-rack front is local negative Z.

These footprints are gameplay/blockout values and may be refined when the production model is created, while keeping the building spatial contract stable.

## Furniture placement

FurniturePlacementController remains responsible for:

- G move
- R rotate right
- T rotate left
- E confirm
- Q cancel
- StoreAreaType placement validation
- body collision
- front-access clearance
- NavMesh rebuild

Lesson 108 fixes the furniture look-ray behavior so a non-furniture collider in front of a furniture collider is skipped rather than prematurely ending the furniture search.

Newly purchased furniture becomes runtime-active only after payment and placement confirmation.

## Store computer

No new UI tab is required.

The existing Furniture tab reads FurnitureCatalog, so adding storage definitions automatically adds:

- Ambient Storage Rack
- Chilled Storage Rack

The store computer remains the centralized active-management purchasing surface.

Future checkout counters, basket racks, terminals, and self-checkouts should also be purchased from this management system, with progression/filtering improvements added when those families are introduced.

## Save system

Lesson 108 changes persistent StorageBay IDs and adds the starter storage rack to FurnitureIdentity persistence.

This intentionally invalidates Lesson-107 development saves.

Development save schema:

`2`

File:

`little_market_dev_v2_save.json`

No migration is provided because this is pre-release prototype data and the project explicitly chose not to preserve obsolete prototype compatibility.

## Save/load order

Expansion geometry is restored before furniture layout.

Furniture layout is restored before shelf and storage inventory.

Purchased storage racks therefore exist and register their StorageBay children before StorageRegistry inventory restoration occurs.

## Current building contract

Unchanged from Lesson 107:

- 1 Unity unit = 1 metre
- starter sales floor 12 × 10 m
- starter backroom 4 × 10 m
- sales expansion toward +X
- backroom expansion toward -X
- StoreAreaType SalesFloor / Backroom
- furniture placement grid 0.5 m

Storage rack definitions require Backroom.

## Deferred operational furniture

The following are deliberately not converted in Lesson 108, but are now explicit future furniture families:

### Checkout counters

The starter checkout should eventually become movable operational furniture.

Larger stores should be able to purchase additional staffed checkouts.

Checkout architecture will need a dynamic checkout registry/selection system instead of one permanently referenced CheckoutQueue.

### Self-checkouts

Future purchasable customer-operated checkout furniture.

This will likely share checkout-service concepts while having different staffing and throughput rules.

### Customer basket racks

Basket stations should eventually be placeable so the player controls entrance-service layout.

Customer logic will eventually need to select an available/reachable basket station instead of holding one permanent scene reference.

### Store-management terminal

The terminal should eventually be placeable backroom equipment rather than a permanent scene fixture.

Moving it must not change the principle that active management remains centralized through the computer UI.

## Immediate roadmap

### Lesson 109 — expansion progression and whole-store space-management review

Validate independent SalesFloor and Backroom expansion, storage capacity, furniture density, circulation, customer navigation, and save/load with purchasable storage.

Also review how furniture purchasing should be filtered/unlocked as the catalog grows.

### Lesson 110 — dynamic service furniture foundation

Before employees, begin removing the single-hardcoded-service-fixture assumptions needed for movable checkout counters and basket racks.

The focus should be registries and dynamic service-point selection, not final checkout art.

### Lesson 111 — first employee foundation

After service furniture and physical layout are no longer hard-coded, begin the first employee system. A restocker remains the preferred first employee.

## Still deferred

- spoilage/perishables
- operating expenses
- production building art
- additional expansion tiers
- furniture selling/recovery
- final economy balancing
