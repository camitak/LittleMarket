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
- Request a completed-day snapshot from StoreReportHistory.
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
- Provide historical data for reporting UI.
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

Manager Book Reports reads this history rather than reconstructing
previous days from current gameplay state.

## GameUI

Scene GameObject: `GameUI`

Owns screen-space UI controllers including:

- `InteractionUI`
- `OrderingUI`
- `EndOfDayUI`
- `PauseMenuUI`
- `ProductCatalogueUI`
- `ManagerBookUI`
- `ReportsUI`
- `StockWatchUI`
- `StoreGoalsUI`
- `StoreLevelUI`
- `StorageCapacityUI`
- `BasketContentsHUD`
- `BasketLocatorUI`

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

### ManagerBookUI

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

Lesson 90 behavior:

- Observes `StoreDayLifecycle.IsDayComplete`.
- Opens one compact Day Complete screen when day processing is finished.
- Displays the completed day, sales revenue, and customers served.
- Shows one short achievement message, prioritizing a store level-up,
  then new product unlocks, then completion of all daily goals.
- Shows a neutral message if no such achievement occurred.
- Starts the next day through `StoreDayLifecycle.StartNextDay()`.
- Handles the transition screen's cursor, player controls, and time pause.

The former expanded end-of-day report and View Details button
are removed in Lesson 90. Detailed reports are available in
Manager Book -> Reports during normal gameplay.

EndOfDayUI reads existing gameplay data for presentation.
It does not award rewards, change reputation, grant XP, unlock products,
or write report history.

The end-of-day screen is intentionally brief and pleasant.
Financial and operational reports are not compulsory screens.

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

### Reports direction

Completed-day history is retained by StoreReportHistory and
persisted through SaveManager.

Current POC features:

- Latest completed-day report by default.
- Previous/Next day navigation.
- Financial and customer statistics.
- Goal rewards, reputation changes, XP, and Store Level.
- Names of newly unlocked products.
- A safe empty state for players without historical reports.

Future reporting improvements may include:

- Better visual hierarchy and formatting.
- Simple comparison between completed days.
- Small, useful trend indicators.
- More detailed records where gameplay justifies them.

Historical reports should not become a compulsory gameplay screen.

## OrderingTerminal

Scene GameObject: `OrderingTerminal`

Component: `OrderingTerminal`

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

## Customer checkout feedback

Prefab: `Assets/_Game/Prefabs/Characters/PF_CustomerPrototype.prefab`

Components on prefab root:

- `CustomerController`: authoritative customer behavior, checkout state,
  accumulated waiting time and satisfaction penalties.
- `CustomerQueuePatienceVisual`: presentation-only feedback when a customer
  is currently waiting and checkout delay is costing satisfaction.

Visual child: `PatienceText`, a small 3D TextMeshPro label above the customer.

Lesson 91 behavior:

- `CustomerController.IsWaitingAtCheckout` reports whether the customer is
  in `WaitingForCheckoutSpace` or `WaitingInCheckoutQueue`.
- `CustomerController.CurrentCheckoutWaitPenalty` exposes the existing
  satisfaction penalty calculation without adding duplicate timing rules.
- The patience visual is hidden before the existing checkout grace period
  is exhausted, and while the customer is shopping, walking or leaving.
- An amber `!` appears once the current waiting penalty becomes positive.
- A coral `!!` appears when the existing penalty reaches the visual's
  configurable `criticalPenalty` presentation threshold (default: 15).
- The label gently bobs and faces the player's camera. It is not a HUD.

`CustomerQueuePatienceVisual` must not change customer state, queue positions,
checkout transactions, the accumulated wait, or any satisfaction values.

Changing the marker's colors or bob animation must not change gameplay rules.
The checkout satisfaction grace period and penalty remain owned by
`CustomerController`.

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

Lesson 91 introduces prefab-only checkout patience visuals and two read-only
CustomerController properties. No additional save fields are required.

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
