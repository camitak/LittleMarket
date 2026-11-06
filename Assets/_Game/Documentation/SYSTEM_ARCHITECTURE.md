# Little Market — System Architecture

Current architecture after **Lesson 134 — Expansion Discoverability & Hover Preview UX**.

## Verified source of truth

Lesson 134 was designed after verifying GitHub `main` at:

`20202f689197cb7823010f8ce475cbea59b37500`

Commit:

`Lesson 133 complete - world space expansion purchase`

Re-verify newest `main` before every future lesson.

## Permanent teaching / implementation rules

These are project-level rules and must survive fresh chats:

- Inspect newest GitHub `main` before every lesson.
- Read this complete architecture file before proposing implementation.
- When a scene reference is known, name the exact GameObject path and component. Never write vague instructions such as “your existing LayoutCameraController” or “existing FurniturePlacementController”.
- Current exact references include:
    - `LayoutCameraRig -> LayoutCameraController`
    - `Player -> FurniturePlacementController`
    - `GameUI -> BuildCatalogUI`
    - `StoreSystems -> StoreLayoutModeController`
    - `StoreSystems -> LayoutWorldVisualizer`
    - `StoreSystems -> StoreExpansionSystem`
    - `StoreSystems -> StoreExpansionLayoutController`
    - `StoreSystems -> StoreExpansionWorldVisualizer`
- Every UI setup instruction must say which Unity UI object/component to create or add, the exact hierarchy path, anchor Min/Max, pivot, Anchored Position or offsets with correct sign, size, font/alignment, Raycast Target, references, and colors when relevant.
- For top-anchored RectTransforms, moving downward normally means a negative Anchored Position Y. For bottom-anchored RectTransforms, moving upward normally means a positive Anchored Position Y. Verify the exact RectTransform mode before describing a value as “Top”, “Bottom”, or “Position”.
- Preserve user-tuned scene values from GitHub unless the lesson explicitly changes them.
- C# formatting must stay conventional and compact. Do not split simple declarations, method signatures, conditions, or method calls across many lines merely for wrapping. Wrap only when a line is genuinely too long or readability materially improves.
- Entire lesson implementation stays together in chat. Only the architecture and fresh-chat continuation `.md` files are separate unless explicitly requested.

## Lesson 134 UX rule

Expansion interaction now has two levels.

### Hover preview
Hovering a locked expansion:
- immediately highlights the expansion
- immediately opens `ExpansionDetailsPanel`
- immediately explains price, area, Store Level requirement, and why it is unavailable
- does not move the camera
- does not arm purchase
- panel disappears when the pointer leaves the expansion

The hover panel is intentionally non-interactive.

### Pinned selection
Clicking a locked expansion:
- pins the details panel
- smoothly focuses the local expansion bounds
- makes the panel interactive
- allows the existing two-step BUY -> CONFIRM permanent purchase
- remains open while the pointer moves onto the panel

Clicking another expansion changes the pinned selection.
Clicking empty world or pressing Esc clears the pinned selection.

## Expansion panel CanvasGroup

`GameUI / LayoutModeFrame / ExpansionDetailsPanel` now also has a `CanvasGroup`.

Hover preview:
- Alpha 1
- Interactable OFF
- Blocks Raycasts OFF

Pinned selection:
- Alpha 1
- Interactable ON
- Blocks Raycasts ON

This prevents the right-side panel from stealing the pointer from the world expansion during hover and avoids hover/show/hide flicker.

The close button is hidden during hover preview and shown for pinned selection.

## Verified current ExpansionDetailsPanel RectTransforms

The latest committed scene already contains the user's corrected values:

`ExpansionDetailsPanel`
- anchor Min/Max `(1,0.5)`
- pivot `(1,0.5)`
- anchored position `(-28,0)`
- size `460 x 320`

`ExpansionTitleText`
- top-left
- anchored position `(20,-18)`
- size `340 x 38`

`ExpansionCloseButton`
- top-right
- anchored position `(-18,-18)`
- size `40 x 40`

`ExpansionDescriptionText`
- top stretch
- anchored position `(0,-100)`
- size delta `(-40,68)`

`ExpansionRequirementText`
- top stretch
- anchored position `(0,-142)`
- size delta `(-40,24)`

`ExpansionPriceText`
- top stretch
- anchored position `(0,-172)`
- size delta `(-40,30)`

`ExpansionStatusText`
- bottom stretch
- anchored position `(0,80)`
- size delta `(-40,50)`
- pivot `(0.5,0)`

`ExpansionPurchaseButton`
- bottom stretch
- anchored position `(0,20)`
- size delta `(-40,50)`
- pivot `(0.5,0)`

Do not reset these signs.

The committed panel Image is currently approximately `#FFF4DE64`; preserve the committed scene value unless a later visual-polish lesson intentionally changes it.

## Expansion world visual states

Neutral locked:
- fill `#625D7040`
- border `#B9AFCE99`

Purchasable hover/pinned:
- fill `#6FCF9738`
- border `#A9E6D5E6`

Store-level locked while idle:
- fill `#6B657A33`
- border `#8A7F96A6`

Unavailable hover/pinned:
- fill `#D9A57A38`
- border `#E6B98FE6`

The warm unavailable focus is not the sole explanation. The details panel always provides the textual reason, such as Store Level requirement or insufficient money.

## Expansion authority

`StoreExpansionSystem` remains the only business authority for ownership, Store Level, affordability, spending, unlock activation, NavMesh rebuild, and save/load IDs.

Hover/pinned UX never duplicates those rules.

## Persistence

No save schema change.

## Roadmap

Next likely:
- copy / eyedropper placement
- repeat placement
- selling / refunds
- variants / colors
- more decor / surface placement
- Ambience
- autosave
- exact carry persistence
- advanced fixture/socket semantics

Player testing may reorder this roadmap.
