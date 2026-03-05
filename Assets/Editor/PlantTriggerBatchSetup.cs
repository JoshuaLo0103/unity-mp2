using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PlantTriggerBatchSetup
{
    private const string MenuPath = "Tools/Greenhouse/Apply PlantTrigger Template To All Empty Pots";

    [MenuItem(MenuPath)]
    private static void ApplyTemplateToAllEmptyPots()
    {
        Transform selected = Selection.activeTransform;
        if (selected == null)
        {
            Debug.LogWarning("Select a PlantTrigger object (with PotPlanting) first.");
            return;
        }

        PotPlanting templatePot = selected.GetComponent<PotPlanting>();
        BoxCollider templateCollider = selected.GetComponent<BoxCollider>();
        if (templatePot == null)
        {
            Debug.LogWarning("Selected object must have a PotPlanting component.");
            return;
        }

        if (templateCollider == null)
        {
            Debug.LogWarning("Selected object should have a BoxCollider to copy trigger settings.");
            return;
        }

        Transform[] allTransforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int updatedCount = 0;
        int skippedCount = 0;

        foreach (Transform root in allTransforms)
        {
            if (!LooksLikeEmptyPotRoot(root))
                continue;

            Transform plantTrigger = FindDirectChild(root, "PlantTrigger");
            if (plantTrigger == null)
            {
                skippedCount++;
                Debug.LogWarning($"Skipped '{root.name}': missing child named 'PlantTrigger'.", root);
                continue;
            }

            Undo.RecordObject(plantTrigger, "Apply PlantTrigger Transform");
            plantTrigger.localPosition = selected.localPosition;
            plantTrigger.localRotation = selected.localRotation;
            plantTrigger.localScale = selected.localScale;
            plantTrigger.tag = selected.tag;
            plantTrigger.gameObject.layer = selected.gameObject.layer;

            BoxCollider targetCollider = plantTrigger.GetComponent<BoxCollider>();
            if (targetCollider == null)
                targetCollider = Undo.AddComponent<BoxCollider>(plantTrigger.gameObject);

            Undo.RecordObject(targetCollider, "Apply PlantTrigger Collider");
            targetCollider.isTrigger = templateCollider.isTrigger;
            targetCollider.center = templateCollider.center;
            targetCollider.size = templateCollider.size;
            targetCollider.material = templateCollider.material;
            targetCollider.enabled = templateCollider.enabled;

            PotPlanting targetPot = plantTrigger.GetComponent<PotPlanting>();
            if (targetPot == null)
                targetPot = Undo.AddComponent<PotPlanting>(plantTrigger.gameObject);

            Undo.RecordObject(targetPot, "Apply PotPlanting Values");
            CopyPotPlantingValues(templatePot, targetPot);

            Transform plantVisual = FindDirectChild(root, "PlantVisual");
            Transform potVisual = FindDirectChild(root, "PotBig");
            targetPot.plantVisual = plantVisual != null ? plantVisual.gameObject : null;
            targetPot.potVisual = potVisual;

            EditorUtility.SetDirty(plantTrigger);
            EditorUtility.SetDirty(targetCollider);
            EditorUtility.SetDirty(targetPot);
            updatedCount++;
        }

        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.IsValid() && activeScene.isLoaded)
            EditorSceneManager.MarkSceneDirty(activeScene);

        Debug.Log($"PlantTrigger batch update complete. Updated: {updatedCount}, Skipped: {skippedCount}.");
    }

    private static bool LooksLikeEmptyPotRoot(Transform tr)
    {
        if (tr == null)
            return false;

        // Handles names like: EmptyPot_01, EmptyPot_01 (12), etc.
        return tr.name.StartsWith("EmptyPot_01");
    }

    private static Transform FindDirectChild(Transform parent, string childName)
    {
        if (parent == null)
            return null;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name == childName)
                return child;
        }

        return null;
    }

    private static void CopyPotPlantingValues(PotPlanting from, PotPlanting to)
    {
        to.seedRateBase = from.seedRateBase;
        to.perCrystalMultiplier = from.perCrystalMultiplier;
        to.maxTotalMultiplier = from.maxTotalMultiplier;
        to.perCrystalScaleMultiplier = from.perCrystalScaleMultiplier;
        to.maxScaleMultiplier = from.maxScaleMultiplier;
        to.potScaleFactor = from.potScaleFactor;
    }
}
