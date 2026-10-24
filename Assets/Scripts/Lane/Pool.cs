using System;
using System.Collections.Generic;
using UnityEngine;

// Minimal object pool keyed by name. Returned objects are inactive.
public static class Pool
{
    static readonly Dictionary<string, Stack<GameObject>> pools = new Dictionary<string, Stack<GameObject>>();

    public static GameObject Get(string key, Func<GameObject> create)
    {
        if (pools.TryGetValue(key, out var stack))
        {
            // Skip objects destroyed by a scene reload.
            while (stack.Count > 0)
            {
                var go = stack.Pop();
                if (go != null) return go;
            }
        }
        var created = create();
        created.SetActive(false);
        return created;
    }

    public static void Release(string key, GameObject go)
    {
        go.SetActive(false);
        if (!pools.TryGetValue(key, out var stack))
            pools[key] = stack = new Stack<GameObject>();
        stack.Push(go);
    }
}
