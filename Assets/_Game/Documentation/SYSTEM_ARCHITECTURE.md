# Little Market — System Architecture (Lesson 142 candidate update)

## Verified source of truth
- Repository: `camitak/LittleMarket` `main`, commit `d75fc61a5a5e091a1c8b2e32d07d64a74d481cd4` (Lesson 141 complete - camera toolbar placement and reset view), checked 2026-11-10.
- Unity Editor version: `6000.6.0f1`.
- **Implementation status**: The Lesson 142 changes below are an implementation plan to apply and playtest. They are **not** committed or Unity-tested in this document. Do not describe them as implemented until verified locally and committed. Before Lesson 143, inspect latest GitHub `main` again and read this document plus the full affected scripts and scene.

## Verified existing design
- `Player -> PlayerController` is a first-person `CharacterController` movement/mouse-look script. `Assets/Scenes/Market.unity` serializes `moveSpeed=4.5`, `mouseSensitivity=0.09`. Camera `Player/Main Camera -> Camera` field of view is 75. `PlayerController` presently has no public mouse sensitivity access API.
- `Assets/Settings/SampleSceneProfile.asset` contains Motion Blur with `active=0`; do not turn it on. No camera bob or roll was present in inspected `PlayerController`.
- `LayoutCameraRig -> LayoutCameraController` owns a **separate Store Design camera** and is not modified in Lesson 142. User-tested preferred view rotation is `RotateViewLeft() => RotateViewBy(+45f)` and `RotateViewRight() => RotateViewBy(-45f)`; preserved as committed. Preserve all orbit, home reset, pitch, working distance, zoom=2 and movement tunings.
- `GameUI -> PauseMenuUI` owns `GameUI/PauseOverlay` and `GameUI/PauseOverlay/PausePanel` and disables `PlayerController` / `PlayerInteraction`, unlocks cursor, and sets `Time.timeScale=0` when paused. Escape ordinarily closes Pause Menu. The main panel is centered `(520x480)` and contains PauseTitleText, ResumeButton, SaveGameButton, LoadGameButton, SaveStatusText.
- `GameUI` is Screen Space Overlay; its Canvas Scaler uses Scale With Screen Size, reference resolution `(1920,1080)` and match `0.5`. The pause overlay image is fully opaque; changes to FOV are visible numerically in settings and in gameplay upon resuming, not through the backdrop.
- `StoreSystems -> SaveManager` stores shop progress to JSON under `Application.persistentDataPath`, not PlayerPrefs. Comfort preferences should be separated from store slots.
- Do not silently alter scene values, authored prefabs or ScriptableObjects. No prefab/ScriptableObject needs modification for Lesson 142.

## Lesson 142 implementation to apply
1. Add public `MouseSensitivity` read-only property and `SetMouseSensitivity(float)` to `Assets/_Game/Scripts/Player/PlayerController.cs`, preserving movement and mouse-look logic and serialized fields.
2. Add `Assets/_Game/Scripts/Player/PlayerComfortSettings.cs` as a new independent component on `Player`. It reads the *authored* camera FOV and mouse sensitivity before applying optional persistent overrides, uses `PlayerPrefs` keys scoped to Little Market, and exposes FOV / mouse-sensitivity setters and Reset-to-authored-defaults. Suggested limits: FOV 65–95 degrees and sensitivity 0.02–0.18. Save preferences only when settings screen is exited or application closes. These are per-installation preferences, independent of store saves.
3. Add `Assets/_Game/Scripts/UI/ComfortSettingsUI.cs` on `GameUI`; expose `Open`, `Close`, `IsOpen`, bind UI sliders and buttons, reflect numbers live, and call PlayerComfortSettings. Use `Slider.SetValueWithoutNotify` for panel initialization and Reset Defaults to prevent loops. `Time.timeScale=0` must not stop UI events.
4. Add a new `ComfortSettingsButton` inside `GameUI/PauseOverlay/PausePanel` at top-centre anchored position `(0,-345)`, size `(260,58)`; preserve existing Resume/Save/Load/button and status placements. Add a sibling `GameUI/PauseOverlay/ComfortSettingsPanel` centered `(520,480)`, initially inactive, with title, labels, sliders, numeric values, help note, Back and Reset Defaults buttons.
5. Add a serialized `ComfortSettingsUI` field to `GameUI -> PauseMenuUI`, first route Escape in the Comfort Settings screen to Back, then close Pause. Ensure `Close()` flushes preferences before resuming gameplay.
6. Wire exact scene object references and use numeric labels such as `75.0°` and `0.090`. Scene-authored defaults must remain **75** and **0.09**, movement speed **4.5**, motion blur off, and layout camera untouched.

## UX and persistence decisions
- Two settings only in this vertical slice: first-person FOV and mouse-look sensitivity. Avoid introducing unnecessary camera effects or overhauling HUD.
- Live slider values change the runtime first-person camera and sensitivity immediately; full-screen opaque PauseOverlay means players evaluate the visual feel once they Resume. Closing the screen saves, including when Escape backs out. Reset Defaults uses the serialized scene values captured at startup rather than hardcoding values.
- `PlayerPrefs.Save` is not invoked during every slider change; this avoids synchronous disk writes while dragging. The preferences do not modify `SaveManager` or saved shop JSON.
- Add main-menu accessibility and other comfort options, broader UI/UX consistency and future third-person/multiplayer input abstraction in later lessons based on playtesting.

## Regression checks before committing
- Settings button opens panel while time is paused; Back/Escape restores PausePanel, not gameplay; second Escape resumes.
- FOV and sensitivity sliders update numeric displays and take effect on Resume. Restart app/Unity Play Mode to verify persistence. Reset Defaults returns to authored 75 and .09 and persists. Shop Save/Load does not overwrite them.
- All preexisting Pause Save/Load/Resume and exit behaviour remains; no duplicated InputSystem handling or cursor-lock regressions.
- Store Design movement, copy/sell, reset view, Left=+45 Right=-45, zoom=2 and other tuned Inspector values stay unchanged. Motion blur remains off.
- Test at reference 1920x1080 and smaller 1280x720 including slider keyboard navigation, legibility and click targets.

**Suggested commit after successful Unity tests:** `Lesson 142 complete - persistent first-person comfort settings`
