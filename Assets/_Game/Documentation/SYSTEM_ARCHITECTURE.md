# Little Market — System Architecture

Current architecture after **Lesson 131 — Decorations Foundation**.

## Verified source of truth

Lesson 131 was designed after verifying GitHub `main` at:

`e471f74834eae57f03c7e37539bf8f8596f5cfe6`

Commit:

`Lesson 130 complete - catalog scroll and store design back navigation`

Re-verify newest `main` before every future lesson.

## Core placeable architecture

Decorations use the SAME architecture as functional furniture:

FurnitureDefinition
-> FurnitureCatalog
-> FurnitureShop
-> StoreFurnitureLayout
-> FurniturePlacementController
-> save/load

There is no parallel DecorationDefinition or decoration save system.

## Operational access semantics

New enum:

`FurnitureAccessRequirement`

Values:
- `FrontOperational = 0`
- `None = 1`

The legacy/default value is deliberately `0 = FrontOperational`, so existing furniture assets preserve their access behavior even before they serialize the new field.

FurnitureDefinition now exposes:
- `AccessRequirement`
- `RequiresOperationalAccess`

Functional furniture keeps FrontOperational:
- Standard Shelf
- Refrigerated Display
- Ambient Storage Rack
- Chilled Storage Rack

The first decoration, Potted Plant, uses None.

## Placement rules

All placeables still require a BODY footprint.

Body validation always:
- stays inside required StoreAreaType
- respects fixed LayoutClearanceZone body restrictions
- cannot overlap physical colliders
- cannot occupy another functional furniture item's operational access footprint

Operational-access validation is conditional.

When RequiresOperationalAccess is true:
- access footprint must stay inside area
- access footprint respects protected clearances
- access footprint cannot overlap blocking colliders
- access preview/outline is visible

When false:
- no access-in-area test
- no access-clearance test
- no access-collider test
- no access preview/outline

This means decor loses only its own service/access requirement, not protections around real store operations.

## Visualizers

FurniturePlacementVisualizer gains a `showAccess` argument.
- body footprint always shown
- access footprint enabled only for definitions requiring operational access

LayoutFurnitureAccessVisualizer skips existing furniture whose definition does not require operational access.

Functional access zones therefore remain visible; decor does not create meaningless yellow footprints.

## Navigation

Navigation remains component-driven.

Lesson 131 Potted Plant is floor-standing and physically occupies customer space, so its prefab contains:
- BoxCollider
- NavMeshModifierVolume
- FurnitureNavigationBlocker

No new navigation system is introduced.

Future surface/tabletop decor may omit FurnitureNavigationBlocker when appropriate, but that requires a later surface-placement lesson.

## First decoration

New catalog item:

Potted Plant
- Furniture ID: `decor_potted_plant`
- Category: Decor
- Price: £25
- Placement Area: SalesFloor
- Access Requirement: None
- Body Center: `(0, 0.68, 0)`
- Body Half Extents: `(0.40, 0.68, 0.40)`

Prefab:
`PF_Decor_PottedPlant`

It is built from simple primitive meshes in a warm cozy low-poly style.
Root stays at floor pivot, position zero, rotation zero, scale one.

No FurnitureSnapSocket is added.

## Build Catalog

Decor category is now exposed because actual Decor content exists.

Current categories:
- All
- Fixtures
- Storage
- Refrigeration
- Decor

New `DecorCategoryButton` is wired to BuildCatalogUI.

The category bar keeps the existing HorizontalLayoutGroup. Category TMP labels use font size 14 to comfortably fit five equal-width buttons.

Build Catalog scroll rules from Lesson 130 remain:
- Clamped
- no inertia
- sensitivity 12
- vertical enabled only when content overflows

## Ambience

No Ambience score/stat is implemented yet.

Direction remains:
- creativity first
- soft benefits later
- diminishing returns
- variety over spam
- no hard “place N decorations” chores

Lesson 131 proves the placeable semantics before adding scoring.

## Preserved systems

Unchanged:
- Structured/Free placement
- 0.5m structured grid
- magnetic snap architecture
- camera input isolation
- cross-area purchase camera routing
- Store Design Esc hierarchy
- purchase preview-first / spend-on-confirm
- schema v2 saves
- EOD idempotency
- time does not gate management

## Roadmap

### Lesson 132 — World-Space Expansion Preview & Purchase

Then:
- copy/eyedrop
- repeat placement
- selling/refunds
- variants/colors
- more decor types / surface placement
- Ambience
- autosave
- exact carry persistence
- advanced fixture/socket semantics

## Permanent rules

- GitHub main first
- inspect exact current source/scene/assets
- preserve tuned values
- one placeable/purchase/save authority
- only add special semantics where the object actually needs them
- body footprint remains authoritative for physical occupancy
- operational access remains explicit, not inferred from catalog category
- player testing may reorder roadmap
- implementation stays in chat
- only handoff `.md` files separate
