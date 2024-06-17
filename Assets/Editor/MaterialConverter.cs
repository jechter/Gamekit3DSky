using System.Collections.Generic;
using System.IO;
using SkyEngine;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using Material = UnityEngine.Material;

public class MaterialConverter
{
    [MenuItem("Convert/ConvertAllMaterials")]
    static void ConvertAllMaterials()
    {

        var project = AssetDatabase.LoadAssetAtPath<SkyProject>("Assets/MySkyProject.asset");
        const string texturePrefixPath = "Assets/3DGamekit/Art/Textures/";
        foreach (var p in AssetDatabase.GetAllAssetPaths())
        {
            if (AssetDatabase.GetMainAssetTypeAtPath(p) != typeof(Material)) continue;
            var mat = AssetDatabase.LoadAssetAtPath<Material>(p);
            foreach (var prop in mat.GetTexturePropertyNames())
            {
                var tex = mat.GetTexture(prop);
                if (tex == null) continue;
                var texPath = AssetDatabase.GetAssetPath(tex);
                Debug.Log($"Material : {p} Prop: {prop} Path: {texPath}");

                if (!texPath.StartsWith(texturePrefixPath)) continue;
                
                var skyMat = SkyMaterialTextureReferencesContainer.ForMaterial(mat, true);
                skyMat.Project = project;
                skyMat.TextureProps[prop] = $"/3DGameKitTextures/{texPath.Substring(texturePrefixPath.Length)}";
                mat.SetTexture(prop, null);
                EditorUtility.SetDirty(skyMat);
                EditorUtility.SetDirty(mat);
            }
        }
    }
  
    [MenuItem("Convert/ConvertAllPrefabInstances")]
    static void ConvertAllPrefabInstances()
    {
        var gos = Resources.FindObjectsOfTypeAll<GameObject>();
        var roots = new HashSet<GameObject>();
        foreach (var go in gos)
        {
            if (PrefabUtility.IsPartOfPrefabInstance(go))
            {
                var root = PrefabUtility.GetNearestPrefabInstanceRoot(go);
                var orig = PrefabUtility.GetCorrespondingObjectFromOriginalSource(root);
                var type = PrefabUtility.GetPrefabAssetType(orig);
                if (type == PrefabAssetType.Model)
                    roots.Add(root);
            }
        }

        var project = AssetDatabase.LoadAssetAtPath<SkyProject>("Assets/MySkyProject.asset");
        const string modelPrefixPath = "Assets/3DGamekit/Art/Models/";
        foreach (var root in roots)
        {
            var meshPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(root);
            if (!meshPath.StartsWith(modelPrefixPath)) continue;
            if (Path.GetExtension(meshPath).ToLowerInvariant() != ".fbx") continue;

            var overrides = PrefabUtility.GetPropertyModifications(root);
            var originalRoot = PrefabUtility.GetCorrespondingObjectFromOriginalSource(root);
            bool hasOverrides = false;
            foreach (var objectOverride in overrides)
            {
                if (PrefabUtility.IsDefaultOverride(objectOverride)) continue;
                if (objectOverride.target != originalRoot.transform && objectOverride.target != originalRoot)
                {
                    Debug.Log($"{root} objectOverride.target {objectOverride.target} {AssetDatabase.GetAssetPath(objectOverride.target)} {AssetDatabase.GetAssetPath(originalRoot)}");
                    hasOverrides = true;
                }
            }
            PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

            if (!hasOverrides)
            {
                while (root.transform.childCount > 0)
                    Object.DestroyImmediate(root.transform.GetChild(0).gameObject);
                foreach (var cmp in root.GetComponents<UnityEngine.Component>())
                {
                    if (cmp is not Transform)
                        Object.DestroyImmediate(cmp);
                }

                var ser = root.AddComponent<SkyEntityReference>();
                ser.project = project;
                ser.projectPath = $"/3DGameKitModels/{meshPath.Substring(modelPrefixPath.Length)}";
                ser.loaded = false;
            }

            EditorUtility.SetDirty(root);  
        }
    }
    
    [MenuItem("Convert/ConvertAllMeshes")]
    static void ConvertAllMeshes()
    {
        var project = AssetDatabase.LoadAssetAtPath<SkyProject>("Assets/MySkyProject.asset");
        const string modelPrefixPath = "Assets/3DGamekit/Art/Models/";
        var meshFilters = Resources.FindObjectsOfTypeAll<MeshFilter>();
        foreach (var mf in meshFilters)
        {
            var mesh = mf.sharedMesh;
            if (mesh == null) continue;
            var meshPath = AssetDatabase.GetAssetPath(mesh);
            if (!meshPath.StartsWith(modelPrefixPath)) continue;
            if (Path.GetExtension(meshPath).ToLowerInvariant() != ".fbx") continue;

            var go = mf.gameObject;
            var smf = go.GetOrAddComponent<SkyMeshFilter>();
            if (smf != null)
            {
                smf.Usage |= SkyMeshFilter.MeshUsageFlags.MeshFilter;
                smf.Project = project;
                smf.ProjectPath = $"/3DGameKitModels/{meshPath.Substring(modelPrefixPath.Length)}#{mesh.name}";
                Object.DestroyImmediate(mf, true);
                EditorUtility.SetDirty(go);
            }
        }
        
        var meshColliders = Resources.FindObjectsOfTypeAll<MeshCollider>();
        foreach (var mc in meshColliders)
        {
            var mesh = mc.sharedMesh;
            if (mesh == null) continue;
            var meshPath = AssetDatabase.GetAssetPath(mesh);
            if (!meshPath.StartsWith(modelPrefixPath)) continue;
            if (Path.GetExtension(meshPath).ToLowerInvariant() != ".fbx") continue;

            var go = mc.gameObject;
            var smf = go.GetOrAddComponent<SkyMeshFilter>();
            if (smf != null)
            {
                smf.Usage |= SkyMeshFilter.MeshUsageFlags.MeshCollider;
                smf.Project = project;
                smf.ProjectPath = $"/3DGameKitModels/{meshPath.Substring(modelPrefixPath.Length)}#{mesh.name}";
                mc.sharedMesh = null;
                EditorUtility.SetDirty(go);
            }
        }
    }

    [MenuItem("Convert/FixMeshes")]
    static void FixMeshes()
    {
        var smfs = Resources.FindObjectsOfTypeAll<SkyMeshFilter>();
        foreach (var smf in smfs)
        {
            smf.Usage = SkyMeshFilter.MeshUsageFlags.None;
            if (smf.GetComponent<MeshRenderer>())
            {
                smf.Usage |= SkyMeshFilter.MeshUsageFlags.MeshFilter;
            }
            else
            {
                if (smf.GetComponent<MeshFilter>())
                    Object.DestroyImmediate(smf.GetComponent<MeshFilter>(), true);
            }

            if (smf.GetComponent<MeshCollider>())
            {
                smf.Usage |= SkyMeshFilter.MeshUsageFlags.MeshCollider;
            }
            EditorUtility.SetDirty(smf.gameObject);
        } 
    }

}
