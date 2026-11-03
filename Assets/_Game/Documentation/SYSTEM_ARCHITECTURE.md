# Little Market — System Architecture

Current proposed architecture after **Lesson 119 — Layout Mode HUD Hierarchy & Control Legend**.

## Verified source of truth

Lesson 119 was designed after inspecting GitHub `main` at:

`0e1f5cce627ab2c0bdb72dbad30c6f23b19730c0`

Commit:

`Lesson 118 complete - grid snap toggle and free precision placement`

GitHub `main` remains the source of truth. Re-verify the user's actual latest push before every future lesson.

## Stack

- Unity 6.3
- C#
- JetBrains Rider
- URP
- Unity Input System
- Unity AI Navigation 2.0.14
- Repository `camitak/LittleMarket`
- branch `main`
- scene `Assets/Scenes/Market.unity`

## Core design principle

> The building provides space; the player decides how the grocery operates inside that space.

Little Market is a cozy first-person grocery-management game. Placement is a major design pillar.

## Layout Mode controls

- LMB: select / place
- R / T: rotate
- G: toggle Grid Snap
- MMB drag: orbit / tilt
- Wheel: zoom
- WASD: pan
- Shift + WASD: faster pan
- RMB / Esc / Q: cancel
- L: exit

Cursor is visible/unlocked and the normal first-person crosshair is hidden.

## Layout Mode HUD hierarchy — Lesson 119

The UI now deliberately separates three information classes.

### Persistent mode status

Lives near screen edges.

Current top-left status:

- `GRID SNAP ON · 0.10 m`
- `GRID SNAP OFF · FREE`

The `[G]` hint is removed from this status because controls are shown in the dedicated legend.

### Persistent control legend

A new non-interactive `LayoutControlsPanel` lives under:

`GameUI -> LayoutModeFrame`

It is anchored bottom-right, because the current HUD already uses:

- top-left: ClockPanel during normal play
- top-right: MoneyText / MoneyGainText
- bottom-left: BasketContentsPanel
- top-center: BasketLocator
- center-lower: InteractionPrompt

Bottom-right is therefore the least contested persistent HUD region.

The legend is static and compact. It does not use InteractionUI.

Recommended text:

`LAYOUT CONTROLS`

`LMB   Select / Place`
`R / T   Rotate      G   Grid Snap`
`MMB   Orbit         Wheel   Zoom`
`WASD   Pan          Shift   Faster`
`RMB / Esc   Cancel  L   Exit`

Both the panel Image and TMP text must have Raycast Target OFF so this informational overlay never blocks pointer-based furniture placement.

### Contextual feedback

`InteractionUI` remains the generic center-lower prompt system for short, high-attention contextual information only.

During Layout Mode it is no longer used as a persistent control legend.

Appropriate Layout Mode center prompts include:

- `LMB Move Standard Shelf`
- `LMB Buy & place £40.00`
- `Can't place here — keep furniture bodies and required customer access clear.`

When no contextual message is needed, InteractionUI is hidden.

This keeps the center of the editing view clear.

## ClockPanel visibility

Actual current normal-play ClockPanel:

- `GameUI -> ClockPanel`
- top-left
- anchored position `(30,-25)`
- size `260 x 120`
- contains DayText, TimeText, StatusText and ReputationText

Actual Lesson-118 GridSnapStatusText:

- child of `GameUI -> LayoutModeFrame`
- top-left
- anchored position `(28,-28)`
- size `340 x 28`

These overlap by design location.

Lesson 119 does not move GridSnapStatusText to work around this collision.

Instead, LayoutModeUI hides ClockPanel while Layout Mode is active and restores its exact previous active state when Layout Mode exits.

This is appropriate because StoreLayoutModeController already calls:

`storeClock.SetLayoutSuspended(true)`

while Layout Mode is active.

Therefore the normal-play time/status card is low-value while editing. Money remains visible because furniture purchasing can occur during Layout Mode.

LayoutModeUI owns this presentation transition; StoreClock remains unaware of HUD mode.

## LayoutModeUI responsibilities

`LayoutModeUI` on `GameUI` owns:

- LayoutModeFrame visibility
- normal crosshair hide/restore
- ClockPanel hide/restore during Layout Mode
- Grid Snap status presentation

Serialized references after Lesson 119:

- Frame Root -> `GameUI/LayoutModeFrame`
- Normal Crosshair -> `GameUI/Crosshair`
- Clock Panel -> `GameUI/ClockPanel`
- Placement Controller -> `Player/FurniturePlacementController`
- Grid Snap Status Text -> `GameUI/LayoutModeFrame/GridSnapStatusText`

It stores previous active state before hiding normal gameplay UI so it can restore state safely on exit.

## InteractionUI contract

`InteractionUI` itself remains unchanged.

