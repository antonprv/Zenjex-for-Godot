// ZenjexGodot - Zenject-like DI framework for Godot

using System;

namespace ZenjexGodot;

/// <summary>
/// Fluent builder for service bindings. Chain methods to configure a service registration,
/// then terminate with a lifetime method (AsSingleton, AsTransient).
/// 
/// Patterns:
///   // Simple binding
///   builder.Register&lt;IService&gt;().To&lt;ServiceImpl&gt;().AsSingleton();
///   
///   // Instance
///   builder.Register&lt;IConfig&gt;().FromInstance(config).AsSingleton();
///   
///   // Factory
///   builder.Register&lt;IService&gt;().FromFactory(() => new ServiceImpl()).AsSingleton();
///   
///   // Keyed
///   builder.Register&lt;ILogger&gt;().To&lt;FileLogger&gt;().WithKey("file").AsSingleton();
/// </summary>
public sealed class BindingBuilder<T> where T : class
{
    private readonly DiContainerBuilder _builder;
    private Type _implementationType;
    private T _instance;
    private Func<T> _factory;
    private string _key = string.Empty;
    private bool _isCommitted;

    internal BindingBuilder(DiContainerBuilder builder)
    {
        _builder = builder;
        _implementationType = typeof(T);
    }

    #region Source Configuration

    /// <summary>Bind interface/base-class to a concrete implementation.</summary>
    public BindingBuilder<T> To<TImpl>() where TImpl : class, T
    {
        _implementationType = typeof(TImpl);
        return this;
    }

    /// <summary>Bind to a pre-created instance (singleton).</summary>
    public BindingBuilder<T> FromInstance(T instance)
    {
        _instance = instance;
        _factory = null;
        return this;
    }

    /// <summary>Bind using a factory method.</summary>
    public BindingBuilder<T> FromFactory(Func<T> factory)
    {
        _factory = factory;
        _instance = null;
        return this;
    }

    /// <summary>Register with a key for keyed resolution.</summary>
    public BindingBuilder<T> WithKey(string key)
    {
        _key = key;
        return this;
    }

    #endregion

    #region Lifetime Configuration

    /// <summary>Register as a singleton (one instance, reused).</summary>
    public void AsSingleton()
    {
        EnsureNotCommitted();
        CommitBinding(ServiceLifetime.Singleton);
    }

    /// <summary>Register as transient (new instance each time).</summary>
    public void AsTransient()
    {
        EnsureNotCommitted();
        CommitBinding(ServiceLifetime.Transient);
    }

    #endregion

    #region Implementation

    private void CommitBinding(ServiceLifetime lifetime)
    {
        if (_isCommitted)
            throw new InvalidOperationException("Binding already committed.");

        _isCommitted = true;

        // Record the binding in the builder
        if (_instance != null)
        {
            _builder.RecordInstance(_instance, _key);
        }
        else if (_factory != null)
        {
            _builder.RecordFactoryBinding<T>(typeof(T), _implementationType, _factory, lifetime, _key);
        }
        else
        {
            _builder.RecordBinding(typeof(T), _implementationType, lifetime, _key);
        }
    }

    private void EnsureNotCommitted()
    {
        if (_isCommitted)
            throw new InvalidOperationException("Binding is already committed. Do not call lifetime methods multiple times.");
    }

    #endregion
}