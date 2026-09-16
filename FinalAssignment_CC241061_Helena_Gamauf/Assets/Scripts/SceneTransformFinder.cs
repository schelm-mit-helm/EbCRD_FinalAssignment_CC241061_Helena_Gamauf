using UnityEngine;

public static class SceneTransformFinder
{
    public static Transform FindRecursive(string objectName)
    {
        foreach (var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
        {
            var found = FindChildRecursive(root.transform, objectName, false);
            if (found != null)
                return found;
        }

        return null;
    }

    public static Transform FindRecursiveIgnoreCase(string objectName)
    {
        foreach (var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
        {
            var found = FindChildRecursive(root.transform, objectName, true);
            if (found != null)
                return found;
        }

        return null;
    }

    public static Transform FindByNameContains(string namePart)
    {
        if (string.IsNullOrWhiteSpace(namePart))
            return null;

        foreach (var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
        {
            var found = FindChildContainsRecursive(root.transform, namePart);
            if (found != null)
                return found;
        }

        return null;
    }

    static Transform FindChildRecursive(Transform parent, string objectName, bool ignoreCase)
    {
        var comparison = ignoreCase
            ? System.StringComparison.OrdinalIgnoreCase
            : System.StringComparison.Ordinal;

        if (string.Equals(parent.name.Trim(), objectName, comparison))
            return parent;

        for (var i = 0; i < parent.childCount; i++)
        {
            var found = FindChildRecursive(parent.GetChild(i), objectName, ignoreCase);
            if (found != null)
                return found;
        }

        return null;
    }

    public static Transform FindInHierarchy(Transform root, string objectName, bool ignoreCase = false)
    {
        if (root == null)
            return null;

        return FindChildRecursive(root, objectName, ignoreCase);
    }

    public static Transform FindInHierarchyContains(Transform root, string namePart)
    {
        if (root == null || string.IsNullOrWhiteSpace(namePart))
            return null;

        return FindChildContainsRecursive(root, namePart);
    }

    public static Transform FindPlayer()
    {
        var taggedPlayer = GameObject.FindGameObjectWithTag("Player");
        if (taggedPlayer != null)
            return taggedPlayer.transform;

        var movement = Object.FindFirstObjectByType<PlayerMovement>(FindObjectsInactive.Exclude);
        if (movement != null)
            return movement.transform;

        return GameObject.Find("Player")?.transform;
    }

    static Transform FindChildContainsRecursive(Transform parent, string namePart)
    {
        if (parent.name.IndexOf(namePart, System.StringComparison.OrdinalIgnoreCase) >= 0)
            return parent;

        for (var i = 0; i < parent.childCount; i++)
        {
            var found = FindChildContainsRecursive(parent.GetChild(i), namePart);
            if (found != null)
                return found;
        }

        return null;
    }
}
