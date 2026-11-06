# Little Market — System Architecture

Current architecture after **Lesson 133 — World-Space Expansion Preview & Purchase**.

Verified source: `3c5b57043e8edd42b3e5d26d8d4ed8705de46f15` (`Lesson 132 complete - protect store doorway clearances`). Re-verify newest `main` before every future lesson.

## Expansion authority
`StoreExpansionSystem` remains the single business authority for expansion ownership, Store Level requirements, affordability, spending, unlock activation, NavMesh rebuild, and save/load IDs. The Store Computer Expansion tab and Store Design world interaction both call it.

## Authored locked-area bounds
`StoreBuildZone.TryGetAuthoredBounds(out Bounds)` derives BoxCollider bounds directly from authored local geometry, so locked floor preview remains available even while its collider/root is disabled. `StoreExpansionZone.TryGetPreviewBounds(out Bounds)` delegates to it.

## World-space expansion visualization
New `StoreExpansionWorldVisualizer` generates runtime overlays for locked zones with no trigger colliders. Ray/plane hit testing uses authored floor bounds.

States:
- neutral locked / insufficient money: fill `#625D7040`, border `#B9AFCE99`
- purchasable hover/selected: fill `#6FCF9738`, border `#A9E6D5E6`
- Store-Level locked: fill `#6B657A33`, border `#8A7F96A6`

Unlocked zones hide the expansion overlay and use the normal cyan build grid.

## Store Design interaction
New `StoreExpansionLayoutController` handles transient selection/UI only. Selection is available when Store Design is active, no furniture placement is active, Build Catalog is closed, camera is not orbiting, and pointer is not over UI.

Clicking a locked zone selects it, opens the details panel, and calls `LayoutCameraController.FocusWorkingBounds` for smooth local focus without full auto-fit.

## Purchase confirmation
First BUY EXPANSION click arms confirmation without spending. Second click calls `StoreExpansionSystem.TryPurchase`.

Successful purchase:
- spends money
- activates hidden expansion geometry/lights
- removes barrier wall
- enables StoreBuildZone
- rebuilds NavMesh
- refreshes locked expansion overlays
- refreshes cyan build grid immediately
- leaves panel showing OWNED
- new floor is immediately buildable

No time-of-day restriction.

## LayoutWorldVisualizer
Adds public `RefreshVisuals()` so available floor grid and clearance visuals can rebuild while Layout Mode is already active.

## UI
`GameUI / LayoutModeFrame / ExpansionDetailsPanel`
- right-center
- 460 x 320
- anchored position `(-28,0)`
- background `#FFF4DEEB`
- raycast ON

Children:
- ExpansionTitleText
- ExpansionCloseButton
- ExpansionDescriptionText
- ExpansionRequirementText
- ExpansionPriceText
- ExpansionStatusText
- ExpansionPurchaseButton / ExpansionPurchaseButtonText

Inactive by default.

## Current expansions
- `sales_floor_01`: 4x10 at `(10,0,0)`, £100, Store Level 1, SalesFloor
- `backroom_01`: 4x10 at `(-10,0,0)`, £100, Store Level 1, Backroom
- `sales_rear_01`: 12x4 at `(2,0,7)`, £120, Store Level 2, SalesFloor

## Escape ownership
1. furniture placement
2. Build Catalog
3. expansion selection/details
4. exit Store Design

L remains direct exit. Same-frame Pause protection remains.

## Persistence
Existing save schema v2 already stores `unlockedExpansionIDs`; no schema bump.

## Next roadmap
Copy/eyedrop, repeat placement, selling/refunds, variants/colors, more decor/surface placement, Ambience, autosave, exact carry persistence, advanced fixture/socket semantics.

Permanent rules: GitHub first; one expansion business authority; locked preview/selectability is not buildability; no trigger colliders just for editor selection; time is not permission; preserve local camera behavior; player testing may reorder roadmap; implementation stays in chat.
