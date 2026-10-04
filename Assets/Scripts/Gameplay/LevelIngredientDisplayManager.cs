using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace TheTasteReviver
{
    public class LevelIngredientDisplayManager : MonoBehaviour
    {
        private const string ContainerName = "Ingredient Slots";
        private const string MixedPowderContainerName = "Mixed Powder Slots";
        private const string LabelName = "Ingredient Label";
        private const string PowderVisualsName = "Powder Visuals";
        private const int MaxSlots = 4;
        private const int MaxMixedPowderSlots = 4;
        private const float LabelSurfaceOffset = 0.08f;
        private const float PlateLabelYOffset = 0.018f;
        private const float PlateIngredientSurfaceY = 0.16f;
        private const float IngredientPlateFootprint = 0.54f;
        private const float IngredientPlateMaxHeight = 0.3f;
        private const float MortarDropHeight = 0.2f;
        private const float GroundVisualLift = 0.1f;
        private const float MortarGroundVisualLift = 0.04f;
        private const float GroundVisualScale = 0.8f;
        private const float GroundVisualFootprint = 0.7f;
        private const float GroundVisualMaxHeight = 0.14f;
        private const float HomeGroundVisualLift = 0.24f;
        private const string PortionClusterName = "Portion Cluster";

        public MortarArea mortarArea;
        public RecipeAttemptManager attemptManager;
        public GameObject platePrefab;
        public bool hideLegacyDisplaysOnFirstRefresh = true;
        public bool showIngredientLabels = false;
        public Vector3 labelOffset = new Vector3(0f, PlateLabelYOffset, -0.28f);
        public int labelFontSize = 60;
        public float labelCharacterSize = 0.035f;

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
            ClearMixedPowderBatches();

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

        public void ShowMixedPowderBatch(int batchID, IReadOnlyList<IngredientData> ingredients)
        {
            if (batchID <= 0 || ingredients == null || ingredients.Count == 0)
            {
                return;
            }

            List<GameObject> slots = EnsureMixedPowderSlots();
            int index = Mathf.Clamp(batchID - 1, 0, slots.Count - 1);
            GameObject slot = slots[index];
            if (!IsAlive(slot))
            {
                return;
            }

            slot.SetActive(false);
            slot.transform.position = GetMixedPowderSlotPosition(index, Mathf.Min(batchID, slots.Count));
            slot.transform.rotation = Quaternion.identity;

            EnsurePlateVisual(slot.transform);
            ConfigureMixedPowderVisuals(slot.transform, ingredients);
            ConfigureMixedPowderLabel(slot.transform, "Mixed Powder " + batchID);
            ConfigureMixedPowderDrag(slot.transform, ingredients);
            slot.SetActive(true);
        }

        public void ClearMixedPowderBatches()
        {
            Transform container = transform.Find(MixedPowderContainerName);
            if (!IsAlive(container))
            {
                return;
            }

            foreach (Transform child in container)
            {
                if (IsAlive(child))
                {
                    child.gameObject.SetActive(false);
                }
            }
        }

        public void ShowMixturePowderInIngredientSlot(IReadOnlyList<IngredientData> ingredients)
        {
            List<IngredientData> validIngredients = ingredients != null
                ? ingredients.Where(ingredient => ingredient != null).Distinct().ToList()
                : new List<IngredientData>();
            if (validIngredients.Count <= 1)
            {
                return;
            }

            List<GameObject> slots = EnsureSlots();
            GameObject targetSlot = slots.FirstOrDefault(slot => SlotContainsIngredient(slot, validIngredients));
            if (!IsAlive(targetSlot))
            {
                return;
            }

            targetSlot.SetActive(true);
            EnsurePlateVisual(targetSlot.transform);

            Transform item = FindIngredientItem(targetSlot.transform);
            if (IsAlive(item))
            {
                item.gameObject.SetActive(false);
            }

            ConfigureMixedPowderVisuals(targetSlot.transform, validIngredients);
            ConfigureMixedPowderLabel(targetSlot.transform, "Mixture Powder");
            ConfigureMixedPowderDrag(targetSlot.transform, validIngredients);
        }

        public void ClearPreparedPowderDisplays()
        {
            foreach (GameObject slot in EnsureSlots())
            {
                if (!IsAlive(slot))
                {
                    continue;
                }

                Transform visuals = slot.transform.Find(PowderVisualsName);
                if (IsAlive(visuals))
                {
                    DestroyObject(visuals.gameObject);
                }

                Transform item = slot.transform.Find("Ingredient");
                if (IsAlive(item))
                {
                    item.gameObject.SetActive(true);
                }
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

        private List<GameObject> EnsureMixedPowderSlots()
        {
            Transform container = transform.Find(MixedPowderContainerName);
            if (!IsAlive(container))
            {
                container = new GameObject(MixedPowderContainerName).transform;
                container.SetParent(transform, false);
            }

            List<GameObject> slots = new List<GameObject>();
            for (int i = 0; i < MaxMixedPowderSlots; i++)
            {
                string slotName = "Mixed Powder Slot " + (i + 1);
                Transform slotTransform = container.Find(slotName);
                if (!IsAlive(slotTransform))
                {
                    slotTransform = new GameObject(slotName).transform;
                    slotTransform.SetParent(container, false);
                    EnsurePlateVisual(slotTransform);
                    EnsureLabel(slotTransform);
                    slotTransform.gameObject.SetActive(false);
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

            slot.transform.position = GetSlotPosition(index, activeCount);
            slot.transform.rotation = Quaternion.identity;

            EnsurePlateVisual(slot.transform);
            Transform previousPowderVisuals = slot.transform.Find(PowderVisualsName);
            if (IsAlive(previousPowderVisuals))
            {
                DestroyObject(previousPowderVisuals.gameObject);
            }

            Transform item = EnsureIngredientItem(slot.transform, ingredient);
            if (!IsAlive(item))
            {
                return;
            }

            item.gameObject.name = "Ingredient";
            item.gameObject.SetActive(true);
            StripLodGroups(item.gameObject);
            ConfigurePortionCluster(item, ingredient);
            FitIngredientItemToPlate(item, ingredient.prefab != null);
            ConfigureIngredientDragCollider(item, ingredient.prefab != null);
            if (ingredient.prefab == null)
            {
                ApplyColor(item.gameObject, ingredient.ingredientColor);
            }

            DraggableIngredient drag = item.GetComponent<DraggableIngredient>();
            if (!IsAlive(drag))
            {
                drag = item.gameObject.AddComponent<DraggableIngredient>();
            }

            drag.ingredientData = ingredient;
            drag.representedIngredients.Clear();
            drag.mortarArea = mortarArea;
            drag.attemptManager = attemptManager;
            drag.sourcePrefab = ingredient.prefab;
            drag.mortarDropHeight = MortarDropHeight;
            drag.groundVisualLift = GroundVisualLift;
            drag.mortarGroundVisualLift = MortarGroundVisualLift;
            drag.groundVisualScale = GroundVisualScale;
            drag.groundVisualFootprint = GroundVisualFootprint;
            drag.groundVisualMaxHeight = GroundVisualMaxHeight;
            drag.homeGroundVisualLift = HomeGroundVisualLift;
            drag.ResetHomePosition();

            ConfigureLabel(slot.transform, ingredient);
            slot.SetActive(true);
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
                DraggableIngredient drag = item.GetComponent<DraggableIngredient>();
                if (!IsAlive(drag))
                {
                    drag = item.AddComponent<DraggableIngredient>();
                }

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

        private static void ConfigureIngredientDragCollider(Transform item, bool usesPrefab)
        {
            if (!IsAlive(item))
            {
                return;
            }

            foreach (Collider collider in item.GetComponentsInChildren<Collider>(true))
            {
                if (IsAlive(collider) && collider.transform != item)
                {
                    collider.enabled = false;
                }
            }

            if (!usesPrefab)
            {
                Collider fallbackCollider = item.GetComponent<Collider>();
                if (IsAlive(fallbackCollider))
                {
                    fallbackCollider.enabled = true;
                }

                return;
            }

            Bounds bounds = CalculateLocalBounds(item);
            if (bounds.size.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            BoxCollider boxCollider = item.GetComponent<BoxCollider>();
            if (!IsAlive(boxCollider))
            {
                boxCollider = item.gameObject.AddComponent<BoxCollider>();
            }

            if (!IsAlive(boxCollider))
            {
                return;
            }

            boxCollider.center = bounds.center;
            boxCollider.size = new Vector3(
                Mathf.Max(bounds.size.x, 0.2f),
                Mathf.Max(bounds.size.y, 0.12f),
                Mathf.Max(bounds.size.z, 0.2f));
            boxCollider.enabled = true;
        }

        private static void ConfigurePortionCluster(Transform item, IngredientData ingredient)
        {
            if (!IsAlive(item) || ingredient == null || !ShouldBuildPortionCluster(ingredient))
            {
                ClearPortionCluster(item);
                return;
            }

            if (IsAlive(item.Find(PortionClusterName)))
            {
                return;
            }

            Transform source = FindVisualSource(item);
            if (!IsAlive(source))
            {
                return;
            }

            GameObject cluster = new GameObject(PortionClusterName);
            cluster.transform.SetParent(item, false);

            int count = GetPortionCopyCount(ingredient);
            for (int i = 0; i < count; i++)
            {
                GameObject copy = CreatePortionCopy(source, item, cluster.transform);
                if (!IsAlive(copy))
                {
                    continue;
                }

                copy.name = "Portion Piece " + (i + 1);
                copy.transform.localPosition += GetPortionOffset(i, count);
                copy.transform.localRotation *= Quaternion.Euler(0f, i * 37f, 0f);
                float scale = 0.72f + (i % 5) * 0.06f;
                copy.transform.localScale = Vector3.Scale(copy.transform.localScale, Vector3.one * scale);
                StripInteractionComponents(copy);
            }
        }

        private static void ClearPortionCluster(Transform item)
        {
            if (!IsAlive(item))
            {
                return;
            }

            Transform cluster = item.Find(PortionClusterName);
            if (IsAlive(cluster))
            {
                DestroyObject(cluster.gameObject);
            }
        }

        private static bool ShouldBuildPortionCluster(IngredientData ingredient)
        {
            string id = ingredient.ingredientID;
            return id == "Rice"
                || id == "GlutinousRice"
                || id == "RedBean"
                || id == "BlackSesame"
                || id == "WhitePepper"
                || id == "CoarseSalt"
                || id == "YangjiangDouchi"
                || id == "DriedTangerinePeel"
                || id == "Ginger"
                || id == "SandGinger"
                || id == "Chili";
        }

        private static int GetPortionCopyCount(IngredientData ingredient)
        {
            string id = ingredient.ingredientID;
            if (id == "Rice" || id == "GlutinousRice" || id == "RedBean" || id == "BlackSesame" || id == "WhitePepper")
            {
                return 18;
            }

            if (id == "CoarseSalt" || id == "YangjiangDouchi")
            {
                return 12;
            }

            return 6;
        }

        private static Vector3 GetPortionOffset(int index, int count)
        {
            float radius = count > 12 ? 0.34f : 0.24f;
            float angle = index * 137.5f * Mathf.Deg2Rad;
            float ring = Mathf.Sqrt((index + 0.5f) / Mathf.Max(1f, count)) * radius;
            float y = 0.005f * (index % 4);
            return new Vector3(Mathf.Cos(angle) * ring, y, Mathf.Sin(angle) * ring);
        }

        private static GameObject CreatePortionCopy(Transform source, Transform item, Transform parent)
        {
            if (!IsAlive(source) || !IsAlive(parent))
            {
                return null;
            }

            if (source != item)
            {
                return Instantiate(source.gameObject, parent, false);
            }

            MeshFilter sourceMesh = source.GetComponent<MeshFilter>();
            Renderer sourceRenderer = source.GetComponent<Renderer>();
            if (!IsAlive(sourceMesh) || !IsAlive(sourceRenderer))
            {
                return null;
            }

            GameObject copy = new GameObject("Portion Piece");
            copy.transform.SetParent(parent, false);
            copy.transform.localPosition = Vector3.zero;
            copy.transform.localRotation = Quaternion.identity;
            copy.transform.localScale = Vector3.one;

            MeshFilter meshFilter = copy.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = sourceMesh.sharedMesh;

            MeshRenderer meshRenderer = copy.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterials = sourceRenderer.sharedMaterials;
            return copy;
        }

        private static Transform FindVisualSource(Transform item)
        {
            Renderer renderer = item.GetComponentsInChildren<Renderer>(true)
                .FirstOrDefault(x => IsAlive(x) && !IsChildOf(x.transform, item.Find(PortionClusterName)));
            if (!IsAlive(renderer))
            {
                return null;
            }

            Transform source = renderer.transform;
            while (source.parent != null && source.parent != item)
            {
                source = source.parent;
            }

            return source;
        }

        private static void StripInteractionComponents(GameObject copy)
        {
            StripLodGroups(copy);
            RemoveRedundantLodModels(copy.transform);

            foreach (Collider collider in copy.GetComponentsInChildren<Collider>(true))
            {
                collider.enabled = false;
            }

            foreach (DraggableIngredient drag in copy.GetComponentsInChildren<DraggableIngredient>(true))
            {
                DestroyComponent(drag);
            }
        }

        private static void StripLodGroups(GameObject target)
        {
            if (!IsAlive(target))
            {
                return;
            }

            RemoveRedundantLodModels(target.transform);
            foreach (LODGroup lodGroup in target.GetComponentsInChildren<LODGroup>(true))
            {
                DestroyComponent(lodGroup);
            }
        }

        private static void RemoveRedundantLodModels(Transform root)
        {
            if (!IsAlive(root))
            {
                return;
            }

            IEnumerable<IGrouping<Transform, Transform>> siblingGroups = root
                .GetComponentsInChildren<Transform>(true)
                .Where(transform => transform != root && GetLodIndex(transform) >= 0)
                .GroupBy(transform => transform.parent);
            foreach (IGrouping<Transform, Transform> siblingGroup in siblingGroups)
            {
                Transform keep = siblingGroup.FirstOrDefault(transform => GetLodIndex(transform) == 0)
                    ?? siblingGroup.OrderBy(GetLodIndex).First();
                keep.gameObject.SetActive(true);
                foreach (Transform candidate in siblingGroup.Where(transform => transform != keep).ToList())
                {
                    if (IsAlive(candidate))
                    {
                        candidate.gameObject.SetActive(false);
                        DestroyObject(candidate.gameObject);
                    }
                }
            }
        }

        private static int GetLodIndex(Transform transform)
        {
            const string prefix = "model_LOD";
            if (!IsAlive(transform) || !transform.name.StartsWith(prefix))
            {
                return -1;
            }

            return int.TryParse(transform.name.Substring(prefix.Length), out int index) ? index : -1;
        }

        private static Transform FindChildRecursive(Transform root, string childName)
        {
            if (!IsAlive(root))
            {
                return null;
            }

            foreach (Transform child in root)
            {
                if (!IsAlive(child))
                {
                    continue;
                }

                if (child.name == childName)
                {
                    return child;
                }

                Transform match = FindChildRecursive(child, childName);
                if (IsAlive(match))
                {
                    return match;
                }
            }

            return null;
        }

        private static void FindChildrenRecursive(Transform root, string childName, List<Transform> matches)
        {
            if (!IsAlive(root))
            {
                return;
            }

            foreach (Transform child in root)
            {
                if (!IsAlive(child))
                {
                    continue;
                }

                if (child.name == childName)
                {
                    matches.Add(child);
                }

                FindChildrenRecursive(child, childName, matches);
            }
        }

        private void ConfigureMixedPowderVisuals(Transform slot, IReadOnlyList<IngredientData> ingredients)
        {
            Transform visuals = slot.Find(PowderVisualsName);
            if (IsAlive(visuals))
            {
                DestroyObject(visuals.gameObject);
            }

            visuals = new GameObject(PowderVisualsName).transform;
            visuals.SetParent(slot, false);
            visuals.localPosition = new Vector3(0f, PlateIngredientSurfaceY + 0.01f, 0.02f);
            visuals.localRotation = Quaternion.identity;
            visuals.localScale = Vector3.one;

            List<IngredientData> validIngredients = ingredients.Where(x => x != null && x.groundPrefab != null).ToList();
            if (validIngredients.Count == 0)
            {
                GameObject fallback = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                fallback.name = "Powder";
                fallback.transform.SetParent(visuals, false);
                fallback.transform.localScale = new Vector3(0.85f, 0.12f, 0.85f);
                ApplyColor(fallback, new Color(0.86f, 0.82f, 0.72f));
                StripInteractionComponents(fallback);
                return;
            }

            for (int i = 0; i < validIngredients.Count; i++)
            {
                IngredientData ingredient = validIngredients[i];
                GameObject powder = Instantiate(ingredient.groundPrefab, visuals, false);
                powder.name = ingredient.DisplayName + " Powder";
                powder.transform.localPosition = Vector3.zero;
                powder.transform.localRotation = Quaternion.Euler(0f, i * 41f, 0f);
                powder.transform.localScale = Vector3.one;
                StripInteractionComponents(powder);
                FitPowderVisualToSlot(powder.transform);
                powder.transform.localPosition += GetPowderStackOffset(i, validIngredients.Count);
            }
        }

        private static void FitPowderVisualToSlot(Transform powder)
        {
            if (!IsAlive(powder))
            {
                return;
            }

            Bounds bounds = CalculateLocalBounds(powder);
            if (bounds.size.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            float footprint = Mathf.Max(bounds.size.x, bounds.size.z);
            float scale = Mathf.Min(
                GroundVisualFootprint / Mathf.Max(footprint, 0.0001f),
                GroundVisualMaxHeight / Mathf.Max(bounds.size.y, 0.0001f));
            powder.localScale *= Mathf.Clamp(scale, 0.05f, 4f);
            powder.localPosition += new Vector3(
                -bounds.center.x * powder.localScale.x,
                -bounds.min.y * powder.localScale.y,
                -bounds.center.z * powder.localScale.z);
        }

        private static Vector3 GetPowderStackOffset(int index, int count)
        {
            if (count <= 1)
            {
                return Vector3.zero;
            }

            float layerLift = 0.004f * index;
            float centerJitter = index % 2 == 0 ? 0f : 0.012f;
            return new Vector3(centerJitter, layerLift, -centerJitter);
        }

        private void ConfigureMixedPowderDrag(Transform slot, IReadOnlyList<IngredientData> ingredients)
        {
            Transform visuals = slot.Find(PowderVisualsName);
            if (!IsAlive(visuals))
            {
                return;
            }

            DraggableIngredient drag = visuals.GetComponent<DraggableIngredient>();
            if (!IsAlive(drag))
            {
                drag = visuals.gameObject.AddComponent<DraggableIngredient>();
            }

            BoxCollider collider = visuals.GetComponent<BoxCollider>();
            if (!IsAlive(collider))
            {
                collider = visuals.gameObject.AddComponent<BoxCollider>();
            }

            collider.center = Vector3.zero;
            collider.size = new Vector3(1.2f, 0.35f, 1.2f);
            collider.enabled = true;

            drag.ingredientData = null;
            drag.representedIngredients = ingredients != null
                ? ingredients.Where(ingredient => ingredient != null).Distinct().ToList()
                : new List<IngredientData>();
            drag.mortarArea = mortarArea;
            drag.attemptManager = attemptManager;
            drag.mortarDropHeight = MortarDropHeight;
            drag.groundVisualLift = GroundVisualLift;
            drag.mortarGroundVisualLift = MortarGroundVisualLift;
            drag.groundVisualScale = GroundVisualScale;
            drag.groundVisualFootprint = GroundVisualFootprint;
            drag.groundVisualMaxHeight = GroundVisualMaxHeight;
            drag.homeGroundVisualLift = HomeGroundVisualLift;
            drag.ResetHomePosition();
        }

        private void ConfigureMixedPowderLabel(Transform slot, string labelText)
        {
            TextMeshPro label = EnsureLabel(slot);
            label.gameObject.SetActive(true);
            label.text = labelText;
            label.color = Color.black;
            label.fontSize = Mathf.Max(8, labelFontSize);
            label.font = ThemeFontProvider.GetTmpFont(labelFontSize);
            label.transform.localScale = Vector3.one * GetLabelCharacterSize(label.text) * 0.9f;
            label.transform.localPosition = new Vector3(0f, Mathf.Max(0.006f, labelOffset.y), 0.32f);
            ConfigurePlateLabel(label.transform);
        }

        private static bool SlotContainsIngredient(GameObject slot, IReadOnlyList<IngredientData> ingredients)
        {
            if (!IsAlive(slot) || ingredients == null)
            {
                return false;
            }

            DraggableIngredient drag = slot.GetComponentInChildren<DraggableIngredient>(true);
            return IsAlive(drag) && drag.ingredientData != null && ingredients.Contains(drag.ingredientData);
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
                    DisablePlateColliders(importedPlate.transform);
                    return importedPlate.transform;
                }

                current.name = "Plate";
                current.localPosition = Vector3.zero;
                current.localRotation = Quaternion.identity;
                current.localScale = Vector3.one;
                DisablePlateColliders(current);
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
                DisablePlateColliders(plate.transform);
                return plate.transform;
            }

            current.name = "Plate";
            ApplyColor(current.gameObject, Color.white);
            DisablePlateColliders(current);
            return current;
        }

        private static void DisablePlateColliders(Transform plate)
        {
            if (!IsAlive(plate))
            {
                return;
            }

            foreach (Collider collider in plate.GetComponentsInChildren<Collider>(true))
            {
                if (IsAlive(collider))
                {
                    collider.enabled = false;
                }
            }
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

        private TextMeshPro EnsureLabel(Transform slot)
        {
            Transform existing = slot.Find(LabelName);
            if (IsAlive(existing))
            {
                TextMeshPro existingText = existing.GetComponent<TextMeshPro>();
                return IsAlive(existingText) ? existingText : existing.gameObject.AddComponent<TextMeshPro>();
            }

            GameObject labelObject = new GameObject(LabelName);
            labelObject.transform.SetParent(slot, false);
            TextMeshPro text = labelObject.AddComponent<TextMeshPro>();
            text.alignment = TextAlignmentOptions.Center;
            return text;
        }

        private void ConfigureLabel(Transform slot, IngredientData ingredient)
        {
            TextMeshPro label = EnsureLabel(slot);
            label.gameObject.SetActive(showIngredientLabels);
            label.text = ingredient != null ? ingredient.DisplayName : string.Empty;
            label.color = Color.black;
            label.fontSize = Mathf.Max(8, labelFontSize);
            label.font = ThemeFontProvider.GetTmpFont(labelFontSize);
            label.transform.localScale = Vector3.one * GetLabelCharacterSize(label.text);
            label.transform.localPosition = GetLabelPosition(label.text);
            ConfigurePlateLabel(label.transform);
        }

        private static void ConfigurePlateLabel(Transform label)
        {
            if (!IsAlive(label))
            {
                return;
            }

            label.localRotation = Quaternion.Euler(90f, 0f, 0f);
        }

        private float GetLabelCharacterSize(string text)
        {
            int length = string.IsNullOrWhiteSpace(text) ? 0 : text.Length;
            float size = Mathf.Max(0.001f, labelCharacterSize);
            if (length > 16)
            {
                size *= 0.72f;
            }
            else if (length > 10)
            {
                size *= 0.84f;
            }

            return size;
        }

        private Vector3 GetLabelPosition(string text)
        {
            int length = string.IsNullOrWhiteSpace(text) ? 0 : text.Length;
            float z = length > 16 ? -0.34f : labelOffset.z;
            return new Vector3(labelOffset.x, Mathf.Max(0.006f, labelOffset.y), z);
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

        private static void DestroyComponent(Component target)
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

        private Vector3 GetMixedPowderSlotPosition(int index, int activeCount)
        {
            int safeCount = Mathf.Max(1, activeCount);
            if (TryGetGrindingTableSlotPosition(index, safeCount, out Vector3 tablePosition))
            {
                GameObject table = GameObject.Find("GrindingTable");
                if (IsAlive(table) && TryCalculateWorldBounds(table, out Bounds bounds))
                {
                    float usableWidth = Mathf.Max(0.8f, bounds.size.x - 1.6f);
                    float spacing = Mathf.Min(1.05f, usableWidth / Mathf.Max(1, safeCount - 1));
                    float x = bounds.center.x + (index - (safeCount - 1) * 0.5f) * spacing;
                    float z = bounds.center.z + Mathf.Clamp(bounds.size.z * 0.22f, 0.45f, 0.95f);
                    return new Vector3(x, tablePosition.y, z);
                }
            }

            float fallbackX = (index - (safeCount - 1) * 0.5f) * 1.05f;
            return new Vector3(fallbackX, 0.43f, 0.95f);
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
