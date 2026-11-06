# Little Market — System Architecture

Current architecture after **Lesson 132 — Protected Doorway Clearances**.

Verified source of truth:
`3ee596415a57489d10670480ee6f700c23a1ed56`
`Lesson 131 complete - decorations foundation and potted plant`

## Permanent circulation rule
Permanent circulation infrastructure is protected from player placement.

Doorways use the existing `LayoutClearanceZone` system rather than customer-AI special cases.

## Reused architecture
No new scripts are required.

Existing:
- `LayoutClearanceZone`
- `FurniturePlacementController.OverlapsProtectedClearance`
- `LayoutWorldVisualizer`

already provide:
- furniture-body blocking
- operational-access blocking
- StoreAreaType filtering
- Store Design purple clearance visualization

## Scene hierarchy
Under `StoreBuilding/Base` create identity root:

`LayoutClearanceZones`

Children:
- `Clearance_CustomerEntrance`
- `Clearance_CustomerExit`
- `Clearance_StaffDoor_Sales`
- `Clearance_StaffDoor_Backroom`

All clearance objects have:
- identity rotation/scale
- Transform + LayoutClearanceZone only
- no collider
- no renderer
- no NavMesh component

## Customer entrance
Door center X `5.8`, front wall near Z `-5.1`, door width `2.0m`.

Clearance transform:
`(5.8, 0, -3.9)`

Zone:
- Area Type `SalesFloor`
- Center `(0,0.75,0)`
- Half Extents `(1.10,0.75,1.10)`
- Block Furniture Body ON
- Block Furniture Access ON

Horizontal protected area:
X `4.7..6.9`
Z `-5.0..-2.8`

## Customer exit
Clearance transform:
`(-1.8, 0, -3.9)`

Same zone values as entrance.

Protected area:
X `-2.9..-0.7`
Z `-5.0..-2.8`

## Staff doorway
Partition at X `-4`, opening centered Z `2.5`, width about `1.4m`.

Because placement area is explicit, protect both sides.

Sales:
- transform `(-3.25,0,2.5)`
- Area `SalesFloor`
- Center `(0,0.75,0)`
- Half Extents `(0.75,0.75,0.80)`
- both blockers ON

Backroom:
- transform `(-4.75,0,2.5)`
- Area `Backroom`
- same center/half extents
- both blockers ON

## Visual semantics
Existing purple clearance visual remains:
`#977EFF42`

Meaning:
permanent operational/circulation space — do not build here.

Checkout and doorway clearances intentionally share the same visual language.

## Placement behavior
All placeables respect doorway clearances:
- shelves
- refrigerators
- storage racks
- potted plants
- future decor

Decor with `AccessRequirement.None` still cannot place its BODY in a doorway clearance.

Functional furniture access footprints cannot extend into doorway clearance either.

## Customer AI
No AI changes.

Current NavMesh/customer route system remains unchanged. The layout system prevents the obstruction that caused customers to route through the exit.

## Preserved systems
Unchanged:
- decoration architecture
- FurnitureAccessRequirement
- structured/free placement
- Build Catalog
- camera/input behavior
- Esc hierarchy
- preview-first/spend-on-confirm purchase
- save/load
- navigation rebuild

## Roadmap
Lesson 133 — World-Space Expansion Preview & Purchase.

Then:
- copy/eyedrop
- repeat placement
- selling/refunds
- variants/colors
- more decor/surface placement
- Ambience
- autosave
- exact carry persistence
- advanced fixture/socket semantics
