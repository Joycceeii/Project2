using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TheTasteReviver
{
    public class LevelIngredientDisplayManager : MonoBehaviour
    {
        private const string ContainerName = "Ingredient Slots";
        private const int MaxSlots = 4;

        public MortarArea mortarArea;
        public RecipeAttemptManager attemptManager;
        public bool hideLegacyDisplaysOnFirstRefresh = true;

        private bool legacyDisplaysHandled;

        public void ShowLevelIngredients(RecipeLevelData level)
        {
            List<GameObject> slots = EnsureSlots();
            HideLegacyIngredientDisplaysOnce();
            SetAllSlotsInactive(slots);

            if (level == null)
            {
                return;
            }

            List<IngredientData> availableIngredients = level.availableIngredients
                .Where(x => x != null)
                .Take(MaxSlots)
                .ToList();

            for (int i = 0; i < availableIngredients.Count; i++)
            {
                ConfigureSlot(slots[i], availableIngredients[i], i, availableIngredients.Count);
            }
        }

        private void HideLegacyIngredientDisplaysOnce()
        {
            if (legacyDisplaysHandled || !hideLegacyDisplaysOnFirstRefresh)
            {
                return;
            }

            legacyDisplaysHandled = true;
            HideLegacyIngredientDisplays();
        }

        private List<GameObject> EnsureSlots()
        {
            Transform container = transform.Find(ContainerName);
            if (!IsAlive(container))
            {
                container = new GameObject(ContainerName).transform;
                container.SetParent(transform, false);
            }

            List<GameObject> slots = new List<GameObject>();
            for (int i = 0; i < MaxSlots; i++)
            {
                string slotName = "Ingredient Slot " + (i + 1);
                Transform slotTransform = FindSlot(container, slotName);
                if (!IsAlive(slotTransform))
                {
                    slotTransform = CreateSlot(slotName, container).transform;
                }
                else
                {
                    slotTransform.name = slotName;
                }

                slots.Add(slotTransform.gameObject);
            }

            return slots;
        }

        private GameObject CreateSlot(string slotName, Transform container)
        {
            GameObject slot = new GameObject(slotName);
            slot.transform.SetParent(container, false);

            GameObject plate = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            plate.name = "Plate";
            plate.transform.SetParent(slot.transform, false);
            plate.transform.localPosition = Vector3.zero;
            plate.transform.localScale = new Vector3(0.75f, 0.08f, 0.75f);
            ApplyColor(plate, Color.white);

            GameObject item = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            item.name = "Ingredient";
            item.transform.SetParent(slot.transform, false);
            item.transform.localPosition = Vector3.up * 0.45f;
            item.transform.localScale = Vector3.one * 0.38f;
            item.AddComponent<DraggableIngredient>();

            return slot;
        }

        private void ConfigureSlot(GameObject slot, IngredientData ingredient, int index, int activeCount)
        {
            if (!IsAlive(slot))
            {
                return;
            }

            slot.SetActive(true);
            slot.transform.position = GetSlotPosition(index, activeCount);
            slot.transform.rotation = Quaternion.identity;

            Transform plate = FindPlate(slot.transform);
            if (IsAlive(plate))
            {
                plate.gameObject.name = "Plate";
                ApplyColor(plate.gameObject, Color.white);
            }

            Transform item = FindIngredientItem(slot.transform);
            if (!IsAlive(item))
            {
                return;
            }

            item.gameObject.name = "Ingredient";
            item.localPosition = Vector3.up * 0.45f;
            item.localScale = Vector3.one * 0.38f;
            ApplyColor(item.gameObject, ingredient.ingredientColor);

            DraggableIngredient drag = item.GetComponent<DraggableIngredient>() ?? item.gameObject.AddComponent<DraggableIngredient>();
            drag.ingredientData = ingredient;
            drag.mortarArea = mortarArea;
            drag.attemptManager = attemptManager;
            drag.ResetHomePosition();
        }

        private void SetAllSlotsInactive(List<GameObject> slots)
        {
            foreach (GameObject slot in slots)
            {
                if (!IsAlive(slot))
                {
                    continue;
                }

                Transform item = FindIngredientItem(slot.transform);
                if (IsAlive(item))
                {
                    DraggableIngredient drag = item.GetComponent<DraggableIngredient>();
                    if (IsAlive(drag))
                    {
                        drag.ingredientData = null;
                    }
                }

                slot.SetActive(false);
            }
        }

        private static Transform FindSlot(Transform container, string slotName)
        {
            Transform exact = container.Find(slotName);
            if (IsAlive(exact))
            {
                return exact;
            }

            foreach (Transform child in container)
            {
                if (IsAlive(child) && child.name.StartsWith(slotName))
                {
                    return child;
                }
            }

            return null;
        }

        private static Transform FindPlate(Transform slot)
        {
            Transform exact = slot.Find("Plate");
            if (IsAlive(exact))
            {
                return exact;
            }

            foreach (Transform child in slot)
            {
                if (IsAlive(child) && child.name.EndsWith(" Plate"))
                {
                    return child;
                }
            }

            return null;
        }

        private static Transform FindIngredientItem(Transform slot)
        {
            Transform exact = slot.Find("Ingredient");
            if (IsAlive(exact))
            {
                return exact;
            }

            DraggableIngredient draggable = slot.GetComponentInChildren<DraggableIngredient>(true);
            return IsAlive(draggable) ? draggable.transform : null;
        }

        private void HideLegacyIngredientDisplays()
        {
            Transform slotContainer = transform.Find(ContainerName);
            foreach (DraggableIngredient ingredient in FindObjectsByType<DraggableIngredient>(FindObjectsSortMode.None))
            {
                if (IsAlive(ingredient) && !IsChildOf(ingredient.transform, slotContainer))
                {
                    ingredient.gameObject.SetActive(false);
                }
            }

            foreach (GameObject plate in FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            {
                if (IsAlive(plate) && IsLegacyIngredientDisplayName(plate.name) && !IsChildOf(plate.transform, slotContainer))
                {
                    plate.SetActive(false);
                }
            }
        }

        private static bool IsLegacyIngredientDisplayName(string objectName)
        {
            if (string.IsNullOrWhiteSpace(objectName))
            {
                return false;
            }

            return objectName.EndsWith(" Plate")
                || objectName.EndsWith(" Ingredient")
                || StartsWithNumberedPrefix(objectName, "Plate")
                || StartsWithNumberedPrefix(objectName, "Ingredient")
                || objectName == "Ingredient Plates";
        }

        private static bool StartsWithNumberedPrefix(string objectName, string prefix)
        {
            if (!objectName.StartsWith(prefix) || objectName.Length <= prefix.Length)
            {
                return false;
            }

            return char.IsDigit(objectName[prefix.Length]);
        }

        private static bool IsChildOf(Transform child, Transform parent)
        {
            return IsAlive(child) && IsAlive(parent) && (child == parent || child.IsChildOf(parent));
        }

        private static bool IsAlive(UnityEngine.Object obj)
        {
            return obj != null;
        }

        private static Vector3 GetSlotPosition(int index, int activeCount)
        {
            int safeCount = Mathf.Max(1, activeCount);
            float spacing = safeCount <= 3 ? 1.6f : 1.35f;
            float x = (index - (safeCount - 1) * 0.5f) * spacing;
            return new Vector3(x, 0.15f, -1.6f);
        }

        private static void ApplyColor(GameObject target, Color color)
        {
            Renderer renderer = target.GetComponent<Renderer>();
            if (!IsAlive(renderer))
            {
                return;
            }

            if (Application.isPlaying)
            {
                renderer.material.color = color;
                return;
            }

            renderer.sharedMaterial = CreatePlaceholderMaterial(color);
        }

        private static Material CreatePlaceholderMaterial(Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (!IsAlive(shader))
            {
                shader = Shader.Find("Standard");
            }

            Material material = new Material(shader);
            material.name = "IngredientSlot_" + ColorUtility.ToHtmlStringRGBA(color);
            material.color = color;
            return material;
        }
    }
}
