# Little Market — System Architecture

Current architecture after **Lesson 128 — In-Mode Build Catalog**.

## Verified source of truth

Lesson 128 was designed after verifying GitHub `main` at:

`395cd6c28058233507180310dcf182b0ed886ea0`

Commit:

`Lesson 127 complete - smooth local store design camera`

Re-verify newest `main` before every future lesson.

## Permanent lesson-format rule

The full implementation belongs in chat:
- code
- explanations
- Inspector/UI setup
- tests

Only the required handoff `.md` files are created separately unless explicitly requested.

## Core principles

> The building provides space; the player decides how the grocery operates inside that space.

> The clock describes the store's day; it does not grant permission to manage the store.

Store Design Mode is core cozy gameplay.

## Lesson 128 Build Catalog architecture

The Build Catalog is a NEW UI doorway over the EXISTING furniture business pipeline.

It does NOT duplicate:
- FurnitureCatalog
- FurnitureShop
- StoreEconomy spending
- StoreFurnitureLayout preview creation
- FurniturePlacementController confirmation/cancel
- save persistence

Authoritative purchase flow remains:

FurnitureDefinition
-> FurnitureCatalog
-> FurnitureShop.TryBeginPurchase
-> StoreFurnitureLayout.TryCreatePreviewFurniture
-> FurniturePlacementController.BeginPurchasedFurniturePreview
-> FurnitureShop.TryCompletePurchase on confirmed placement

Money is still spent only when placement is confirmed.
Cancel still costs nothing.

## Player flow

Inside Store Design Mode:

`B`
-> toggle Build Catalog

Choose item:
-> panel hides
-> existing furniture preview begins
-> player positions/rotates item
-> confirm or cancel
-> Build Catalog automatically reopens

The previously selected catalog category is preserved.

If the player selects/moves an existing piece of furniture while the catalog is open:
-> catalog hides while placement is active
-> catalog reopens after placement/cancel

This keeps the floor visible while manipulating objects.

## Catalog categories

New enum:

`FurnitureCatalogCategory`

Current values:
- Fixtures
- Storage
- Refrigeration
- Checkout
- Decor
- LightingAndSigns

Current UI exposes only categories that have actual content:
- All
- Fixtures
- Storage
- Refrigeration

Do not show empty Checkout/Decor/Lighting tabs yet.

Current definitions:
- Standard Shelf -> Fixtures
- Refrigerated Display -> Refrigeration
- Ambient Storage Rack -> Storage
- Chilled Storage Rack -> Storage

Future Lesson 129 will activate Decor/Lighting categories when content exists.

## BuildCatalogUI

New component on `GameUI`.

Responsibilities:
- listens for B only while Store Design Mode is active
- opens/closes `BuildCatalogPanel`
- filters existing FurnitureShop catalog
- spawns existing `PF_FurnitureCatalogRow`
- preserves active filter
- asks FurnitureShop whether purchase may begin
- starts FurnitureShop purchase
- hides during active furniture placement
- reopens after placement/cancel
- never owns purchase/economy logic

References:
- BuildCatalogPanel
- BuildCatalogStatusText
- BuildCatalogCloseButton
- four current category buttons
- BuildCatalogScrollView
- BuildCatalogListContent
- PF_FurnitureCatalogRow
- StoreLayoutModeController
- FurnitureShop
- FurniturePlacementController

## Build Catalog UI

Hierarchy:

GameUI
└── LayoutModeFrame
├── PlacementModeStatusText
├── ...
├── LayoutControlsPanel
└── BuildCatalogPanel
├── BuildCatalogTitleText
├── BuildCatalogCloseButton
├── BuildCatalogCategoryBar
│   ├── AllCategoryButton
│   ├── FixturesCategoryButton
│   ├── StorageCategoryButton
│   └── RefrigerationCategoryButton
├── BuildCatalogScrollView
│   └── Viewport
│       └── BuildCatalogListContent
└── BuildCatalogStatusText

Panel:
- anchor `(0, .5)`
- pivot `(0, .5)`
- position `(28, 0)`
- size `700 x 800`
- background `#FFF4DEEB`
- Raycast Target ON

This occupies the left side while keeping most of the store visible.

Title:
- position `(24,-20)`
- `420 x 40`
- font 30
- left/middle
- `#594640FF`
- Raycast OFF

Close:
- top-right
- position `(-20,-18)`
- `44 x 44`

Category bar:
- left/right 24
- top 74
- height 42
- HorizontalLayoutGroup
- spacing 8
- equal-width category buttons

Category selected state:
- active button disabled
- disabled/selected color `#BFE3CFFF`
- label gains `✓`
- selection is not communicated by color alone

Scroll:
- left/right 24
- top 130
- height 550
- duplicate existing FurnitureScrollView structure
- content uses existing VerticalLayoutGroup / ContentSizeFitter
- row prefab stays `PF_FurnitureCatalogRow`

Status:
- bottom
- left/right 24
- bottom 20
- height 46
- font 16
- left/middle
- `#7A6860FF`
- Raycast OFF

Default status:
`Choose an item to place. Payment happens only when placement is confirmed.`

## Row prefab cleanup

`PF_FurnitureCatalogRow` remains shared between:
- store computer furniture tab
- Store Design Build Catalog

Do not fork it.

Text Raycast Target should be OFF for:
- FurnitureNameText
- DescriptionText
- PriceText
- BuyButtonText

Root row Image remains Raycast ON so world selection does not leak through the catalog.

## Controls

Layout controls panel grows from `360 x 190` to `360 x 214`.

Text:

STORE DESIGN CONTROLS
B   Build Catalog
LMB   Select / Place
R / T   Rotate      G   Placement Mode
Ctrl   Temporary Opposite Mode
MMB   Orbit         Wheel   Zoom
WASD   Pan          Shift   Faster
RMB / Esc   Cancel  L   Exit

## Camera

Unchanged from Lesson 127:
- Working Distance 9
- Overview Pitch 62
- Overview Yaw 0
- Padding 1.08
- Fallback 4
- Min/Max pitch 15/88
- Min/Max distance 4/32
- Pan 7
- Boost 2
- Pan Smooth .08
- Yaw .12
- Pitch .10
- Scroll 2
- Orbit Smooth .06
- Zoom Smooth .10

## Placement

Unchanged from Lesson 126.

Structured:
- .5m grid
- magnetic sockets
- cardinal rotation

Free:
- raw position
- no magnet
- continuous R/T rotation

G default; Ctrl temporary inverse.

## Save / management

Unchanged:
- time/customer state never gates design, expansions, saving
- save schema remains v2
- end-of-day processing idempotent

## Roadmap

### Lesson 129 — Decorations Foundation

Goals:
- extend same FurnitureDefinition / FurnitureCatalog pipeline
- add decorative placeables without inventing a parallel DecorationDefinition
- decor may have no access footprint and/or no nav blocker when appropriate
- activate Decor and Lighting & Signs catalog categories as content appears
- establish soft Ambience direction without hard decoration chores

### Lesson 130 — World-Space Expansion Preview & Purchase

Then:
- copy / eyedrop
- repeat placement
- selling/refunds
- variants/colors
- Ambience
- autosave
- exact carry persistence
- advanced fixture/socket semantics

## Permanent engineering/UX rules

- GitHub main first
- exact current source/scene
- preserve tuned Inspector values
- one business authority, multiple UI entry points
- lesson implementation stays in chat
- only handoff `.md` files separate
- research meaningful UX decisions
- player playtesting drives iteration
- time is simulation, not permission
- save/load idempotent
- no color-only critical state
- avoid god classes
