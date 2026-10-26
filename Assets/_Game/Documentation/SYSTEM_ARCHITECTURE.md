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
- `StoreDayLifecycle`

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

Owns screen-space UI controllers including:

- `InteractionUI`
- `OrderingUI`
- `EndOfDayUI`
- `PauseMenuUI`
- `ProductCatalogueUI`
- `ManagerBookUI`
- `StockWatchUI`
- `StoreGoalsUI`
- `StoreLevelUI`
- `StorageCapacityUI`
- `BasketContentsHUD`
- `BasketLocatorUI`


### StoreDayLifecycle

Scene GameObject: `StoreSystems`

Component: `StoreDayLifecycle`

Coordinates the transition between store days.

Responsibilities:

- Wait until StoreClock reports that the day has ended.
- Wait until CustomerFlow reports no active customers.
- Process end-of-day gameplay exactly once per day.
- Apply daily satisfaction through StoreReputation.
- Claim completed goal rewards through StoreGoals.
- Apply daily XP through StoreLevelProgression.
- Refresh product unlocks through StoreProgression.
- Coordinate starting the next day and resetting daily state.

StoreDayLifecycle does not define opening or closing times.
StoreClock remains authoritative for all schedule rules.

EndOfDayUI is responsible for presentation only.
It observes StoreDayLifecycle.IsDayComplete to know when
the end-of-day screen can appear.

The Start Next Day button requests the transition through
StoreDayLifecycle.StartNextDay().

The end-of-day presentation can therefore be redesigned
without transferring gameplay progression responsibilities
back into the UI.


### HUD information rule

Normal gameplay HUD should stay minimal.

Permanent information should be limited to things the player needs continuously,
such as:

- money
- current day/time/store status
- interaction feedback

Contextual information may appear only while relevant, for example:

- basket contents while carrying the restock basket
- basket locator after requesting it
- money gain feedback

Management information should live inside `ManagerBookUI` rather than remain
permanently visible.

Current Manager Book pages:

- Today
- Stock
- Catalogue

ManagerBookUI owns book navigation and modal behavior.
ProductCatalogueUI handles catalogue content and rendering,
not a separate fullscreen modal.

Future candidates include:

- Reports
- Orders
- Store progression/upgrades
- Store progression/upgrades


### EndOfDayUI

Scene GameObject: `GameUI`

Component: `EndOfDayUI`

Presentation-only controller for the end-of-day transition.

Current behavior:

- Observes `StoreDayLifecycle.IsDayComplete`.
- Opens the end-of-day screen when processing is finished.
- Displays a compact Day Complete summary by default.
- Shows sales revenue and customers served as quick highlights.
- Allows the player to expand the existing detailed statistics
  through the View Details button.
- Allows the player to return to the compact presentation.
- Starts the next day through `StoreDayLifecycle.StartNextDay()`.

The detailed presentation currently includes:

- revenue, supplier spending, and net cash flow
- customer satisfaction and reputation changes
- goal results and money rewards
- store level, XP, and product unlocks
- products missed and customers leaving without buying
- items sold, customers served, and ending cash

The detailed values are read from the existing authoritative
systems. EndOfDayUI does not calculate or award gameplay rewards.

The detailed report is optional. Players should not be forced
to read financial and operational statistics to continue playing.

### Future Reports Direction

Detailed reporting should eventually move to
`ManagerBookUI → Reports`.

This future page should make statistics available on demand,
including information from completed days where appropriate.

The current DailyStats values are reset when a new day begins.
Historical reporting therefore requires an explicit data-retention
design before removing the detailed end-of-day view entirely.

Do not introduce duplicate gameplay counters just for reporting.
Prefer capturing completed-day results from existing authoritative
systems.

The end-of-day transition should remain small, pleasant,
and focused on continuing the supermarket gameplay loop.


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