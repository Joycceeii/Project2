# Project Continuity and Agent Change Log

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

- Final order is Sand Ginger → Ginger → Scallion White → Coarse Salt.
- Sand Ginger and Ginger share the first batch; Scallion White and Coarse Salt each use separate batches.
- Ratios remain Sand Ginger=`Large`, Ginger=`Medium`, Scallion White=`Small`, Coarse Salt=`Very Small`.
- The ginger batch retains Heavy Force and Medium Speed. Combination, ratio, score, and duration settings remain otherwise unchanged.
- Rewrote intro, feedback, hints, Traits, and permanent clues so L11 functions as a synthesis of earlier Sand Ginger, Ginger, and Coarse Salt knowledge while introducing Scallion White as the fresh separate layer.

### L12 - Douchi-Scallion Mastery (Added)

- Ingredients: Yangjiang Douchi, Ginger, Scallion White, and Coarse Salt; all reuse existing ingredient assets.
- Final order is Yangjiang Douchi → Ginger → Scallion White → Coarse Salt.
- Yangjiang Douchi and Ginger share the first batch; Scallion White and Coarse Salt each use separate batches.
- Ratios are Yangjiang Douchi=`Large`, Ginger=`Medium`, Scallion White=`Small`, and Coarse Salt=`Very Small`.
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
- Retuned L11 order from Coarse Salt first to Sand Ginger → Ginger → Scallion White → Coarse Salt so the final level respects the earlier rule that Very Small Coarse Salt finishes the recipe.
- Updated L11 order-dependent profiles, hints, Traits, feedback, source tables, and generated asset while preserving its existing ratios, batch groups, Force/Speed targets, score, and duration.
- Added L12 Douchi-Scallion Mastery using only Yangjiang Douchi, Ginger, Scallion White, and Coarse Salt, with synchronized source tables, hints, profiles, generated asset, and scene level lists.
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
