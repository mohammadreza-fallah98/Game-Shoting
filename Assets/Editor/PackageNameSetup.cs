using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

// Android (and other platforms) refuse to build without a valid package name, so set one when the editor loads.
[InitializeOnLoad]
public static class PackageNameSetup
{
    const string PackageName = "com.mrf.lanesurvival";

    static PackageNameSetup()
    {
        EditorApplication.delayCall += Apply;
    }

    [MenuItem("Tools/Lane Survival/Set Package Name")]
    static void Apply()
    {
        SetIfInvalid(NamedBuildTarget.Android);
        SetIfInvalid(NamedBuildTarget.Standalone);
        SetIfInvalid(NamedBuildTarget.iOS);
    }

    static void SetIfInvalid(NamedBuildTarget target)
    {
        string current = PlayerSettings.GetApplicationIdentifier(target);
        // Valid identifiers look like com.company.product (letters, digits, underscores; segments start with a letter).
        bool valid = System.Text.RegularExpressions.Regex.IsMatch(current ?? "", @"^[A-Za-z][A-Za-z0-9_]*(\.[A-Za-z][A-Za-z0-9_]*)+$");
        if (valid) return;
        PlayerSettings.SetApplicationIdentifier(target, PackageName);
        Debug.Log("Package name for " + target.TargetName + " set to " + PackageName);
    }
}
