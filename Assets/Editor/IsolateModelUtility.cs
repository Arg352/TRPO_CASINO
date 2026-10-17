using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class IsolateModelUtility : EditorWindow
{
    [MenuItem("Tools/Isolate Selected Model Completely")]
    public static void Isolate()
    {
        GameObject target = Selection.activeGameObject;
        if (target == null) { Debug.LogError("Выдели модель на сцене!"); return; }

        string path = EditorUtility.SaveFolderPanel("Выбери пустую папку для сохранения копии", "Assets", "");
        if (string.IsNullOrEmpty(path)) return;
        path = "Assets" + path.Substring(Application.dataPath.Length);

        // 1. Копируем и изолируем меши
        var meshFilters = target.GetComponentsInChildren<MeshFilter>();
        var skinnedMeshRenderers = target.GetComponentsInChildren<SkinnedMeshRenderer>();
        var oldToNewMesh = new Dictionary<Mesh, Mesh>();

        foreach (var mf in meshFilters) { if (mf.sharedMesh != null) DuplicateMesh(mf.sharedMesh, path, oldToNewMesh); }
        foreach (var smr in skinnedMeshRenderers) { if (smr.sharedMesh != null) DuplicateMesh(smr.sharedMesh, path, oldToNewMesh); }

        // 2. Копируем и изолируем материалы
        var renderers = target.GetComponentsInChildren<Renderer>();
        var oldToNewMat = new Dictionary<Material, Material>();

        foreach (var rend in renderers)
        {
            Material[] sharedMats = rend.sharedMaterials;
            for (int i = 0; i < sharedMats.Length; i++)
            {
                if (sharedMats[i] != null && !oldToNewMat.ContainsKey(sharedMats[i]))
                {
                    Material newMat = new Material(sharedMats[i]);
                    string matPath = Path.Combine(path, sharedMats[i].name + "_Isolated.mat");
                    AssetDatabase.CreateAsset(newMat, matPath);
                    oldToNewMat[sharedMats[i]] = newMat;
                }
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // 3. Создаем автономный дубликат на сцене и переназначаем ресурсы
        GameObject clonedObject = Instantiate(target, target.transform.parent);
        clonedObject.name = target.name + "_Isolated_Final";

        foreach (var mf in clonedObject.GetComponentsInChildren<MeshFilter>())
            if (mf.sharedMesh != null && oldToNewMesh.ContainsKey(mf.sharedMesh)) mf.sharedMesh = oldToNewMesh[mf.sharedMesh];

        foreach (var smr in clonedObject.GetComponentsInChildren<SkinnedMeshRenderer>())
            if (smr.sharedMesh != null && oldToNewMesh.ContainsKey(smr.sharedMesh)) smr.sharedMesh = oldToNewMesh[smr.sharedMesh];

        foreach (var rend in clonedObject.GetComponentsInChildren<Renderer>())
        {
            Material[] newMats = new Material[rend.sharedMaterials.Length];
            for (int i = 0; i < rend.sharedMaterials.Length; i++)
            {
                if (rend.sharedMaterials[i] != null && oldToNewMat.ContainsKey(rend.sharedMaterials[i]))
                    newMats[i] = oldToNewMat[rend.sharedMaterials[i]];
                else
                    newMats[i] = rend.sharedMaterials[i];
            }
            rend.sharedMaterials = newMats;
        }

        // 4. Делаем из него чистый префаб
        string prefabPath = Path.Combine(path, clonedObject.name + ".prefab");
        PrefabUtility.SaveAsPrefabAsset(clonedObject, prefabPath);

        Debug.LogWarning("Клон готов! Он на сцене под именем " + clonedObject.name + " и в папке как независимый Префаб. Теперь можешь удалять все старые папки.");
    }

    private static void DuplicateMesh(Mesh sourceMesh, string savePath, Dictionary<Mesh, Mesh> dict)
    {
        if (dict.ContainsKey(sourceMesh)) return;
        Mesh newMesh = Instantiate(sourceMesh);
        string meshAssetPath = Path.Combine(savePath, sourceMesh.name + "_IsolatedMesh.mesh");
        AssetDatabase.CreateAsset(newMesh, meshAssetPath);
        dict[sourceMesh] = newMesh;
    }
}
