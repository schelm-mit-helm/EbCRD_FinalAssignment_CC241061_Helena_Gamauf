using UnityEngine;

public static class ItemVisualUtility
{
    public const float HeldTargetMaxSize = 0.35f;
    public const float DroppedTargetMaxSize = 0.25f;

    const float MinRenderableSize = 0.001f;
    const float FallbackDisplayScale = 50f;

    public static void PrepareForDisplay(
        GameObject root,
        float targetMaxSize,
        float extraScale = 1f,
        bool preservePrefabTransform = false)
    {
        if (root == null)
        {
            return;
        }

        ActivateHierarchy(root);
        StripPhysics(root);
        EnableRenderers(root);

        if (!preservePrefabTransform)
        {
            root.transform.localRotation = Quaternion.identity;
        }

        var displayScale = ComputeDisplayScale(root, targetMaxSize) * ResolveExtraScale(extraScale);
        root.transform.localScale = Vector3.one * displayScale;
        SetLayerRecursively(root, LayerMask.NameToLayer("Default"));
    }

    public static void AlignBottomToWorldY(Transform root, float worldY)
    {
        var renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            return;
        }

        var bounds = renderers[0].bounds;
        for (var i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        var lift = worldY - bounds.min.y;
        if (Mathf.Abs(lift) > 0.0001f)
        {
            root.position += new Vector3(0f, lift, 0f);
        }
    }

    static void SetLayerRecursively(GameObject root, int layer)
    {
        if (layer < 0)
        {
            return;
        }

        root.layer = layer;

        foreach (var child in root.GetComponentsInChildren<Transform>(true))
        {
            child.gameObject.layer = layer;
        }
    }

    public static float ResolveExtraScale(float extraScale) =>
        extraScale <= 0f ? 1f : extraScale;

    public static float ComputeDisplayScale(GameObject root, float targetMaxSize)
    {
        if (root == null || targetMaxSize <= 0f)
        {
            return FallbackDisplayScale;
        }

        if (!TryGetMaxDimension(root, out var maxDim))
        {
            Debug.LogWarning($"ItemVisualUtility: No mesh bounds found on '{root.name}'. Using fallback scale.");
            return FallbackDisplayScale;
        }

        if (maxDim < MinRenderableSize)
        {
            return FallbackDisplayScale;
        }

        return targetMaxSize / maxDim;
    }

    static bool TryGetMaxDimension(GameObject root, out float maxDim)
    {
        maxDim = 0f;
        var found = false;

        foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            var size = renderer.localBounds.size;
            var candidate = Mathf.Max(size.x, size.y, size.z);
            if (candidate > maxDim)
            {
                maxDim = candidate;
                found = true;
            }
        }

        if (found && maxDim > MinRenderableSize)
        {
            return true;
        }

        foreach (var meshFilter in root.GetComponentsInChildren<MeshFilter>(true))
        {
            if (meshFilter.sharedMesh == null)
            {
                continue;
            }

            var size = meshFilter.sharedMesh.bounds.size;
            var candidate = Mathf.Max(size.x, size.y, size.z);
            if (candidate > maxDim)
            {
                maxDim = candidate;
                found = true;
            }
        }

        return found && maxDim > MinRenderableSize;
    }

    static void ActivateHierarchy(GameObject root)
    {
        root.SetActive(true);

        foreach (var transform in root.GetComponentsInChildren<Transform>(true))
        {
            transform.gameObject.SetActive(true);
        }
    }

    static void EnableRenderers(GameObject root)
    {
        foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            renderer.enabled = true;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }
    }

    static void StripPhysics(GameObject root)
    {
        foreach (var rigidbody in root.GetComponentsInChildren<Rigidbody>(true))
        {
            DestroyObject(rigidbody);
        }

        foreach (var collider in root.GetComponentsInChildren<Collider>(true))
        {
            DestroyObject(collider);
        }
    }

    static void DestroyObject(Object obj)
    {
        if (obj == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Object.Destroy(obj);
        }
        else
        {
            Object.DestroyImmediate(obj);
        }
    }
}