It owns the generic `InteractionPrompt` TMP object at center-lower screen.

FurniturePlacementController changes how it uses the service:

- invalid selection -> short placement error
- valid new purchase -> short buy/place action
- hover movable furniture -> short move action
- valid existing placement -> no center prompt
- idle Layout Mode -> no center prompt

StoreLayoutModeController no longer writes a full Layout Mode control legend into InteractionUI on entry.

## Grid / precision architecture

FurniturePlacementController owns:

- `gridSnapEnabled`
- `gridSize`

Actual pushed scene values:

- Grid Snap Enabled `true`
- Grid Size `.1`
- Furniture Snap Distance `.35`
- Furniture Snap Gap `.01`
- Alignment Snap Distance `.3`

Grid ON:

- quantize floor position to `.1m`
- old provisional body/alignment assist may run
- normal validation

Grid OFF:

- raw permitted floor point
- old provisional body/alignment assist bypassed
- normal validation

The visible `.5m` LayoutWorldVisualizer grid remains independent and visible in both modes.

## Placement validation

Unchanged:

1. selected body inside build area
2. selected access inside build area
3. selected body vs explicit LayoutClearanceZone
4. selected access vs explicit LayoutClearanceZone
5. selected body vs other furniture access
6. selected body vs physical colliders
7. selected access vs physical colliders

Body vs another furniture's access is forbidden.

Access vs access remains allowed so facing furniture may share an aisle.

## Visual architecture

### LayoutWorldVisualizer
StoreSystems:
- `.5m` cyan visual reference grid
- purple explicit operational zones

### FurniturePlacementVisualizer
Player:
- selected body green/red
- selected access amber/red

### LayoutFurnitureAccessVisualizer
StoreSystems:
- existing furniture access amber
- conflicting existing access red

### LayoutVisualUtility
Rendering geometry/material helper.

### LayoutFootprintUtility
Placement footprint intersection mathematics.

Presentation and placement policy remain separate.

## FurnitureDefinition

Remains authoritative for:

- PlacementArea
- BodyCenter / BodyHalfExtents
- AccessCenter / AccessHalfExtents

Local `-Z` is functional/front direction.

## Modular production standard

For new/revised placeable furniture:

- 1 Unity unit = 1 metre
- root local position `(0,0,0)`
- root local rotation identity
- root scale `(1,1,1)`
- root pivot at floor level
- prefer X/Z root around body center
- local `-Z` = functional/front
- prefer `.25m` horizontal modules/offsets/socket positions where practical
- child art may use local scaling
- avoid arbitrary placeable-root scaling

Current four furniture prefab roots were verified compatible with the root position/scale baseline.

## Camera baseline

Preserve:

- Overview Pitch `62`
- Overview Yaw `0`
- Overview Padding `1.08`
- Fallback Distance `4`
- Min Pitch `15`
- Max Pitch `88`
- Min Distance `4`
- Max Distance `32`
- Pan `7`
- Boost `2`
- Yaw `.12`
- Pitch `.10`
- Scroll Zoom Sensitivity **2**

Future dedicated camera pass:

- area-aware sales-floor/backroom focus
- comfortable fixed editing distance
- manual wider zoom-out
- exterior focus/build region
- easing/smoothing

## Navigation

Fixed fixtures:

- FixedNavigationBlocker + NavMeshModifier

Movable furniture:

- FurnitureNavigationBlocker + NavMeshModifierVolume

Operational access is not a NavMesh obstacle.

Dirty Layout Mode sessions rebuild NavMesh once on exit.

Lesson 119 changes no navigation behavior.

## Save

- schema `2`
- `little_market_dev_v2_save.json`

No Lesson-119 HUD state is persisted.

## Forward roadmap

### Lesson 120 — Magnetic Socket Snapping

Implement a real snapping architecture independent from Grid Snap:

- explicit furniture sockets
- compatibility rules
- zero-gap adjacency
- capture distance
- attach/detach hysteresis
- alignment guides
- clear separation from the old provisional body-based snap

### Later — Layout Camera & Feel Pass

- area-aware entry focus
- fixed comfortable editing distance
- manual full-store overview
- exterior policy
- pan/orbit/zoom easing
- object-follow smoothing
- optional edge pan

## Permanent principles

- GitHub `main` is source of truth
- inspect actual current project before every lesson
- preserve user-authored tuning
- protect the center of the screen from persistent HUD clutter
- persistent mode information belongs at screen edges
- contextual prompts should be short and temporary
- mode-specific HUD can hide normal-play HUD that is not relevant
- non-interactive HUD overlays must not capture raycasts
- visual grid and Grid Snap are independent
- magnetic snapping and Grid Snap are independent
- placement, navigation and presentation remain separate
- FurnitureDefinition remains authoritative
- avoid god classes
- end each lesson with complete updated architecture and continuation prompt
