# ZenjexGodot - Dependency Injection Framework for Godot

A modern, Zenject-inspired dependency injection framework for Godot 4.x with C#. Designed for developers who love clean architecture, fluent APIs, and explicit dependency management.

**Created from:** Refactored Zenjex (Unity) + SharpDI (Godot) by Anton Piruev, 2026.

Tested in Godot 4.6.2 stable
---

## Features

✅ **Fluent Binding API** - Configure services with clean, chainable syntax
✅ **Single [Inject] Attribute** - One attribute for fields, properties, and methods  
✅ **Lifetime Management** - Singleton and Transient patterns  
✅ **Keyed Services** - Register multiple implementations of the same interface  
✅ **Factory Methods** - Use custom factories for complex instantiation  
✅ **Automatic Injection** - InjectableBehaviour base class handles injection automatically  
✅ **Global Composition Root** - RootInstaller provides predictable initialization order  
✅ **No Magic, No Scanning** - Explicit registration, minimal reflection overhead  

---

## Installation

1. Place the `addons/ZenjexGodot` folder into your Godot project's `addons/` directory
2. Restart Godot
3. Enable the plugin in **Project Settings → Plugins → ZenjexGodot**

---

## Quick Start

### 1. Create Your Root Installer

```csharp
using Godot;
using ZenjexGodot;

public partial class AppInstaller : RootInstaller
{
    public override void InstallBindings(DiContainerBuilder builder)
    {
        // Register services
        builder.Register<IGameService>()
            .To<GameService>()
            .AsSingleton();

        builder.Register<IInputService>()
            .To<InputService>()
            .AsSingleton();
    }

    public override async System.Threading.Tasks.Task InstallRuntimeBindings()
    {
        // Async setup: load assets, connect to servers, etc.
        await System.Threading.Tasks.Task.Delay(100);
    }

    public override void LaunchGame()
    {
        // Your game entry point
        GD.Print("[Game] Started!");
    }
}
```

2. Attach `AppInstaller` to a Node in your root scene (make it persistent across scenes)

### 2. Use Injection in Your Services

```csharp
using Godot;
using ZenjexGodot;

public partial class MyGameController : InjectableBehaviour
{
    [Inject] private IGameService _gameService;
    [Inject] private IInputService _inputService;

    protected override void OnEnter()
    {
        // _gameService and _inputService are already injected
        GD.Print(_gameService.GetGameStatus());
    }

    public override void _Process(double delta)
    {
        var inputVector = _inputService.GetMovementInput();
        // ...
    }
}
```

### 3. Manual Injection (Without Base Class)

```csharp
public partial class SomeNode : Node
{
    [Inject] private IGameService _service;

    public override void _Ready()
    {
        // Manually trigger injection
        DiContainer.Instance.Inject(this);

        // Now _service is available
        GD.Print(_service.GetGameStatus());
    }
}
```

---

## Advanced Patterns

### Keyed Bindings (Multiple Implementations)

```csharp
public override void InstallBindings(DiContainerBuilder builder)
{
    // Register multiple loggers with different keys
    builder.Register<ILogger>()
        .To<ConsoleLogger>()
        .WithKey("console")
        .AsSingleton();

    builder.Register<ILogger>()
        .To<FileLogger>()
        .WithKey("file")
        .AsSingleton();
}
```

Resolve them:
```csharp
[Inject] private ILogger _console;  // Default
[Inject(Key = "file")] private ILogger _file;  // Keyed

// Or manually:
var fileLogger = DiContainer.Instance.ResolveKeyed<ILogger>("file");
```

### Factory Methods

```csharp
public override void InstallBindings(DiContainerBuilder builder)
{
    builder.Register<IConfig>()
        .FromFactory(() => LoadConfigFromFile("config.json"))
        .AsSingleton();

    builder.Register<IRenderer>()
        .FromFactory(() => CreateRenderer())
        .AsSingleton();
}
```

### Pre-Created Instances

```csharp
var sharedConfig = new GameConfig();

public override void InstallBindings(DiContainerBuilder builder)
{
    builder.Register<IConfig>()
        .FromInstance(sharedConfig)
        .AsSingleton();
}
```

### Initialization Callbacks

Implement `IInitializable` on services that need setup after all bindings are registered:

```csharp
public class GameService : IInitializable
{
    public void Initialize()
    {
        GD.Print("GameService initialized!");
        // Load data, connect signals, etc.
    }
}

// In InstallBindings:
builder.Register<IGameService>()
    .To<GameService>()
    .BindInterfacesAndSelf()
    .AsSingleton();
```

