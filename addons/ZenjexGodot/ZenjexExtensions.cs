// ZenjexGodot - Zenject-like DI framework for Godot

using Godot;
using System.Collections.Generic;

namespace ZenjexGodot;

/// <summary>
/// Extension methods for DiContainer and related types.
/// </summary>
public static class ZenjexExtensions
{
    /// <summary>
    /// Register multiple interfaces from a single implementation.
    /// Example: Register&lt;IService&gt;().BindInterfacesAndSelf().To&lt;ServiceImpl&gt;()
    /// </summary>
    public static BindingBuilder<T> BindInterfacesAndSelf<T>(this BindingBuilder<T> builder)
        where T : class
    {
        // For now, this is a placeholder. In a full implementation,
        // this would register both the interface and all implemented interfaces.
        return builder;
    }

    /// <summary>
    /// Attempt to resolve a service, returning null if not found instead of throwing.
    /// </summary>
    public static T ResolveOrNull<T>(this DiContainer container, string key = "") where T : class
    {
        try
        {
            return container.Resolve<T>(key);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Inject dependencies into a node and all its children.
    /// </summary>
    public static void InjectTree(this DiContainer container, Node root)
    {
        if (root == null)
            return;

        container.Inject(root);

        foreach (Node child in root.GetChildren())
        {
            container.InjectTree(child);
        }
    }

    /// <summary>
    /// Create a new instance and inject its dependencies.
    /// </summary>
    public static T CreateAndInject<T>(this DiContainer container) where T : class, new()
    {
        var instance = new T();
        container.Inject(instance);
        return instance;
    }

    /// <summary>
    /// Check if a service is registered.
    /// </summary>
    public static bool IsRegistered<T>(this DiContainer container, string key = "") where T : class
    {
        return container.TryResolve<T>(out _, key);
    }
}

/// <summary>
/// Utilities for working with Godot-specific DI patterns.
/// </summary>
public static class GodotDiUtils
{
    /// <summary>
    /// Automatically wire up all InjectableBehaviour nodes in a scene tree.
    /// Call this from your RootInstaller.LaunchGame() if needed.
    /// </summary>
    public static void InjectSceneTree(Node root)
    {
        if (DiContainer.Instance == null)
        {
            GD.PushError("[ZenjexGodot] No DiContainer instance available");
            return;
        }

        InjectNode(root);
    }

    private static void InjectNode(Node node)
    {
        // Inject this node
        DiContainer.Instance.Inject(node);

        // Recursively inject children
        foreach (Node child in node.GetChildren())
        {
            InjectNode(child);
        }
    }

    /// <summary>
    /// Create a scoped container for a subscene or level.
    /// This creates a child container that inherits parent bindings.
    /// </summary>
    public static DiContainer CreateScopedContainer(this DiContainer parent)
    {
        // For now, return the parent. A full implementation would create
        // a true child container with inherited bindings.
        return parent;
    }

    /// <summary>
    /// Register all nodes of a specific type in a scene subtree.
    /// Useful for discovering managers, services, etc.
    /// </summary>
    public static void RegisterNodesOfType<T>(
        this DiContainerBuilder builder,
        Node sceneRoot,
        string key = "")
        where T : class
    {
        var nodes = FindNodesOfType<T>(sceneRoot);

        if (nodes.Count == 0)
        {
            GD.PushWarning($"[ZenjexGodot] No nodes of type {typeof(T).Name} found in scene");
            return;
        }

        if (nodes.Count == 1)
        {
            // Single instance - register as is
            builder.RecordInstance(nodes[0] as T, key);
        }
        else
        {
            // Multiple instances - register each with unique key or as list
            GD.Print($"[ZenjexGodot] Found {nodes.Count} nodes of type {typeof(T).Name}");
        }
    }

    private static List<Node> FindNodesOfType<T>(Node root) where T : class
    {
        var results = new List<Node>();
        CollectNodesOfType<T>(root, results);
        return results;
    }

    private static void CollectNodesOfType<T>(Node node, List<Node> results) where T : class
    {
        if (node is T)
            results.Add(node);

        foreach (Node child in node.GetChildren())
        {
            CollectNodesOfType<T>(child, results);
        }
    }
}
