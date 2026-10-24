using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

// Points Unity at the JDK 17, Android SDK and NDK installed on this machine.
// Reflection is used so the script still compiles when the Android module changes.
[InitializeOnLoad]
public static class AndroidToolsSetup
{
    const string JdkPath = @"D:\Programming\Java\jdk-17.0.20.1+1";
    const string SdkPath = @"D:\Programming\Android\sdk";
    const string NdkPath = @"D:\Programming\Android\sdk\ndk\27.2.12479018";

    static AndroidToolsSetup()
    {
        EditorApplication.delayCall += Apply;
    }

    [MenuItem("Tools/Lane Survival/Set Android Tool Paths")]
    static void Apply()
    {
        Type settings = null;
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            settings = asm.GetType("UnityEditor.Android.AndroidExternalToolsSettings");
            if (settings != null) break;
        }
        if (settings == null)
        {
            Debug.LogWarning("Android module not found; tool paths were not set.");
            return;
        }

        // The Preferences checkboxes "installed with Unity" must be off for custom paths to be used.
        EditorPrefs.SetBool("JdkUseEmbedded", false);
        EditorPrefs.SetBool("AndroidSdkUseEmbedded", false);
        EditorPrefs.SetBool("AndroidNdkUseEmbedded", false);

        SetPath(settings, "jdkRootPath", JdkPath);
        SetPath(settings, "sdkRootPath", SdkPath);
        SetPath(settings, "ndkRootPath", NdkPath);
    }

    static void SetPath(Type settings, string property, string path)
    {
        if (!Directory.Exists(path))
        {
            Debug.LogWarning("Directory not found, skipped: " + path);
            return;
        }

        var prop = settings.GetProperty(property, BindingFlags.Public | BindingFlags.Static);
        if (prop == null || !prop.CanWrite)
        {
            Debug.LogWarning("Android setting not available: " + property);
            return;
        }

        var current = prop.GetValue(null) as string;
        if (string.Equals(current, path, StringComparison.OrdinalIgnoreCase)) return;

        prop.SetValue(null, path);
        Debug.Log("Android " + property + " set to " + path);
    }
}
