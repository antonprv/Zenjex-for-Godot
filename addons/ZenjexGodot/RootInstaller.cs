// ZenjexGodot - Zenject-like DI framework for Godot

using System;
using System.Collections.Generic;

using Godot;

namespace ZenjexGodot;

/// <summary>
/// Abstract base class for the global composition root.
/// Create one concrete subclass, attach it to a Node in your scene,
/// and override the abstract methods to configure your DI bindings.
///
/// Lifecycle (execution order):
/// 1. InstallBindings() — register all global services
/// 2. OnContainerReady — first injection pass (optional custom hook)
/// 3. InstallRuntimeBindings() — async setup, load assets, register runtime bindings
/// 4. OnInitializables() — call Initialize() on all IInitializable services
/// 5. LaunchGame() — user entry point (start game state, etc.)
/// 6. OnGameLaunched — second injection pass for late-bound services
/// 
/// Usage:
///   public partial class AppInstaller : RootInstaller
///   {
///       public override void InstallBindings(DiContainerBuilder builder)
///       {
///           builder.Register&lt;IInputService&gt;()
///               .To&lt;InputService&gt;()
///               .AsSingleton();
///       }
///
///       public override void LaunchGame()
///       {
///           // Start your game here
///       }
///   }
/// </summary>
[GlobalClass]
public abstract partial class RootInstaller : Node
{
    /// <summary>The global root container instance.</summary>
    public static DiContainer Container { get; private set; }

    /// <summary>Fired after the container is built and bindings are registered.</summary>
    public static event Action OnContainerReady;

    /// <summary>Fired after all initialization is complete and the game is launched.</summary>
    public static event Action OnGameLaunched;

    private bool _initialized;

    public override void _Ready()
    {
        if (_initialized)
            return;

        _initialized = true;

        // Create and configure container
        var builder = new DiContainerBuilder();

        // User setup
        InstallBindings(builder);

        // Build container and make it globally available
        Container = builder.Build();

        // Fire ready event
        OnContainerReady?.Invoke();

        // Trigger a deferred initialization routine
        CallDeferred(MethodName.InitializeRoutine);
    }

    private async void InitializeRoutine()
    {
        // Allow async setup
        await InstallRuntimeBindings();

        // Call IInitializable services
        CallInitializable();

        // User launch point
        LaunchGame();

        // Fire launch event for late bindings
        OnGameLaunched?.Invoke();
    }

    #region Abstract Methods

    /// <summary>Register all global services here.</summary>
    public abstract void InstallBindings(DiContainerBuilder builder);

    /// <summary>Async setup: load assets, configure runtime services, etc.</summary>
    public virtual async System.Threading.Tasks.Task InstallRuntimeBindings()
    {
        await System.Threading.Tasks.Task.CompletedTask;
    }

    /// <summary>Called after all setup. Start your game here.</summary>
    public abstract void LaunchGame();

    #endregion

    #region Helpers

    private static void CallInitializable()
    {
        if (Container == null)
            return;

        // Try to resolve IInitializable implementations
        try
        {
            var initializables = Container.ResolveAll<IInitializable>();
            foreach (var initializable in initializables)
            {
                try
                {
                    initializable.Initialize();
                }
                catch (Exception ex)
                {
                    GD.PushError($"[ZenjexGodot] IInitializable.Initialize() failed on {initializable.GetType().Name}: {ex}");
                }
            }
        }
        catch
        {
            // No IInitializable services registered
        }
    }

    #endregion
}

/// <summary>
/// Fluent builder for the dependency container configuration.
/// Used within RootInstaller.InstallBindings() to register services.
/// </summary>
public sealed class DiContainerBuilder
{
    private readonly List<(Type ServiceType, Type ImplementationType, ServiceLifetime Lifetime, string Key)> _typeBindings = new();
    private readonly List<(Type ServiceType, object Instance, string Key)> _instances = new();
    private readonly List<(Type ServiceType, Type ImplementationType, Delegate Factory, ServiceLifetime Lifetime, string Key)> _factories = new();

    /// <summary>Start a new binding fluent chain.</summary>
    public BindingBuilder<T> Register<T>() where T : class
    {
        return new BindingBuilder<T>(this);
    }

    /// <summary>Register an instance directly.</summary>
    public void RegisterInstance<T>(T instance, string key = "") where T : class
    {
        _instances.Add((typeof(T), instance, key));
    }

    /// <summary>Build the container from all registered bindings.</summary>
    public DiContainer Build()
    {
        // Create the global container (will initialize itself as Instance)
        var container = new DiContainer();

        // Register all instances first (they're direct values)
        foreach (var (serviceType, instance, key) in _instances)
        {
            if (string.IsNullOrEmpty(key))
            {
                container.RegisterInstance(instance);
            }
            else
            {
                container.RegisterInstance(key, instance);
            }
        }

        // Register all type bindings
        foreach (var (serviceType, implType, lifetime, key) in _typeBindings)
        {
            RegisterServiceBinding(container, serviceType, implType, lifetime, key);
        }

        // Register all factory bindings
        foreach (var (serviceType, implType, factory, lifetime, key) in _factories)
        {
            RegisterFactoryBinding(container, serviceType, implType, factory, lifetime, key);
        }

        return container;
    }

    /// <summary>Internal: record a type binding for later building.</summary>
    internal void RecordBinding(Type serviceType, Type implType, ServiceLifetime lifetime, string key)
    {
        _typeBindings.Add((serviceType, implType, lifetime, key));
    }

    /// <summary>Internal: record a factory binding for later building.</summary>
    internal void RecordFactoryBinding<T>(Type serviceType, Type implType, Delegate factory, ServiceLifetime lifetime, string key)
        where T : class
    {
        _factories.Add((serviceType, implType, factory, lifetime, key));
    }

    /// <summary>Internal: record an instance for later building.</summary>
    internal void RecordInstance<T>(T instance, string key = "") where T : class
    {
        _instances.Add((typeof(T), instance, key));
    }

    #region Helpers

    private static void RegisterServiceBinding(
        DiContainer container,
        Type serviceType,
        Type implType,
        ServiceLifetime lifetime,
        string key)
    {
        var method = container.GetType().GetMethod(
            "RegisterService",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (method != null)
        {
            var genMethod = method.MakeGenericMethod(serviceType, implType);
            genMethod.Invoke(container, new object[] { lifetime, null, null, key });
        }
    }

    private static void RegisterFactoryBinding(
        DiContainer container,
        Type serviceType,
        Type implType,
        Delegate factory,
        ServiceLifetime lifetime,
        string key)
    {
        var method = container.GetType().GetMethod(
            "RegisterService",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (method != null)
        {
            var genMethod = method.MakeGenericMethod(serviceType, implType);
            genMethod.Invoke(container, new object[] { lifetime, null, factory, key });
        }
    }

    #endregion
}

/// <summary>Optional interface for services that need explicit initialization.</summary>
public interface IInitializable
{
    void Initialize();
}