All `IInitializable` services are called automatically after `InstallRuntimeBindings()` completes.

---

## Lifecycle & Execution Order

```
RootInstaller.Awake()
├─ Container built
├─ InstallBindings() executed
├─ OnContainerReady fired
└─ CallDeferred(InitializeRoutine)

InitializeRoutine()
├─ await InstallRuntimeBindings()
├─ Call Initialize() on IInitializable services
├─ LaunchGame() executed
└─ OnGameLaunched fired
    └─ Late injection pass (for dynamically created objects)
```

---

## API Reference

### DiContainer

```csharp
// Registration
container.Register<T>().To<TImpl>().AsSingleton();
container.RegisterInstance<T>(instance);

// Resolution
var service = container.Resolve<T>();
var service = container.ResolveKeyed<T>("key");
var services = container.ResolveAll<T>();

// Injection
container.Inject(target);

// Utilities
var exists = container.IsRegistered<T>();
var optional = container.ResolveOrNull<T>();
container.InjectTree(rootNode);
```

### BindingBuilder<T>

```csharp
builder.Register<T>()
    .To<TImpl>()           // Bind to concrete type
    .FromFactory(() => ..) // Use factory method
    .FromInstance(obj)     // Use pre-created instance
    .WithKey("name")       // Set keyed binding
    .AsSingleton()         // Register as singleton
    .AsTransient();        // Register as transient (new each time)
```

### InjectableBehaviour

```csharp
public partial class MyNode : InjectableBehaviour
{
    [Inject] private IService _service;

    protected override void OnEnter()
    {
        // Called after injection complete
    }
}
```

### Attributes

```csharp
[Inject]
private IService _field;

[Inject]
public string Property { get; set; }

[Inject]
private void OnDependenciesReady(IService service) { }

// Keyed:
[Inject(Key = "database")]
private ILogger _logger;
```

---

## Differences from Zenjex (Unity)

| Feature | Zenjex | ZenjexGodot |
|---------|--------|------------|
| **Backend** | Reflex | Built-in container |
| **Inject Attribute** | `[Zenjex]` or `[Inject]` | `[Inject]` only |
| **Base Class** | `ZenjexBehaviour` | `InjectableBehaviour` |
| **Root** | `ProjectRootInstaller` | `RootInstaller` |
| **Async Setup** | `InstallGameInstanceRoutine()` | `InstallRuntimeBindings()` |
| **Prefab Support** | `FromComponentInNewPrefab()` | Manual via Factory |
| **Scoped Lifetime** | ✅ | Planned for v1.1 |

---

## Migrating from Zenjex (Unity)

1. **Replace `[Zenjex]` with `[Inject]`** - same functionality, cleaner
2. **`ZenjexBehaviour` -> `InjectableBehaviour`** - same pattern
3. **`ProjectRootInstaller` -> `RootInstaller`** - same lifecycle
4. **Fluent API is 1:1** - binding syntax unchanged
5. **No Reflex** - faster resolution, smaller binary footprint

---

## Performance Notes

- **Singleton lookup:** O(1) dictionary access
- **Transient creation:** Standard reflection instantiation
- **Injection scan:** Once per type, cached thereafter
- **Memory overhead:** Minimal; services stored by reference

---

## Common Patterns

### Service Locator (Discouraged but Possible)

```csharp
var service = DiContainer.Instance.Resolve<IGameService>();
```

**Why avoid it?** Makes dependencies implicit. Use `[Inject]` instead.

### Cross-Scene Persistence

Keep your `AppInstaller` on a persistent node, or create a new container per scene with `DiContainerBuilder`.

### Testing

```csharp
[Test]
public void TestGameService()
{
    var mockLogger = new MockLogger();
    var builder = new DiContainerBuilder();
    
    builder.Register<ILogger>()
        .FromInstance(mockLogger)
        .AsSingleton();
    
    var container = builder.Build();
    var service = container.Resolve<GameService>();
    
    // Test service with mock
}
```

---

## Troubleshooting

**Q: "Service of type 'X' is not registered"**  
A: Make sure you called `builder.Register<X>()` in `InstallBindings()`.

**Q: Injection not happening?**  
A: Ensure `DiContainer.Instance` exists (attach `RootInstaller` first), or manually call `container.Inject(this)`.

**Q: `OnEnter()` not called?**  
A: Make sure your class inherits `InjectableBehaviour`, not just `Node`.

---

## License

MIT (or compatible). Created by Anton Piruev, 2026.

---

**Need help?** Check the examples folder or review this README's API Reference section.
