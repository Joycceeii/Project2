# Agent Change Log

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

