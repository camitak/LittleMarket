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
- `StoreReportHistory`

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

### StoreReportHistory

Scene GameObject: `StoreSystems`

Component: `StoreReportHistory`

Owns historical reports for completed store days.

Responsibilities:

- Capture a snapshot after end-of-day progression is processed.
- Keep one historical record per completed day.
- Preserve financial, customer, reputation, goal, and level results.
- Record newly unlocked products using stable ProductIDs.
- Provide read-only historical data for future reporting UI.
- Export and restore report history through SaveManager.

StoreDayLifecycle requests report capture after:

1. Daily reputation has been processed.
2. Goal rewards have been claimed.
3. Store Level XP has been applied.
4. Product unlocks have been refreshed.

DailyStats remains authoritative for the current day's statistics.

StoreReportHistory does not maintain independent live counters.
Historical records are snapshots of completed-day results.

DailyReportData is a serializable data class, not a MonoBehaviour.

The current implementation keeps completed-day reports in a list,
ordered by day. It does not impose an arbitrary history limit.

Future Manager Book Reports pages should query StoreReportHistory
rather than reconstructing previous days from current gameplay state.


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
- Reports

ManagerBookUI owns book navigation and modal behavior.

- ProductCatalogueUI handles catalogue content and rendering.
- ReportsUI handles historical report presentation and navigation.

Individual book pages do not own player controls, cursor locking,
time scale, or their own independent modal state.

Future candidates include:

- Orders
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


### ReportsUI

Scene GameObject: `GameUI`

Component: `ReportsUI`

Owns the presentation of completed-day history in the Manager Book.

Current behavior:

- Reads historical reports from StoreReportHistory.
- Shows an empty state when no completed reports exist.
- Displays the most recently completed day when Reports is opened.
- Allows navigation between previous and next completed days.
- Disables navigation buttons at the history boundaries.
- Displays financial, customer, goal, reputation, and progression results.
- Resolves saved ProductIDs into product names through StoreProgression.

ReportsUI is a presentation-only component.

It does not:

- Track live sales or customer statistics.
- Award goal rewards or store experience.
- Change store reputation.
- Record or modify completed-day reports.
- Save or load game data.
- Own the Manager Book's input or modal behavior.

ManagerBookUI activates ReportsPage and calls
ReportsUI.ShowLatestReport().

StoreReportHistory remains the authoritative owner of completed-day
historical snapshots.

The Reports page is available only through the Manager Book.
No additional permanent gameplay HUD is introduced.


### Reports Direction

ManagerBookUI now includes a Reports page.

Completed-day history is retained by StoreReportHistory and
persisted through SaveManager.

ReportsUI provides read-only access to previous days.

Current POC features:

- Latest completed-day report by default.
- Previous/Next day navigation.
- Financial and customer statistics.
- Goal rewards, reputation changes, XP, and Store Level.
- Names of newly unlocked products.
- A safe empty state for players without historical reports.

The end-of-day transition remains compact, with its existing
optional detailed view.

A future lesson may remove the detailed end-of-day view once
the Reports page has been sufficiently playtested.

Future reporting improvements may include:

- Better visual hierarchy and formatting.
- Simple comparison between completed days.
- Small, useful trend indicators.
- More detailed records where gameplay justifies them.

Historical reports should not become a compulsory gameplay screen.


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


Current save version: 17.

Version 17 introduces persistent completed-day reports.

StoreSaveData includes a list of DailyReportData entries.

SaveManager:

- Saves the report history alongside existing store data.
- Restores historical reports when loading version 17 saves.
- Initializes empty report history when loading older saves.

Older saves remain compatible with the existing loading process.

Completed days from saves created before version 17 cannot be
reconstructed reliably and are not backfilled with invented data.

Historical reports survive daily-stat resets and save/load cycles.

The existing save restrictions remain unchanged in this version.


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