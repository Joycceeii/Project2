# Project Continuity and Agent Change Log

## 2026-10-29 - Ending Button Wording

- End page button now reads Return to Start in both prefab and legacy flows. Existing return-to-cover and L01 restart behavior is unchanged.

## 2026-10-29 - Preserve Unlocked Next Level Across Attempt Resets

- Added HasUnlockedNextLevel separately from per-attempt correctness. A Correct evaluation latches the current level's progression unlock; Reset Attempt and later Wrong/Close evaluations preserve it.
- LevelManager and the existing button refresh use the latched unlock. SetLevel clears it for a newly loaded level or Back to Cover restart; evaluated-attempt editing locks remain unchanged.
- Runtime/Editor compilation passed with zero warnings/errors. An isolated harness using the actual ResetAttempt, SetLevel, MarkEvaluated and CanAdvance source passed 156 state checks across 12 levels; Unity scene interactions were stubbed for that harness.

## 2026-10-29 - Move Ingredient Plate Row Toward Screen Bottom

- Moved all four authored ingredient slots 0.45 units toward the camera along Z, retaining Y, spacing, rotation and scale. Child plates, ingredients and placement points move together.
- Removed the editor synchronization script at the user's request. Adjust Ingredient Slot 1-4 directly in Scene view; their child plates, ingredients and placement points move together.

## 2026-10-29 - Kitchen Background Texture Import Repair

- Replaced the incomplete background texture importer settings with a complete TextureImporter configuration and forced Unity reimport. Runtime checks now resolve both the Texture2D and material texture.
- Confirmed the kitchen background renders behind the table in a 1920x1080 camera capture. Preview saved under Art/Backgrounds/LingnanKitchen-in-game-preview.png (camera render excludes overlay UI). Removed temporary editor diagnostics after validation.

## 2026-10-29 - Lingnan Kitchen Gameplay Background

- Added the generated kitchen illustration and URP unlit material under Resources/Backgrounds. Gameplay startup creates a camera-aligned background quad behind scene geometry, preserving the authored table and UI.
- Background fills the viewport with proportional cropping, follows camera framing, casts no shadows, and has no active collider. Runtime and Editor compilation passed with zero warnings/errors; in-game visual confirmation remains pending.

## 2026-10-29 - Restore Gameplay Audio Listener

- Fixed silent grinding audio: the gameplay scene Main Camera had no AudioListener. Added one enabled listener to the saved scene and a missing-listener fallback for generated test scenes.
- Verified exactly one serialized gameplay listener on Main Camera. In-game listening confirmation remains pending.

## 2026-10-29 - Grinding Interaction Audio

- Converted the supplied grinding recording to Resources/Audio/Grinding.wav and loop it only during effective pestle movement inside the mortar.
- Stop audio on mouse release, stationary/outside motion, reset, evaluated attempts, disabled gameplay camera, lost focus, and component disable. Clip and volume are editable on PestleController.
- In-game listening and loop/volume confirmation remain pending.

## 2026-10-29 - Experiment Log Status Ink Colors

