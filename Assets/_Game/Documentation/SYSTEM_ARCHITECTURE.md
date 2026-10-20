# Little Market System Architecture

This document records the current authority and scene ownership of the main systems.

When adding a feature, prefer extending the existing owner instead of duplicating
the same rule in another system.

## StoreSystems

Scene GameObject: `StoreSystems`

Owns store-wide state and rules:

- `StoreClock`
- `StoreEconomy`
- `CustomerFlow`
- `ShelfRegistry`
- `StorageRegistry`
- `SaveManager`
- `StoreProgression`
- `StoreReputation`
- `StoreGoals`
- `StoreLevelProgression`
- `StoreStockOverview`
- `WorldItemRegistry`

### StoreClock

Authoritative source for the store schedule:

- day start
- opening time
- supplier same-day cutoff
- closing time
- hard day-end time
- current day
- current game time

Other systems should query StoreClock rather than define duplicate schedule values.

Examples:

- CustomerFlow asks whether the store is open.
- Ordering/supplier systems ask whether the supplier cutoff has passed.
- OrderingTerminal asks whether closing time has passed.

## GameUI

Scene GameObject: `GameUI`

Owns screen-space UI controllers:

- `InteractionUI`
- `OrderingUI`
- `EndOfDayUI`
- `BasketLocatorUI`

UI should display and request actions.
It should not become the authoritative owner of store rules.

## OrderingTerminal

Scene GameObject: `OrderingTerminal`

Component:

- `OrderingTerminal`

Physical interaction point for:

- opening OrderingUI with E
- ending the day with Hold X after closing conditions are satisfied

It queries StoreClock and CustomerFlow.

## DeliveryZone

Scene GameObject: `DeliveryZone`

Components:

- `DeliveryZone`
- `SupplierDeliveryQueue`

### DeliveryZone

Owns physical receiving positions and currently received cartons.

### SupplierDeliveryQueue

Owns:

- pending supplier orders
- arrival-day data
- unloading due deliveries when receiving space is available

Does NOT own store schedule times.

It queries StoreClock for supplier timing.

## CheckoutCounter

Scene GameObject: `CheckoutCounter`

Components:

- `CheckoutRegister`
- `CheckoutFeedback`

Owns the active manual checkout transaction and physical scanner/bag flow.

## BasketStation

Scene GameObject: `BasketStation`

Component:

- `BasketStation`

Owns the restock basket's home/recovery interaction.

The temporary basket locator UI itself lives on GameUI.

## Player

Scene GameObject: `Player`

Components include:

- `PlayerController`
- `PlayerInteraction`

`PlayerInteraction` coordinates player interaction input.

Carry state and targeting are delegated to their dedicated classes.

## Sales Shelves

Prefab:

`Assets/_Game/Prefabs/Store/PF_Shelf.prefab`

Root-level responsibilities:

- `ShelfIdentity` = persistent furniture identity
- `SalesShelf` = category + customer stand point

Child `ShelfSlot`s own individual facing assignment and stock.

Product-specific merchandising position belongs to ProductData,
not to per-category shelf transforms.

## Persistence

`SaveManager` lives on `StoreSystems`.

SaveManager coordinates persistence but should not become the owner of gameplay rules.

Current save version: 16.

## Ownership rule

Before adding a serialized gameplay rule, ask:

1. Is this already represented by an existing system?
2. Who is the authoritative owner?
3. Can the new system query that owner instead of duplicating the value?

Examples:

Good:

`SupplierDeliveryQueue -> StoreClock.IsPastSupplierCutoff`

Bad:

`SupplierDeliveryQueue.sameDayCutoffHour = 20`
and
`StoreClock` separately owns the rest of the schedule.

## Scene reference rule

Before documenting an Inspector assignment, verify the actual owning GameObject
in `Market.unity`.

Do not use generic instructions such as "the object containing OrderingUI"
when the current scene owner is known.