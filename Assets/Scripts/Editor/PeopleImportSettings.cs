#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>Imports everything under Assets/People as Humanoid; animation clips are in place, looping where it makes sense.</summary>
public class PeopleImportSettings : AssetPostprocessor
{
    static bool InPeople(string p) { return p.StartsWith("Assets/People/"); }

    void OnPreprocessModel()
    {
        if (!InPeople(assetPath)) return;
        var mi = (ModelImporter)assetImporter;
        mi.animationType = ModelImporterAnimationType.Human;
        mi.optimizeGameObjects = false;
        if (assetPath.Contains("/Characters/"))
        {
            mi.importAnimation = false;
            mi.meshCompression = ModelImporterMeshCompression.Medium;
            mi.isReadable = false;
        }
        else
        {
            mi.importAnimation = true;
            mi.materialImportMode = ModelImporterMaterialImportMode.None;
        }
    }

    void OnPreprocessAnimation()
    {
        if (!InPeople(assetPath) || !assetPath.Contains("/Animations/")) return;
        var mi = (ModelImporter)assetImporter;
        var clips = mi.defaultClipAnimations;
        if (clips == null || clips.Length == 0) return;
        string file = System.IO.Path.GetFileNameWithoutExtension(assetPath);
        bool loop = file == "Idle" || file == "Walking" || file == "Talking";
        for (int i = 0; i < clips.Length; i++)
        {
            clips[i].name = file;
            clips[i].loopTime = loop;
            clips[i].lockRootRotation = true;
            clips[i].lockRootHeightY = true;
            clips[i].lockRootPositionXZ = true;
            clips[i].keepOriginalOrientation = true;
            clips[i].keepOriginalPositionY = true;
            clips[i].keepOriginalPositionXZ = true;
        }
        mi.clipAnimations = clips;
    }
}
#endif
