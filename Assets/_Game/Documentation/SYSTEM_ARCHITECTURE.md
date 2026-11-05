# Little Market — System Architecture

Current architecture after **Lesson 130 — Store Design Back Navigation & Catalog Scroll Polish**.

Verified source: GitHub main `52399f509f3b2033accac2364e91fac4c7c266a5` (`Lesson 129 complete - store design input ownership and area focus`).

## Lesson 130

Player testing found:
- Build Catalog scrolling felt too fast and elastic/bouncy.
- Short categories could still scroll even when their content fit.
- Escape felt dead while Store Design was idle.

### Build Catalog scrolling
ScrollRect:
- Movement Type: Clamped
- Inertia: OFF
- Scroll Sensitivity: 12
- Horizontal: OFF

`BuildCatalogUI.RefreshScrollLayout()` dynamically enables vertical scrolling only when content height exceeds viewport height. When content fits, scrolling is disabled, movement stops, and content resets to top.

With the current catalog:
- ALL scrolls
- FIXTURES does not
- STORAGE does not
- REFRIGERATION does not

### Escape ownership
`StoreLayoutModeController` is now the sole Store Design Escape owner:
1. placement active -> cancel placement
2. Build Catalog open -> close catalog
3. otherwise -> exit Store Design

`L` still directly exits Store Design.

Outside Store Design, Esc continues to open Pause normally.

### Same-frame Pause protection
`StoreLayoutModeController` exposes `ConsumedEscapeThisFrame`.
`PauseMenuUI` refuses to open if Store Design is active or Store Design consumed Escape during the current frame. This prevents one Esc press from both exiting Store Design and opening Pause, regardless of component Update order.

`BuildCatalogUI` no longer handles Escape itself.
`FurniturePlacementController` no longer handles Escape locally; RMB/Q still cancel placement.

### UI wiring
`StoreLayoutModeController` gains a Build Catalog UI reference pointing to `GameUI / BuildCatalogUI`.

### Controls
STORE DESIGN CONTROLS
B   Build Catalog
LMB   Select / Place
R / T   Rotate      G   Placement Mode
Ctrl   Temporary Opposite Mode
MMB   Orbit         Wheel   Zoom
WASD   Pan          Shift   Faster
RMB   Cancel        Esc   Back      L   Exit

LayoutControlsPanel remains 360 x 214.

## Preserved systems
Camera values, Structured/Free placement, placement-area camera routing, purchase authority, save schema v2, EOD idempotency, and management-time principles remain unchanged.

## Next roadmap
Lesson 131 — Decorations Foundation.
Lesson 132 — World-Space Expansion Preview & Purchase.
Then copy/eyedrop, repeat placement, selling/refunds, variants/colors, Ambience, autosave, exact carry persistence, advanced fixture/socket semantics.

## Permanent rules
GitHub main first. Inspect exact source/scene. Preserve tuned values. Player testing may reorder roadmap. One owner per input/state transition. UI input must not leak into world. ScrollRect should not scroll if content fits. Entire lesson implementation stays in chat; only handoff `.md` files are separate.
