using System.Collections.Generic;
using UnityEngine;

// Loads the character and monster pictures (PNG files stored as .bytes in Resources/Sprites)
// and builds camera-facing sprite objects from them.
public static class Sprites
{
    static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

    // One unit wide sprite with its pivot at the bottom center, so it can stand on the ground.
    public static Sprite Get(string name)
    {
        if (cache.TryGetValue(name, out var cached) && cached != null) return cached;

        var asset = Resources.Load<TextAsset>("Sprites/" + name);
        if (asset == null)
        {
            Debug.LogWarning("Sprite not found: " + name);
            return null;
        }

        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        tex.LoadImage(asset.bytes);
        tex.wrapMode = TextureWrapMode.Clamp;
        var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0f), tex.width);
        cache[name] = sprite;
        return sprite;
    }

    // Sprites face the camera, so they use the camera's rotation.
    public static Quaternion Facing
    {
        get
        {
            var cam = Camera.main;
            return cam != null ? cam.transform.rotation : Quaternion.identity;
        }
    }

    // Replaces the children of `parent` with `count` sprites placed side by side.
    public static SpriteRenderer[] Build(Transform parent, string name, int count, float spacing, float height)
    {
        foreach (Transform child in parent)
        {
            child.gameObject.SetActive(false);
            Object.Destroy(child.gameObject);
        }

        var sprite = Get(name);
        var result = new SpriteRenderer[count];
        for (int i = 0; i < count; i++)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3((i - (count - 1) / 2f) * spacing, 0f, 0f);
            go.transform.rotation = Facing;
            go.transform.localScale = Vector3.one * height;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            result[i] = sr;
        }
        return result;
    }
}
