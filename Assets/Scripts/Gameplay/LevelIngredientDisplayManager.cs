using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TheTasteReviver
{
    public class LevelIngredientDisplayManager : MonoBehaviour
    {
        private const string ContainerName = "Ingredient Slots";
        private const string LabelName = "Ingredient Label";
        private const int MaxSlots = 4;
        private const float LabelSurfaceOffset = 0.08f;
        private const float PlateLabelYOffset = 0.13f;
        private const float PlateIngredientSurfaceY = 0.16f;
        private const float IngredientPlateFootprint = 0.46f;
        private const float IngredientPlateMaxHeight = 0.28f;

        public MortarArea mortarArea;
        public RecipeAttemptManager attemptManager;
        public GameObject platePrefab;
        public bool hideLegacyDisplaysOnFirstRefresh = true;
        public bool showIngredientLabels = true;
        public Vector3 labelOffset = new Vector3(0f, PlateLabelYOffset, -0.18f);
        public int labelFontSize = 52;
        public float labelCharacterSize = 0.045f;

        private bool legacyDisplaysHandled;

#if UNITY_EDITOR
        private void OnValidate()
        {
            ResolvePlatePrefabInEditor();
        }
#endif

        public void ShowLevelIngredients(RecipeLevelData level)
        {
#if UNITY_EDITOR
            ResolvePlatePrefabInEditor();
#endif
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

            EnsurePlateVisual(slot.transform);

            GameObject item = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            item.name = "Ingredient";
            item.transform.SetParent(slot.transform, false);
            item.transform.localPosition = Vector3.up * PlateIngredientSurfaceY;
            item.transform.localScale = Vector3.one * 0.26f;
            item.AddComponent<DraggableIngredient>();

            EnsureLabel(slot.transform);

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

            EnsurePlateVisual(slot.transform);

            Transform item = EnsureIngredientItem(slot.transform, ingredient);
            if (!IsAlive(item))
            {
                return;
            }

            item.gameObject.name = "Ingredient";
            FitIngredientItemToPlate(item, ingredient.prefab != null);
            if (ingredient.prefab == null)
            {
                ApplyColor(item.gameObject, ingredient.ingredientColor);
            }

            DraggableIngredient drag = item.GetComponent<DraggableIngredient>() ?? item.gameObject.AddComponent<DraggableIngredient>();
            drag.ingredientData = ingredient;
            drag.mortarArea = mortarArea;
            drag.attemptManager = attemptManager;
            drag.sourcePrefab = ingredient.prefab;
            drag.ResetHomePosition();

            ConfigureLabel(slot.transform, ingredient);
        }

        private Transform EnsureIngredientItem(Transform slot, IngredientData ingredient)
        {
            Transform current = FindIngredientItem(slot);
            GameObject prefab = ingredient != null ? ingredient.prefab : null;
            DraggableIngredient currentDrag = IsAlive(current) ? current.GetComponent<DraggableIngredient>() : null;
            bool currentMatchesPrefab = prefab == null
                ? IsAlive(current) && (!IsAlive(currentDrag) || currentDrag.sourcePrefab == null)
                : IsAlive(currentDrag) && currentDrag.sourcePrefab == prefab;

            if (currentMatchesPrefab)
            {
                return current;
            }

            if (IsAlive(current))
            {
                DestroyObject(current.gameObject);
            }

            GameObject item;
            if (prefab != null)
            {
                item = Instantiate(prefab, slot, false);
                item.name = "Ingredient";
                DraggableIngredient drag = item.GetComponent<DraggableIngredient>() ?? item.AddComponent<DraggableIngredient>();
                drag.sourcePrefab = prefab;
            }
            else
            {
                item = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                item.name = "Ingredient";
                item.transform.SetParent(slot, false);
            }

            if (!IsAlive(item.GetComponent<Collider>()))
            {
                SphereCollider collider = item.AddComponent<SphereCollider>();
                collider.radius = 0.6f;
            }

            if (!IsAlive(item.GetComponent<DraggableIngredient>()))
            {
                item.AddComponent<DraggableIngredient>();
            }

            return item.transform;
        }

        private Transform EnsurePlateVisual(Transform slot)
        {
            Transform current = FindPlate(slot);
            if (IsAlive(platePrefab))
            {
                if (IsAlive(current) && !IsImportedPlateInstance(current))
                {
                    DestroyObject(current.gameObject);
                    current = null;
                }

                if (!IsAlive(current))
                {
                    GameObject importedPlate = Instantiate(platePrefab, slot, false);
                    importedPlate.name = "Plate";
                    importedPlate.transform.localPosition = Vector3.zero;
                    importedPlate.transform.localRotation = Quaternion.identity;
                    importedPlate.transform.localScale = Vector3.one;
                    return importedPlate.transform;
                }

                current.name = "Plate";
                current.localPosition = Vector3.zero;
                current.localRotation = Quaternion.identity;
                current.localScale = Vector3.one;
                return current;
            }

            if (!IsAlive(current))
            {
                GameObject plate = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                plate.name = "Plate";
                plate.transform.SetParent(slot, false);
                plate.transform.localPosition = Vector3.zero;
                plate.transform.localScale = new Vector3(0.75f, 0.08f, 0.75f);
                ApplyColor(plate, Color.white);
                return plate.transform;
            }

            current.name = "Plate";
            ApplyColor(current.gameObject, Color.white);
            return current;
        }

        private bool IsImportedPlateInstance(Transform plate)
        {
            if (!IsAlive(platePrefab) || !IsAlive(plate))
            {
                return false;
            }

            return IsAlive(plate.Find("IngredientPlate_LOD0"))
                || plate.GetComponentsInChildren<MeshFilter>(true).Length > 1;
        }

#if UNITY_EDITOR
        private void ResolvePlatePrefabInEditor()
        {
            if (platePrefab == null)
            {
                platePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Environment/IngredientPlate/Prefabs/IngredientPlate.prefab");
            }
        }
#endif

        private static void FitIngredientItemToPlate(Transform item, bool usesPrefab)
        {
            if (!IsAlive(item))
            {
                return;
            }

            item.localPosition = Vector3.up * PlateIngredientSurfaceY;
            item.localScale = Vector3.one * 0.26f;
            if (!usesPrefab)
            {
                return;
            }

            Bounds bounds = CalculateLocalBounds(item);
            float largestFootprint = Mathf.Max(bounds.size.x, bounds.size.z);
            if (largestFootprint <= 0.0001f || bounds.size.y <= 0.0001f)
            {
                return;
            }

            float footprintScale = IngredientPlateFootprint / largestFootprint;
            float heightScale = IngredientPlateMaxHeight / bounds.size.y;
            float scale = Mathf.Clamp(Mathf.Min(footprintScale, heightScale), 0.04f, 2.4f);
            item.localScale = Vector3.one * scale;
            Vector3 horizontalCenterOffset = item.TransformVector(new Vector3(bounds.center.x, 0f, bounds.center.z));
            float bottomOffsetY = item.TransformVector(Vector3.up * bounds.min.y).y;
            item.localPosition = new Vector3(
                -horizontalCenterOffset.x,
                PlateIngredientSurfaceY - bottomOffsetY,
                -horizontalCenterOffset.z);
        }

        private static Bounds CalculateLocalBounds(Transform root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers == null || renderers.Length == 0)
            {
                return new Bounds(Vector3.zero, Vector3.zero);
            }

            Bounds bounds = new Bounds(root.InverseTransformPoint(renderers[0].bounds.center), Vector3.zero);
            foreach (Renderer renderer in renderers)
            {
                if (!IsAlive(renderer))
                {
                    continue;
                }

                Bounds worldBounds = renderer.bounds;
                bounds.Encapsulate(root.InverseTransformPoint(worldBounds.min));
                bounds.Encapsulate(root.InverseTransformPoint(worldBounds.max));
            }

            return bounds;
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

                Transform label = slot.transform.Find(LabelName);
                if (IsAlive(label))
                {
                    label.gameObject.SetActive(false);
                }

                slot.SetActive(false);
            }
        }

        private TextMesh EnsureLabel(Transform slot)
        {
            Transform existing = slot.Find(LabelName);
            if (IsAlive(existing))
            {
                TextMesh existingText = existing.GetComponent<TextMesh>();
                return IsAlive(existingText) ? existingText : existing.gameObject.AddComponent<TextMesh>();
            }

            GameObject labelObject = new GameObject(LabelName);
            labelObject.transform.SetParent(slot, false);
            TextMesh text = labelObject.AddComponent<TextMesh>();
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            return text;
        }

        private void ConfigureLabel(Transform slot, IngredientData ingredient)
        {
            TextMesh label = EnsureLabel(slot);
            label.gameObject.SetActive(showIngredientLabels);
            label.text = ingredient != null ? ingredient.DisplayName : string.Empty;
            label.color = Color.black;
            label.fontSize = Mathf.Max(8, labelFontSize);
            label.characterSize = Mathf.Max(0.001f, labelCharacterSize);
            label.transform.localPosition = labelOffset;
            ConfigurePlateLabel(label.transform);
        }

        private static void ConfigurePlateLabel(Transform label)
        {
            if (!IsAlive(label))
            {
                return;
            }

            label.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            label.localScale = Vector3.one;
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

        private static void DestroyObject(GameObject target)
        {
            if (!IsAlive(target))
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
        }

        private Vector3 GetSlotPosition(int index, int activeCount)
        {
            int safeCount = Mathf.Max(1, activeCount);
            if (TryGetGrindingTableSlotPosition(index, safeCount, out Vector3 tablePosition))
            {
                return tablePosition;
            }

            float spacing = safeCount <= 3 ? 1.6f : 1.35f;
            float x = (index - (safeCount - 1) * 0.5f) * spacing;
            return new Vector3(x, 0.43f, -1.6f);
        }

        private static bool TryGetGrindingTableSlotPosition(int index, int activeCount, out Vector3 position)
        {
            position = Vector3.zero;
            GameObject table = GameObject.Find("GrindingTable");
            if (!IsAlive(table) || !TryCalculateWorldBounds(table, out Bounds bounds))
            {
                return false;
            }

            float usableWidth = Mathf.Max(0.8f, bounds.size.x - 1.6f);
            float spacing = Mathf.Min(1.35f, usableWidth / Mathf.Max(1, activeCount - 1));
            float x = bounds.center.x + (index - (activeCount - 1) * 0.5f) * spacing;
            float z = bounds.min.z + Mathf.Clamp(bounds.size.z * 0.26f, 0.6f, 1.05f);
            float y = GetGrindingTableSurfaceY(table, bounds) + LabelSurfaceOffset;
            position = new Vector3(x, y, z);
            return true;
        }

        private static float GetGrindingTableSurfaceY(GameObject table, Bounds bounds)
        {
            Transform anchor = table.transform.Find("GrindingBowlAnchor");
            if (IsAlive(anchor))
            {
                return anchor.position.y;
            }

            Transform tabletopCollider = table.transform.Find("TabletopCollider");
            if (IsAlive(tabletopCollider))
            {
                Collider collider = tabletopCollider.GetComponent<Collider>();
                if (IsAlive(collider))
                {
                    return collider.bounds.max.y;
                }
            }

            return bounds.max.y;
        }

        private static bool TryCalculateWorldBounds(GameObject root, out Bounds bounds)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers == null || renderers.Length == 0)
            {
                bounds = new Bounds(root.transform.position, Vector3.zero);
                return false;
            }

            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                if (IsAlive(renderers[i]))
                {
                    bounds.Encapsulate(renderers[i].bounds);
                }
            }

            return true;
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