- Correct/Incorrect labels use deep green (#2F5D46) and deep red (#8A3D3A), with a matching journal color guide.
- Runtime and Editor compilation passed with zero warnings/errors; in-game visual confirmation remains pending.

## 2026-10-27 - Lower Mortar Ingredients and Final Interaction Checks

- Lowered serialized ingredient mortar offsets to -0.10 across all 17 ingredient prefabs and existing scene draggable objects. Fixed the missing-placement-point fallback to actually honor that offset; authored placement points still remain editable.
- Returned prepared powders reapply their -0.06 ground offset once on every mortar drop, preventing them from jumping upward or accumulating downward drift. Legacy powder anchoring now honors negative mortarGroundVisualLift values.
- Evaluated attempts reject ingredient additions/removals and new batches until Reset Attempt. Ingredient dragging and pestle grinding are also stopped after evaluation, preserving the evaluated recipe and progression state.
- Ignored Unity-generated .slnx solution files alongside existing .sln exclusions.
- Unity batch compilation and regression checks passed: fallback/explicit mortar placement, repeated powder transition/return, all 12 Correct-only progression gates and reset locks, evaluated-attempt mutation blocking, End return/Cover ordering, 17 prefabs/30 visual sizes, missing-script checks, and Cover/Begin/End resources. In-game visual height confirmation remains pending.


## 2026-10-27 - Correct-Only Progression and Ending Return

- Next Level is enabled only after a Correct evaluation of the current level. LevelManager also checks the same condition before advancing or displaying End; Wrong, Close, and unevaluated attempts cannot skip ahead.
- Reset Attempt and loading a level clear successful evaluation state. Additive Experiment Log preserves the current attempt as before.
- End page button now reads Back to Cover and restarts at L01, clearing the session's shown story-page keys so Cover, Begin, and later story pages can play again. Existing experiment records and clues are retained.
- Updated both prefab-backed and legacy story-button flows. Unity batch compilation and checks for all 12 levels (unevaluated/Wrong/Close/Correct/reset), plus End-to-L01 story-key reset and Cover resource ordering, passed.


## 2026-10-27 - Consistent Ingredient Plate Footprints

- Calibrated all 17 PF ingredient prefabs (30 raw/ground visual states) to a 0.60-unit maximum X/Z mesh footprint, relative to the approximately 0.92-unit plate. Preserved model proportions, visual centers/base heights, and prefab root transforms.
- Replaced the scene's obsolete unpacked Rice ingredient (root scale 0.3) with a connected PF_Rice instance at unit scale. Preserved gameplay bindings and aligned its raw mesh bottom 0.005 above its plate; updated its placement point.
- Rice raw and powder now use the same footprint as other ingredients. All tuning is serialized in prefabs; runtime does not normalize sizes.
- Unity batch import/compilation and saved mesh/scene verification completed; final in-game visual confirmation remains pending.


## 2026-10-27 - TeaLeaf Raw Visual Scale

- Reduced PF_TeaLeaf Before Grinding visual scale from 1 to 0.4 on all axes to address oversized raw leaves. After Grinding remains at its existing 0.4 scale.
- Kept root interaction scale and placement points unchanged. Visual size balance still requires Unity confirmation because the two source meshes have different native dimensions.

## 2026-10-27 - Source Ingredient Model Audit

- SHA256 comparison matched every existing ingredient lod.fbx/lod_basic_pbr.fbx to its original desktop source folder. Audited PF ingredient nested source references for raw/ground roles.
- Rice and TeaLeaf raw visual references restored to existing imported prefabs; ground references retained.
- Prepare Ingredient Placement Points now also imports the missing source chili model/textures and connects its Before Grinding branch, preserving the existing ground prefab. This import executes only when the menu is invoked; not yet executed by the agent.
- No source matches exist for CoarseSalt or ScallionWhite. The bambooleaf-to-LotusLeaf correspondence remains unconfirmed and was not substituted.
- Runtime and Editor compilation passed; Unity import/render validation remains pending.

## 2026-10-27 - Restore Supplied Rice and TeaLeaf Models

- PF_Rice Before Grinding now nests the existing Rice_3D prefab; disabled its five simplified Grain objects without deleting them. Preserved its powder reference.
- PF_TeaLeaf Before Grinding now nests the existing TeaLeaf_3D prefab, with the simplified legacy leaf branch inactive. Preserved TeaPowder_3D for After Grinding.
- Changes are serialized prefab references; Unity import and visual confirmation remain pending.

## 2026-10-27 - Centered Ingredient Rows and TeaLeaf Visibility

- Active plate slots now center symmetrically by ingredient count using editable row center X and spacing; authored Y/Z positions remain intact.
- Restored TeaLeaf's existing leaf visuals: Before Grinding contained only an inactive legacy leaf branch after removal of the broken imported reference. No valid imported before-grinding model was present.
- The edit-mode placement command restores Rice to its prefab-authored root scale and places ingredients 0.005 above the plate mesh rather than 0.015.
- Added independently editable mortar ingredient (-0.05) and powder (-0.06) vertical offsets; powder offset applies once on the first ground-state transition.
- Runtime and Editor builds passed; Unity visual validation remains pending.

## 2026-10-27 - Horizontal Plate Row and Editor Surface Placement

- Authored the four scene Slot positions as a horizontal row at X -1.8, -0.6, 0.6, 1.8, retaining Slot 1's Y and Z.
- Disabled the legacy scene Rice Portion Cluster; the five authored Grain objects remain, and the complete Rice prefab also contains five before-grinding grains.
- Extended Prepare Ingredient Placement Points to arrange the open Scene's slots and align visible ingredient mesh bottoms above visible plate mesh bounds once in edit mode. Saved points remain manually editable; runtime does not repeat the bounds alignment.
- Runtime and Editor compilation passed with zero warnings/errors. The menu must be run outside Play Mode for the open Scene; visual validation remains pending.

## 2026-10-27 - Explicit Ingredient Placement Points

- Added plate and mortar placement Transform references; return-home and accepted mortar drops use their authored positions.
- Removed startup visual-height normalization and existing-plate Transform resets. Different ingredient prefabs no longer inherit the previous ingredient's scale and rotation.
- Legacy ingredient objects lacking IngredientVisualState are replaced with their complete ingredient prefab. Same-ingredient migration retains its authored root transform.
- Added `The Taste Reviver > Prepare Ingredient Placement Points` for an undoable edit-mode migration in the currently open Scene; it creates editable points and replaces legacy ingredient visuals without saving the Scene automatically.
- C# runtime and Editor compilation passed. Unity visual and interaction verification remains pending; the edit-mode menu must be run and its Scene saved by the user.

## 2026-10-27 - Stop Ingredient Home-Position Resets

- Removed the automatic `ReturnHome` call from `LevelIngredientDisplayManager` during level display refreshes.
- Removed the manager's fixed plate-height placement and the initializer's global visual-height overrides. Existing Ingredient Slot child transforms now remain at their authored Scene positions, rotations, and scales; newly instantiated ingredient Prefabs keep their own authored root transform, while the manager only binds the correct ingredient data and preserves the separate runtime mortar placement when an ingredient is dragged into the bowl.
- Runtime and Editor C# projects compile with zero warnings and zero errors.

## 2026-10-27 - Preserve Ingredient Transforms Across Display Refreshes

- Kept `LevelIngredientDisplayManager` because it still owns level-to-ingredient binding, drag setup, plate visuals, and powder displays, but stopped it from losing authored transforms when it refreshes or swaps a prefab.
- Existing Ingredient objects are returned to their authored home state before refresh, and their local position, rotation, and scale are carried onto any replacement prefab.
- Runtime and Editor C# projects compile with zero warnings and zero errors.

## 2026-10-27 - Stable Ingredient Plate Anchor

- Removed imported-model-bounds auto-placement from `LevelIngredientDisplayManager`; large FBX child bounds could move an ingredient such as TeaLeaf to local Y `2.528`, making it appear missing in level 2.
- New or prefab-swapped ingredients now start from the stable plate anchor `Y=0.16`, while existing authored positions remain untouched for manual Scene editing.
- Runtime and Editor C# projects compile with zero warnings and zero errors.

## 2026-10-27 - Preserve Authored Ingredient Child Positions

- Stopped `LevelIngredientDisplayManager.ConfigureSlot` from reapplying the default plate surface position to an existing matching Ingredient child.
- Existing manually positioned ingredient visuals now retain their authored local coordinates; only newly created or prefab-swapped visuals receive automatic placement.
- Runtime and Editor C# projects compile with zero warnings and zero errors.

## 2026-10-27 - Preserve Authored Ingredient Slot Coordinates

- `LevelIngredientDisplayManager` now preserves the Scene-authored position and rotation of existing Ingredient Slot objects when a level starts.
- Only newly created runtime slots receive the fallback table layout, so manually placed plate ingredients no longer jump to the old calculated coordinates.
- Runtime and Editor C# projects compile with zero warnings and zero errors.

## 2026-10-27 - Horizontal Mortar Grinding判定

- Kept ingredient drag/drop using the full Mortar Area collider, but changed pestle grinding validation to use the collider's horizontal X/Z footprint only.
- This allows a manually raised pestle to grind over the stone without being rejected because its authored Y position is above the collider volume.
- Runtime and Editor C# projects compile with zero warnings and zero errors.

## 2026-10-27 - Manual Mortar and Pestle Placement

- Hid the visible Mortar Area mesh while preserving its Capsule Collider and `MortarArea` script for grinding-hit detection.
- Applied the user's authored scene placement for `GrindingPestle` (`0.5478, 1.544, -0.0141`) and `Portion Cluster` (`-0.24, -1.35, 0`), with automatic startup alignment explicitly disabled.

## 2026-10-27 - Preserve Authored Pestle Position and Lower Powder Visuals

- Disabled automatic pestle alignment at level start so the Unity-authored pestle position remains under the user's direct control.
- Lowered in-mortar powder visual anchoring slightly below the mortar collider top, matching the visible stone surface and preventing the powder from appearing too high beneath the pestle.
- Runtime and Editor C# projects compile with zero warnings and zero errors.

## 2026-10-27 - Pestle Tip Raised Onto Grinding Surface

- Corrected the startup alignment of the pestle so its visible grinding tip aligns to the top of the mortar collider/stone surface instead of the collider center.
- This removes the visible pestle-tip clipping through the grinding stone while preserving the mortar position and all gameplay behavior.
- Runtime and Editor C# projects compile with zero warnings and zero errors.

## 2026-10-27 - Prepared Powder Final Fusion Flow

- Separate-batch levels now treat dragging all completed powder batches back into the mortar as the final fusion step, so the player can Evaluate immediately without grinding the combined powders a second time.
- Evaluate now shows a blue Hint Panel message when only some prepared powders have been returned, explaining that all powders must be placed together before evaluating and that no additional grinding is needed.
- Returned prepared powders retain their previously selected ratios instead of reopening ratio selection.
- Runtime and Editor C# projects compile with zero warnings and zero errors.

## 2026-10-27 - Per-Page Story Text Presentation

- Added `The Taste Reviver > Story Page Visual Editor` so story text can be selected and adjusted directly in the Scene view, then saved back to the selected page.
- The editor now previews the selected page's Resources image and story text in Scene view before editing.
- Added independent Story Intro text controls to every story page: size, position, font size, color, and alignment.
- Added a per-page text rotation angle for a more hand-drawn, storybook-like layout.
- Each page can opt into its own presentation from the `StoryIntroPageData` Inspector while pages that do not opt in keep the prefab-authored defaults.
- CSV re-import preserves these per-page presentation overrides by matching existing pages by `pageKey`.
- Applied the same settings to the prefab-backed Story Intro view and the legacy runtime fallback.
- Runtime and Editor C# projects compile with zero warnings and zero errors.

## 2026-10-27 - Dancing Script Font Replacement

- Imported the user-provided `DancingScript-VariableFont_wght.ttf` into `Assets/Resources/Fonts`.
- Story Intro text uses the imported Dancing Script font without changing text content, layout, sizing, or interaction behavior.
- Experiment Log now applies the font to all loaded TMP text objects in its additive scene, including scene-owned text outside the controller hierarchy.
- Runtime and Editor C# projects compile with zero warnings and zero errors.

## 2026-10-27 - Modern Farmhouse Gameplay Font

- Imported `ModernFarmhouse.otf` into `Assets/Resources/Fonts`.
- Gameplay UI and Experiment Log use Modern Farmhouse, while per-level Story Intro text remains on Dancing Script.
- Runtime and Editor C# projects compile with zero warnings and zero errors.

## 2026-10-27 - Pestle Tip Mortar Alignment

- Aligns the visible pestle mesh's lowest point to the mortar center at level start, preventing the pestle head from clipping through the mortar rim while preserving its authored rotation and gameplay motion.
- Runtime and Editor C# projects compile with zero warnings and zero errors.

## 2026-10-27 - Experiment Log Record Ordering and Formatting

- Removed duplicated level prefixes such as `L01 L01 Rice Flour` from ingredient note headings.
- Correct experiment notes now appear before incorrect notes, with `(Correct)` or `(Incorrect)` markers.
- Simplified note details to direct order sequences, ingredient ratio values, force/speed values, and `Together:` combination descriptions; fixed Force/Speed dimensions are also unlocked as permanent clues.
- Runtime and Editor C# projects compile with zero warnings and zero errors.

## 2026-10-27 - Image-Only Cover and Ending

- Cover and `The End` story pages now clear their Intro Text, leaving the supplied artwork unobstructed.
- Continue/Close controls remain available so the player can advance or dismiss those image-only pages.
- Runtime and Editor C# projects compile with zero warnings and zero errors.

## 2026-10-27 - Full-Screen 16:9 Story Images

- Updated the Story Intro image frame and image to stretch across the full story interface, matching the supplied 16:9 artwork instead of the old centered square presentation.
- Applied the same full-screen behavior to the runtime-generated legacy fallback while keeping story text and controls functional.
- Runtime and Editor C# projects compile with zero warnings and zero errors.

## 2026-10-27 - TeaLeaf Prefab Stale Nested Reference

- Removed the invalid nested Prefab instance from `PF_TeaLeaf.prefab` that referenced missing GUID `df8887e...` under the inactive legacy visual branch.
- Preserved the valid TeaLeaf prefab structure and After Grinding nested prefab; the remaining FBX tangent warning is non-blocking.

## 2026-10-27 - Story Resource Folder Cleanup

- Renamed the remaining legacy opening resource to `Begin.jpg` and updated the L01 source/generated references.
- Removed the obsolete `Opening_Guangdong_Map.jpg` and its `.meta`; the story resource folder now contains only `Cover`, `Begin`, `L01Start`–`L12Start` equivalents, and `End` resources.

## 2026-10-27 - Storybook Transition Image Replacement

- Replaced the existing story transition images with the user-named `Begin`, `L1Start`–`L12Start`, `Cover`, and `End` artwork under `Assets/Resources/Story`.
- Added the opening cover page, L11 and L12 story descriptions/pages, and a final `The End` page after the last level's Next Level action.
- Gameplay rules, level mechanics, and gameplay UI layout remain unchanged.
- Runtime and Editor C# projects compile with zero warnings and zero errors.

## 2026-10-26 - Avoid Duplicate Single-Ingredient Powder Displays

- Mixed Powder slots are now created only for batches containing multiple ingredients.
- Separate single-ingredient levels such as L08 no longer create hidden duplicate `Mixed Powder 1/2/3` labels behind the mortar while their powders are already shown on the ingredient plates.
- Runtime and Editor C# projects compile with zero warnings and zero errors.

## 2026-10-26 - Experiment Log Plain Handwritten Presentation

- Removed square-bracket status and section markers from Experiment Log presentation.
- Notes now read as plain handwritten-style lines such as `Force: Medium` and `Final Blend: Peanut + Black Sesame powders combined`, while retaining the underlying record data and fixed-dimension behavior.
- Runtime and Editor C# projects compile with zero warnings and zero errors.

## 2026-10-26 - Experiment Log Fixed Process Dimensions

- Experiment Notes now retain authored fixed Force and Speed values even when those dimensions are locked for the current level.
- Fixed process values are shown as `FIXED`, separate from evaluated `CORRECT`/`INCORRECT` results, so later levels can reuse them through the Experiment Log.
- Runtime and Editor C# projects compile with zero warnings and zero errors.

## 2026-10-26 - Experiment Log Final-Blend Guidance

- Split final-blend recipe notes into preparation-batch correctness and final-combination guidance, so a correctly separated ingredient is no longer mislabeled as an incorrect batch when only the final blend is missing.
- Ingredient details now explicitly tell the player to combine the prepared powders when all required preparation batches are complete, and show the completed final blend separately after success.
- Kept the alternate L05 two-plus-one preparation rule distinct from L04's separate single-ingredient batches.
- Runtime and Editor C# projects compile with zero warnings and zero errors.

## 2026-10-18 - Imported Ingredient Material Overrides Saved

- Reapplied each imported Before Grinding model's authored ingredient material to every child Renderer and saved all 12 migrated ingredient prefabs.
- Kept the five ingredients without imported Before Grinding FBX visuals on their existing authored fallback visuals.
- Removed the one-shot `IngredientVisualPrefabMigration` editor utility after the prefab assets were saved; the remaining editor tools are unchanged.

## 2026-10-18 - Sequential Multi-Dimension Hint Flow

- Hint priority now locks onto the first unresolved enabled dimension instead of falling through to later dimensions when that dimension's progressive hints are exhausted.
- Players must resolve the current dimension through evaluation before hints advance to the next dimension; missing-ingredient guidance and the existing combination-before-process safeguard remain unchanged.
- Added clear hold messages for order, ratio, batch structure, speed, and force so the next action remains understandable without revealing every later answer at once.
- Runtime and Editor C# projects compile with zero warnings and zero errors.

## 2026-10-18 - Ingredient Traits Cross-Level Alignment

- Audited Ingredient Traits against the L01-L12 level mechanics, target values, combination groups, order, and progressive hints.
- Fixed Traits so ingredient-specific Force/Speed targets are shown only on the profiles that actually evaluate those dimensions; whole-level Force/Speed targets remain visible when no ingredient-specific target exists.
- This removes misleading default Medium clues from unrelated ingredients in L07, L11, and L12 while preserving the correct Heavy/Slow whole-level guidance in L10.
- Runtime and Editor C# projects compile with zero warnings and zero errors.

## 2026-10-18 - Imported Before-Grinding Ingredient Models

- Replaced primitive `Before Grinding` visuals with matching raw `lod_basic_pbr.fbx` model instances for 12 ingredients, while preserving the authored Rice visual.
- Kept the four ingredients without matching FBX assets (Chili, Coarse Salt, Lotus Leaf, and Scallion Stem) on their existing placeholder visuals.
- Moved the old primitive visuals under inactive `Legacy Primitive Visual` children for safe rollback; imported models are normalized to the previous visual footprint.
- Runtime and Editor C# projects compile with zero warnings and zero errors.

## 2026-10-18 - Safe Mixed-Powder Visual Fallback

- Fixed the L09 Wrapped Rice Layers crash that occurred when all ingredients, including Lotus Leaf, were incorrectly ground together and evaluated.
- Runtime mixed-powder and returned-ground displays prefer the ingredient's main prefab and its authored `IngredientVisualState`, while retaining a null-safe legacy `IngredientData.groundPrefab` fallback for earlier ingredients that have not yet been migrated.
- Ingredients without either a separate after-grinding child or a legacy ground prefab, including Lotus Leaf, Coarse Salt, and Scallion Stem, safely retain their authored before-grinding display as the result fallback instead of throwing `UnassignedReferenceException`.
- Checked the same path for the late multi-ingredient levels L10-L12; missing `groundPrefab` references are handled without dereferencing null data.
- Runtime and Editor C# projects compile with zero warnings and zero errors.

## 2026-10-18 - Gameplay HUD Prefab Ownership

- Removed the legacy HUD layout records, text-inset controls, and normalization path from `UIManager`; gameplay HUD position, size, typography, and base styling now come exclusively from `GameplayHud.prefab`.
- `GameplayHudView` now owns the Experiment Log button's dynamic label, attention tint, and pulse state. `UIManager` only requests the state change.
- `ForceSliderController` no longer restyles or resizes the force label when a prefab-authored gameplay HUD is present. Its local visual settings remain only for the programmatically generated legacy test fallback.
- Removed `TestLevelInitializer` calls that asked `UIManager` to normalize authored HUD layout.
- Verified both runtime and Editor C# projects compile with zero warnings and zero errors.

## 2026-10-18 - Context-Aware Hint Coverage

- Audited hint complexity across L01-L12: early levels evaluate one dimension, while L11 and L12 evaluate five dimensions and therefore have 31 possible non-empty correct/incorrect dimension combinations before ingredient-specific states are counted.
- Powder-ready and post-`New Batch` guidance now counts remaining required ingredients rather than comparing batch count with ingredient count, which fixes misleading late-level prompts for grouped batches in L09-L12.
- Shared workflow guidance now distinguishes a missing ingredient from a correctly completed batch, detects when a same-batch ingredient was omitted before grinding, names the next ingredient or group, announces when final prepared powders should be recombined, and tells the player when the recipe is ready to Evaluate.
- Incorrect evaluation now prioritizes an explicit missing-ingredient message before discussing ratio, order, combination, force, or speed.
- Pressing `Evaluate` before all required ingredients are present now gives contextual next-step guidance without consuming the attempt or falling through to the misleading powdering-time message.
- Final-combination guidance counts the current powder batch as prepared for next-step instructions, while final-blend validation still requires preparation batches to precede the final combined batch.

## 2026-10-18 - L05 Premature Final Blend Guidance

- L05 now treats grinding all three raw spices together as an incorrect mixed-powder batch rather than mistaking it for the final combination of prepared powders.
- The final combined batch is recognized only after an earlier exact pair batch and the remaining single-ingredient batch have both been prepared.
- An early all-three batch displays as one mixed powder pile and gives an explicit `Reset Attempt` message explaining the required `two together + one separately` preparation.
- Starting an empty next batch no longer causes Evaluate to show the misleading `Keep grinding` message; completed prior batches can proceed to normal recipe evaluation feedback.

## 2026-10-18 - Ingredient Prefab Visual States

- Ingredient prefab instances are the source of truth for ingredient model size; runtime gameplay code positions them but does not normalize or overwrite their authored scale.
- Each migrated ingredient prefab contains editable `Before Grinding` and `After Grinding` children managed by `IngredientVisualState`.
- Existing `After Grinding` visuals use an authored uniform parent scale of `0.4 / 0.4 / 0.4`; adjust individual prefab children only when a specific model genuinely needs a different visual treatment.
- Removed legacy secondary wrapper scaling from Ground Rock Sugar, Ground Chili, Ground Yangjiang Douchi, and Ground Glutinous Rice so the uniform `After Grinding` scale is not multiplied by an older normalization value.
- Tabletop ingredients, in-mortar ground visuals, returned ground ingredients, and mixed-powder plate displays all reuse those same prefab-authored visual states.
- `IngredientData.groundPrefab` remains hidden only as a legacy migration/fallback source; new visual tuning belongs on the two child Transforms in the main ingredient prefab.

## 2026-10-18 - Powdering Time Classification and Hint Alignment

- Reclassified grinding duration as `PowderingSeconds`: a short interaction progress gate that controls when a batch becomes powder, not a scored recipe answer.
- Removed the obsolete `GrindDuration` entries and misleading time ranges from the English design tables and ingredient profiles.
- Early evaluation guidance now asks the player to keep grinding until the ingredients become powder instead of presenting a countdown as an answer requirement.
- Experiment Log presentation no longer lists grinding time alongside evaluated recipe dimensions.
- Corrected profile-driven Force and Speed hints so levels with per-ingredient process targets, especially L08, use the current ingredient's actual answer instead of the level-wide fallback.
- Added Ginger to the L05 fallback hint associations and removed misleading grinding-time language from L01-L03 feedback.

## 2026-10-18 - Prefab-Authored UI Visuals

- Prefab instances are now the source of truth for gameplay UI layout and appearance. Runtime code may update content, visibility, interaction state, sibling order, and genuinely dynamic geometry, but must not restyle authored prefab visuals.
- Enlarged the prefab-authored Current Ratio Panel from height `160.18` to `220.18` so four-ingredient recipes can retain a readable TMP auto-size, and moved the Current Speed Panel down by `60` to preserve the authored gap without overlapping the Force controls.
- `UIManager` skips legacy panel creation, normalization, and named-panel styling whenever prefab views are present; `TestLevelInitializer` follows the same boundary.
- Story Intro runtime presentation no longer overwrites the prefab Backdrop color.
- Re-running the gameplay UI migration only applies default layout and appearance to newly created objects or newly added visual components, preserving existing Story Intro and Mortar Reaction prefab edits.
- Force slider colors are only driven from code when `driveSliderStyleFromInspector` is enabled.

## 2026-10-18 - Scallion Stem Terminology

- Standardized the player-facing scallion ingredient as `Scallion Stem` across English source tables, generated assets, hints, Traits, feedback, and project documentation.
- Kept the internal ingredient ID `ScallionWhite` and existing asset/model names unchanged so serialized level references remain stable.

## 2026-10-18 - Gameplay UI Prefabs Generated

- Ran `Tools > The Taste Reviver > Migrate Gameplay UI To Prefabs` in the unlocked Unity Editor.
- Generated and saved seven independent panel prefabs under `Assets/Prefabs/UI`: Gameplay HUD, ratio selection, ingredient traits, ingredient tooltip, opening title, mortar reaction, and story intro.
- Reconnected and saved `TasteRestorerTestLevel.unity` so its seven UI roots are prefab instances; the Hint Panel is owned by `GameplayHud.prefab`.
- Corrected the migration tool so `UIManager` stores scene-instance component references rather than persistent prefab-asset references, and added runtime recovery from stale asset references.
- Re-ran the migration and verified Play Mode with an empty Console; the prefab Transform mutation error no longer occurs. The runtime and Editor C# projects build with zero warnings and zero errors.

## 2026-10-17 - Gameplay UI Prefab Architecture

- Began replacing the monolithic gameplay `UIManager` with prefab-owned view components. `UIManager` now exposes only seven high-level view references plus gameplay coordination dependencies in its normal Inspector surface.
- Added dedicated components for the gameplay HUD, ratio selection, ingredient traits, ingredient tooltip, opening title, mortar reaction, and story intro. Each component owns its child references and local presentation behavior.
- Added `Tools > The Taste Reviver > Migrate Gameplay UI To Prefabs`, which groups the existing scene UI without changing its authored layout, creates independent prefabs under `Assets/Prefabs/UI`, reconnects the gameplay scene, and keeps a legacy fallback for programmatically generated test canvases.
- The runtime and Editor C# projects compile with zero warnings and zero errors. The prefab migration was subsequently completed in an unlocked Unity Editor session on 2026-10-18.

## 2026-10-03 - Experiment Log Mid-Recipe State Preservation

- Changed the Experiment Log to open additively over the gameplay scene instead of replacing and later reloading it.
- The Experiment Log leaves gameplay roots active and temporarily disables only gameplay cameras, audio listeners, and UI raycasters. This avoids `OnEnable` rebuilding ingredient displays while preserving the current batch, ingredient positions and ground visuals, ratios, force/speed state, and grinding progress.
- Reordered Experiment Log content so permanent `Unlocked Clues` appear first and success/failure `Experiment Notes` appear later in the ingredient detail view and full-log text.
- Permanent clues remain progression-gated per clue ID: completing the current level correctly unlocks all of that level's not-yet-earned clues, while later clues for a repeated ingredient stay hidden until their own level is completed.
- The ingredient hover tooltip is suppressed for the entire time the expanded Traits overlay is open, so it cannot render through or above the enlarged Traits panel.
- The additive Experiment Log reuses the gameplay EventSystem and its canvas renders at an explicit higher sorting order, preventing duplicate EventSystem warnings and input leaking to the gameplay UI.
- The Experiment Log creates an opaque full-screen cover below its own content, hiding all gameplay HUD buttons while keeping the live gameplay objects untouched behind it.
- Kept the existing standalone Experiment Log fallback so opening that scene directly can still return to the test level.
- Verified the Unity C# solution builds successfully with zero warnings and zero errors using a separate output directory because Unity had locked the default build assembly.

## Active Collaboration State (2026-09-24)

- Reply to the user in Chinese unless they request another language.
- End every user-facing response, including progress updates, with a sparkle (`✨`).
- Recipe progression is being reviewed one level at a time. Discuss and confirm a level before changing its design data.
- The systematic recipe/knowledge-chain review is complete through **L12**, including data validation for L06-L12.
- L12 is the final memory-mastery level. It reuses only existing ingredients and mechanics rather than introducing a new ingredient or interaction.
- Preserve the existing 12-level structure and existing ingredient models unless the user explicitly chooses a larger redesign.

## Recipe Design Direction

- The user's goal is to make later recipes meaningfully reuse knowledge learned in earlier levels, especially through repeated ingredients and the Experiment Log.
- Prefer stable, transferable ingredient rules over arbitrary absolute positions. Examples: Dried Peel follows the main flavor and stays restrained; Coarse Salt is a very small supporting amount and generally comes last; Black Sesame should remain distinct and not be overworked with every ingredient; Lotus Leaf is a final wrapper rather than a powder ingredient.
- Later levels should combine familiar rules with one new idea so the player can infer answers from earlier Experiment Log notes instead of guessing.
- Keep hints progressive: first point toward relevant prior knowledge or the currently incorrect dimension, then become more explicit after failure. Avoid giving every answer immediately in the intro.
- Player-facing ratio vocabulary is strictly `Very Small`, `Small`, `Medium`, and `Large`. Internal enum/source values remain `VeryLess`, `Less`, `SlightlyMore`, and `More` respectively; do not expose those internal names to players.
- The game is currently English-facing. Keep runtime UI, Traits, hints, feedback, and ingredient display strings in English to avoid mixed-language UI and TMP missing-glyph warnings.

## Current Level Checkpoint

### L06 - Soft Sesame Paste (Completed)

- Ingredients: Black Sesame and Glutinous Rice.
- Enabled evaluated mechanics: Ratio and Force. Ingredient order, combination grouping, and speed are not evaluated.
- Final player-facing answers:
  - Black Sesame: `Medium` (internal `SlightlyMore`).
  - Glutinous Rice: `Small` (internal `Less`).
  - Whole recipe: `Medium Force`.
- The level transfers the steady Medium Force lesson from L01 Rice to Glutinous Rice and reuses the strong/bitter Black Sesame identity from L04.
- Text communicates that Black Sesame leads without becoming bitter, Glutinous Rice softens/thickens the paste, and a familiar grain lesson may help.
- Ratio and Force hints, Traits text, Experiment Log permanent clues, feedback, design tables, and `L06_Soft_Sesame_Paste.asset` are synchronized.
- Do not re-add order or batch-combination requirements to L06 without a new discussion; the accepted plan intentionally keeps its interaction simple.

### L07 - Sand Ginger Core (Completed)

- Ingredients: Sand Ginger, Dried Tangerine Peel, and Coarse Salt.
- Enabled evaluated mechanics: Ratio and Force. Ingredient order, combination grouping, and speed are not evaluated.
- Final player-facing answers:
  - Sand Ginger: `Large` (internal `More`).
  - Dried Tangerine Peel: `Small` (internal `Less`).
  - Coarse Salt: `Very Small` (internal `VeryLess`).
  - Whole recipe: `Heavy Force`.
- L07 reuses the restrained Dried Peel lesson from L03, introduces Heavy Force for dry Sand Ginger, and establishes Coarse Salt as a Very Small supporting flavor for reuse in L10 and L11.
- Intro text hints at flavor roles without immediately spelling out all four answers. Progressive hints give the exact ratio or force only when needed.
- Ratio and Force hints, Traits text, the Coarse Salt and Sand Ginger Experiment Log clues, feedback, design tables, and `L07_Sand_Ginger_Core.asset` are synchronized.
- The malformed comma in the L07 fallback hint CSV row was corrected so the full fallback stays in `HintText` instead of spilling into `ClueTitle`.

### L08 - Pepper Held in Balance (Completed Text Pass)

- Correct conditions were intentionally unchanged: three separate ingredient batches with White Pepper=`Light/Medium`, Peanut=`Heavy/Slow`, and Dried Tangerine Peel=`Medium/Fast` for Force/Speed.
- Rewrote the existing intro, feedback, hints, Traits, and White Pepper Experiment Log clue to reference earlier spice, nut, and citrus lessons without adding rules or hint rows.

### L09 - Wrapped Rice Layers (Completed Text Pass)

- Correct conditions were unchanged: Red Bean → Glutinous Rice → Black Sesame in one filling batch, followed by Lotus Leaf as the final wrapping step; ratios remain `Small / Large / Medium / Very Small` by ingredient.
- Rewrote all player-facing text to distinguish sequential additions within one batch from simultaneous addition, describe Lotus Leaf as a wrapper rather than powder, and clarify that the brief pestle movement completes the game wrapping step.

### L10 - Savory Bean Current (Completed Text Pass)

- Correct conditions were unchanged: Yangjiang Douchi → Ginger → Dried Tangerine Peel → Coarse Salt; ratios=`Large / Medium / Small / Very Small`; whole-recipe process=`Heavy Force / Slow Speed`.
- Rewrote all player-facing text to connect Ginger/Dried Peel to L05 and Coarse Salt to L07 while treating Yangjiang Douchi as the new savory base.

### L11 - Ginger-Scallion Finale (Completed Order Retune and Text Pass)

- Final order is Sand Ginger → Ginger → Scallion Stem → Coarse Salt.
- Sand Ginger and Ginger share the first batch; Scallion Stem and Coarse Salt each use separate batches.
- Ratios remain Sand Ginger=`Large`, Ginger=`Medium`, Scallion Stem=`Small`, Coarse Salt=`Very Small`.
- The ginger batch retains Heavy Force and Medium Speed. Combination, ratio, score, and duration settings remain otherwise unchanged.
- Rewrote intro, feedback, hints, Traits, and permanent clues so L11 functions as a synthesis of earlier Sand Ginger, Ginger, and Coarse Salt knowledge while introducing Scallion Stem as the fresh separate layer.

### L12 - Douchi-Scallion Mastery (Added)

- Ingredients: Yangjiang Douchi, Ginger, Scallion Stem, and Coarse Salt; all reuse existing ingredient assets.
- Final order is Yangjiang Douchi → Ginger → Scallion Stem → Coarse Salt.
- Yangjiang Douchi and Ginger share the first batch; Scallion Stem and Coarse Salt each use separate batches.
- Ratios are Yangjiang Douchi=`Large`, Ginger=`Medium`, Scallion Stem=`Small`, and Coarse Salt=`Very Small`.
- The douchi-and-ginger batch uses Heavy Force and Slow Speed. Passing score is 90 and the accepted duration is 32-50 seconds.
- L12 adds no new mechanic, model, or permanent Experiment Log clue; it is a memory-mastery test built from rules already taught in L07, L10, and L11.

## Data Authoring and Verification Rules

- English level intros, mechanics, ratios, and result feedback originate in `Assets/Data/DesignTables/English/levels_en.csv`.
- Progressive hints and Experiment Log permanent clues originate in `Assets/Data/DesignTables/English/hints_en.csv`.
- Ingredient-specific Traits/profile text originates in `Assets/Data/DesignTables/English/LevelProfiles/L##_profiles_en.csv`.
- Unity runtime reads the generated assets under `Assets/Data/GeneratedAssets/Levels/`. When making an accepted recipe change, update the source tables and regenerate or synchronize the generated level asset so the change survives future imports.
- Check player-facing terminology across all three sources, not only the generated asset.
- After C# or importer changes, rebuild the Unity C# solution and report whether it compiles. When only recipe data changes, validate the CSV rows and corresponding generated asset fields.

## 2026-09-24 - L07-L12 Knowledge-Chain Review

- Retuned L07 to Sand Ginger=`Large`, Dried Tangerine Peel=`Small`, Coarse Salt=`Very Small`, and whole-recipe Heavy Force.
- Kept L07 focused on Ratio and Force without adding order, combination, or speed evaluation.
- Rewrote the intro and feedback so Sand Ginger is the bold base, Dried Peel is a familiar restrained accent, and Coarse Salt is a minimal supporting flavor.
- Added synchronized progressive Ratio and Force hints, updated Traits descriptions, and updated permanent Experiment Log clues for Sand Ginger Force and Coarse Salt Ratio.
- Corrected the malformed L07 fallback CSV row so its comma no longer shifts text into the wrong column.
- Synchronized `levels_en.csv`, `hints_en.csv`, `L07_profiles_en.csv`, and `L07_Sand_Ginger_Core.asset`.
- Completed native-English text passes for L08, L09, and L10 without changing their correct conditions or adding mechanics.
- Retuned L11 order from Coarse Salt first to Sand Ginger → Ginger → Scallion Stem → Coarse Salt so the final level respects the earlier rule that Very Small Coarse Salt finishes the recipe.
- Updated L11 order-dependent profiles, hints, Traits, feedback, source tables, and generated asset while preserving its existing ratios, batch groups, Force/Speed targets, score, and duration.
- Added L12 Douchi-Scallion Mastery using only Yangjiang Douchi, Ginger, Scallion Stem, and Coarse Salt, with synchronized source tables, hints, profiles, generated asset, and scene level lists.
- Quoted the comma-containing L01-L06 fallback hint fields and synchronized their generated level assets so every English design-table CSV row remains column-safe on reimport; wording and gameplay conditions were unchanged.

## 2026-09-24 - Experiment Log Guidance

- Added contextual Experiment Log reminders at level start when saved clues match ingredients in the current recipe.
- Added an Experiment Log reminder after every incorrect evaluation; when relevant older clues exist, the reminder names the ingredients whose notes should be reviewed.
- The Experiment Log button now changes to `Review Log` or `Log: New Clue`, gains a warm highlight, and pulses briefly when the player should check it.
- Returning from the Experiment Log now confirms that it was reviewed and encourages the player to apply one saved note at a time.
- Correct recipes with newly unlocked clues explicitly ask the player to open the Experiment Log before continuing.
- Added guidance inside the Experiment Log explaining that ingredient entries can be selected to compare saved clues with earlier attempts.
- Standardized the runtime ratio picker to always offer `Very Small`, `Small`, `Medium`, and `Large` on ratio-enabled levels. The old ingredient-count filter made L06's `Medium` and L07's `Very Small` targets impossible to select.
- Increased the opening level-title display time to 5 real-time seconds so it is easier to finish reading and cannot be shortened by game time scaling. L07's separate `Tutorial Complete` card now remains for at least 2.5 real-time seconds.
- Changed Experiment Log ingredient discovery so any ingredient used in an evaluated attempt is unlocked immediately, even when the attempt is incorrect. Attempt notes display only the actual checked results as `CORRECT` or `INCORRECT`; permanent clues remain restricted to correct level completion.

## 2026-09-24 - Mortar Reaction Visual Prototype

- Added a separate `Mortar Reaction` runtime overlay above the mortar rather than reusing the Hint Panel.
- Added a transparent, hand-painted deep tea-brown smoke-ribbon background at `Assets/Resources/UI/MortarReactionBackground.png`, generated to contrast with the pale wooden parchment Hint Panel.
- Mortar reactions fade in and out using real time, never block pointer input, and use short two-line sensory messages such as `AROMA WARNING / The tea leaves are warming too quickly.`
- Added initial real-time reactions for sustained incorrect Speed and Force states. The overlay describes ingredient behavior without naming the correct control setting; the Hint Panel remains responsible for checked mechanics, strategic hints, evaluation results, and Experiment Log guidance.
- Corrected `RecipeAttemptManager.GetCurrentBatch()` to return the active/latest batch so per-batch reactions and records use the ingredient currently being ground.

## 2026-09-23 - L06 Knowledge Chain, Experiment Log, TMP, and UI Polish

- Reviewed the relationship among all 11 levels and established a knowledge-chain direction based on repeated ingredients and transferable Experiment Log clues.
- Completed L06 plan A: Black Sesame=`Medium`, Glutinous Rice=`Small`, and whole-recipe Medium Force, with synchronized text, hints, Traits, clues, scoring data, and generated asset.
- Standardized all player-visible ratio wording to `Very Small`, `Small`, `Medium`, and `Large`, while retaining the existing internal enum names.
- Improved the Experiment Log flow and migrated UI text to TextMesh Pro.
- Removed remaining player-visible Chinese strings from the English runtime presentation and prevented ingredient hover tooltips from duplicating English/Chinese names.
- Kept tabletop ingredient labels hidden because hover tooltips already provide the ingredient name and description.
- Aligned the upper-right hint text and expanded Ingredient Traits content with consistent insets, wrapping, and scroll behavior.
- Fixed ingredient dragging near the mortar rim by calculating a safe drag height from the model bounds and mortar height.
- Added automatic cleanup for empty runtime/imported LOD groups and cleaned redundant serialized LOD objects without deleting model resources.
- Commits pushed to `main`: `eddb94f Migrate UI text to TMP and improve experiment log`, `367e77b Update TMP fallback font serialization`, and `78e787b Polish gameplay UI and clean redundant LODs`.

## 2026-09-09 - UI, Hint, L09 Zongzi, and L10 Tuning

- Added larger, centered expanded Traits overlay behavior in `UIManager`, with a clickable dark backdrop that collapses the panel back to the Traits button.
- Moved the collapsed Traits button lower so it does not overlap the opening title prompt.
- Added Inspector-adjustable UI text inset settings for HUD panels, buttons, and the expanded Traits overlay so text can be pulled inward from the panel artwork.
- Adjusted opening title and button text frames to sit inside the painted panel area.
- Updated `HintManager` so hints prefer the player's current incorrect dimension state before falling back to fixed progressive hints.
- Improved order hints so they no longer repeat "start with the first ingredient" when the first ingredient is already correct.
- Changed non-combination process hints to refer to the whole recipe instead of the current batch.
- Retuned L10 so Force and Speed are whole-recipe checks instead of ingredient-specific checks.
- Changed L10 target process settings from default Medium Force / Medium Speed to Heavy Force / Slow Speed.
- Updated L10 design tables and generated level asset so the new Force/Speed answer and hints persist after regeneration.
- Added L09 zongzi final-crafting flow: after the main filling batch is prepared, adding Lotus Leaf and moving the pestle briefly creates the final zongzi instead of grinding Lotus Leaf into powder.
- Added Inspector-adjustable final crafted object settings to `RecipeAttemptManager`, including target level ID, wrapper ingredient ID, Resources prefab path, texture paths, motion time, position offset, rotation, and scale.
- Imported the zongzi model and textures into `Assets/Resources/Zhongzi/`.
- Added runtime material assignment for the zongzi final model using diffuse, normal, metallic, and roughness textures.
- Increased the default zongzi final object scale so it is visible in the mortar.
- Updated L09 text, hints, success, close, and wrong feedback to describe wrapping the prepared filling into zongzi rather than grinding Lotus Leaf into powder.
- Verified the Unity C# project rebuild successfully after the changes.
- Committed and pushed the implementation to GitHub as `01fec40 Add zongzi crafting and tune late-level hints`.


