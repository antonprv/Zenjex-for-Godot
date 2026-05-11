// ZenjexGodot - Zenject-like DI framework for Godot
// Created by Anton Piruev in 2026.
// Refactored from Zenjex (Unity) and SharpDI (Godot)

using System;

namespace ZenjexGodot;

/// <summary>
/// Mark fields, properties, or methods for dependency injection.
/// The DI container will automatically resolve and inject these members
/// when a node enters the scene tree.
/// 
/// Usage:
///   [Inject] private IGameService _service;
///   [Inject] public string ApiKey { get; set; }
///   [Inject] private void OnDependenciesReady(IGameService service) { }
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
public class InjectAttribute : Attribute
{
    /// <summary>
    /// Optional key for keyed dependency registration.
    /// Use this to distinguish between multiple implementations of the same interface.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    public InjectAttribute() { }

    public InjectAttribute(string key)
    {
        Key = key;
    }
}
