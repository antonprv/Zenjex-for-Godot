// ZenjexGodot - Zenject-like DI framework for Godot

using Godot;

namespace ZenjexGodot;

/// <summary>
/// Optional base class for nodes that use [Inject] dependency injection.
/// Automatically injects all [Inject]-marked fields, properties, and methods
/// before OnEnter() is called.
/// 
/// Usage:
///   public partial class MyService : InjectableBehaviour
///   {
///       [Inject] private IGameService _service;
///       
///       protected override void OnEnter()
///       {
///           // _service is already injected here
///       }
///   }
/// </summary>
[GlobalClass]
public partial class InjectableBehaviour : Node
{
    /// <summary>
    /// Called after injection is complete. Override instead of _Ready() to guarantee
    /// that all [Inject] fields are populated.
    /// </summary>
    protected virtual void OnEnter() { }

    public override void _Ready()
    {
        // Inject dependencies before calling OnEnter
        if (DiContainer.Instance != null)
        {
            DiContainer.Instance.Inject(this);
        }
        else
        {
            GD.PushWarning($"[ZenjexGodot] DiContainer is not ready when {GetType().Name}._Ready() was called. " +
                "Make sure you have a DiContainer instance in the scene.");
        }

        OnEnter();
    }
}
