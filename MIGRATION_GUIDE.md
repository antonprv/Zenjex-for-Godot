# Migration Guide: Zenjex (Unity) → ZenjexGodot (Godot)

This guide helps you port your Zenjex-based Unity projects to ZenjexGodot on Godot 4.x.

---

## Overview of Changes

| Aspect | Unity (Zenjex) | Godot (ZenjexGodot) | Migration Notes |
|--------|----------------|-------------------|-----------------|
| **Framework** | Reflex-based | Built-in container | No external DI lib needed |
| **Inject Attribute** | `[Zenjex]` or `[Inject]` | `[Inject]` only | Simplifies everything |
| **Base Class** | `ZenjexBehaviour : MonoBehaviour` | `InjectableBehaviour : Node` | Direct equivalent |
| **Root Installer** | `ProjectRootInstaller : MonoBehaviour` | `RootInstaller : Node` | Direct equivalent |
| **Binding API** | Fluent (`builder.Bind<>()`) | Fluent (`builder.Register<>()`) | 99% compatible |
| **Container** | `RootContext.Resolve()` | `DiContainer.Instance.Resolve()` | Slightly more explicit |
| **Keyed Services** | `[Inject] IService service` (attribute key) | `[Inject(Key = "name")] IService service` | Same pattern |
| **Async Setup** | `InstallGameInstanceRoutine()` | `InstallRuntimeBindings()` async | Cleaner async/await |
| **Initialization** | `IInitializable` interface | `IInitializable` interface | 100% compatible |

---

## Step-by-Step Migration

### 1. Replace `[Zenjex]` with `[Inject]`

**Before (Zenjex/Unity):**
```csharp
public class MyService : ZenjexBehaviour
{
    [Zenjex] private IGameService _service;
    [Zenjex] private ILogger _logger;
}
```

**After (ZenjexGodot/Godot):**
```csharp
public partial class MyService : InjectableBehaviour
{
    [Inject] private IGameService _service;
    [Inject] private ILogger _logger;
}
```

**Notes:**
- Add `partial` keyword (required for Godot C# reflection)
- One attribute instead of two (`[Zenjex]` + `[Inject]`)

---

### 2. Rename Base Classes

**Before:**
```csharp
public class MyComponent : ZenjexBehaviour
{
    protected override void OnAwake() { }
}
```

**After:**
```csharp
public partial class MyComponent : InjectableBehaviour
{
    protected override void OnEnter() { }
}
```

**Timing differences:**
- `ZenjexBehaviour.OnAwake()` → called after injection in Awake phase
- `InjectableBehaviour.OnEnter()` → called after injection in _Ready phase

Godot's node lifecycle is different from Unity's MonoBehaviour:
```
Unity:        Godot:
_Ready()      _Ready()    <- Injection happens here (call OnEnter)
               _EnterTree()
```

---

### 3. Migrate the Root Installer

**Before (Unity/Zenjex):**
```csharp
public class AppInstaller : ProjectRootInstaller
{
    [SerializeField] private CurtainPrefab _curtain;

    public override void InstallBindings(ContainerBuilder builder)
    {
        builder.Bind<IGameService>()
            .To<GameService>()
            .AsSingle();

        builder.Bind<ICurtainService>()
            .FromComponentInNewPrefab(_curtain)
            .AsSingle();
    }

    public override IEnumerator InstallGameInstanceRoutine()
    {
        // Load Addressables, etc.
        yield return null;
    }

    public override void LaunchGame()
    {
        SceneManager.LoadScene("MainMenu");
    }
}
```

**After (Godot/ZenjexGodot):**
```csharp
public partial class AppInstaller : RootInstaller
{
    [Export] private PackedScene _curtainPrefab;

    public override void InstallBindings(DiContainerBuilder builder)
    {
        builder.Register<IGameService>()
            .To<GameService>()
            .AsSingleton();

        // For prefab instances, create and register manually
        var curtainNode = _curtainPrefab.Instantiate<CurtainService>();
        builder.RegisterInstance<ICurtainService>(curtainNode);
    }

    public override async System.Threading.Tasks.Task InstallRuntimeBindings()
    {
        // Use async/await instead of coroutines
        // Load resources asynchronously
        await System.Threading.Tasks.Task.Delay(100);
    }

    public override void LaunchGame()
    {
        GetTree().ChangeSceneToFile("res://scenes/MainMenu.tscn");
    }
}
```

**Mapping:**
- `ProjectRootInstaller` → `RootInstaller`
- `ContainerBuilder` → `DiContainerBuilder`
- `.AsSingle()` → `.AsSingleton()`
- `.Bind<T>()` → `.Register<T>()`
- `IEnumerator` → `async Task`
- `SceneManager.LoadScene()` → `GetTree().ChangeSceneToFile()`
- `Addressables` → Use GodotResourceLoader or Godot's built-in asset system

---

### 4. Update Method Names

| Zenjex | ZenjexGodot | Notes |
|--------|------------|-------|
| `builder.Bind<T>()` | `builder.Register<T>()` | Same fluent API |
| `.To<Impl>()` | `.To<Impl>()` | Identical |
| `.AsSingle()` | `.AsSingleton()` | Clearer naming |
| `.AsTransient()` | `.AsTransient()` | Identical |
| `.FromInstance()` | `.FromInstance()` | Identical |
| `.FromFactory()` | `.FromFactory()` | Identical |
| `.WithArguments()` | ❌ Not yet implemented | Use factories instead |
| `RootContext.Resolve()` | `DiContainer.Instance.Resolve()` | More explicit |
| `.HasBinding<T>()` | `.IsRegistered<T>()` | Godot style |

---

### 5. Keyed Bindings

**Before:**
```csharp
builder.Bind<ILogger>()
    .To<FileLogger>()
    .AsSingle();

// Usage
[Inject] private ILogger _logger;  // Gets latest

// For specific key, you'd use Zenject's context system
```

**After:**
```csharp
builder.Register<ILogger>()
    .To<FileLogger>()
    .WithKey("file")
    .AsSingleton();

builder.Register<ILogger>()
    .To<ConsoleLogger>()
    .WithKey("console")
    .AsSingleton();

// Usage
[Inject(Key = "file")] private ILogger _fileLogger;
[Inject(Key = "console")] private ILogger _consoleLogger;

// Or manually:
var fileLogger = DiContainer.Instance.ResolveKeyed<ILogger>("file");
```

---

### 6. Interface and Service Definitions

**No changes needed** — interfaces remain identical:

```csharp
// This works the same in both
public interface IGameService
{
    void StartGame();
}

public class GameService : IGameService
{
    public void StartGame() { }
}
```

---

### 7. IInitializable Services

**Zenjex:**
```csharp
public class ConfigService : IInitializable
{
    public void Initialize()
    {
        // Called after all bindings registered
    }
}

// Register
builder.Bind<ConfigService>()
    .AsSingle();
```

**ZenjexGodot:**
```csharp
public partial class ConfigService : IInitializable
{
    public void Initialize()
    {
        // Called after all bindings registered
    }
}

// Register (same)
builder.Register<ConfigService>()
    .AsSingleton();
```

**100% identical pattern** — no changes needed.

---

### 8. Handling Godot-Specific Patterns

#### Exporting Inspector Variables

**Zenjex:**
```csharp
public class MyComponent : ZenjexBehaviour
{
    [SerializeField] private int _speed = 10;
    [Zenjex] private IGameService _service;
}
```

**ZenjexGodot:**
```csharp
public partial class MyComponent : InjectableBehaviour
{
    [Export] public int Speed = 10;
    [Inject] private IGameService _service;
}
```

---

#### Node Tree Navigation

**Zenjex (Unity):**
```csharp
public class GameController : MonoBehaviour
{
    private UIController _ui;

    void Awake()
    {
        _ui = GetComponentInChildren<UIController>();
    }
}
```

**ZenjexGodot (Godot):**
```csharp
public partial class GameController : InjectableBehaviour
{
    private UIController _ui;

    protected override void OnEnter()
    {
        _ui = GetNode<UIController>("UIContainer/UIController");
        // Or use @onready if you prefer
    }
}
```

---

## Common Patterns Comparison

### Pattern 1: Simple Service Registration

**Zenjex:**
```csharp
builder.Bind<IInputService>()
    .To<InputService>()
    .AsSingle();
```

**ZenjexGodot:**
```csharp
builder.Register<IInputService>()
    .To<InputService>()
    .AsSingleton();
```

✅ **100% compatible** (just rename `.AsSingle()` → `.AsSingleton()`)

---

### Pattern 2: Instance Binding

**Zenjex:**
```csharp
var config = LoadConfig();
builder.BindInstance(config);
```

**ZenjexGodot:**
```csharp
var config = LoadConfig();
builder.Register<Config>()
    .FromInstance(config)
    .AsSingleton();
```

✅ **Slightly more verbose but clearer**

---

### Pattern 3: Factory Binding

**Zenjex:**
```csharp
builder.Bind<IDatabase>()
    .FromFactory(() => new Database(connectionString))
    .AsSingle();
```

**ZenjexGodot:**
```csharp
builder.Register<IDatabase>()
    .FromFactory(() => new Database(connectionString))
    .AsSingleton();
```

✅ **Identical**

---

## Troubleshooting Migration Issues

### Issue: "Inject attribute not found"

**Solution:** Make sure you're using:
```csharp
using ZenjexGodot;

[Inject] private IService _service;
```

Not:
```csharp
using Zenjex.Extensions.Attribute;
[Zenjex] private IService _service;  // ❌ Wrong
```

---

### Issue: "OnEnter() not called"

**Solution:** Make sure your class inherits `InjectableBehaviour`:

```csharp
public partial class MyClass : InjectableBehaviour  // ✅ Correct
{
    protected override void OnEnter() { }
}
```

Not just `Node`:
```csharp
public partial class MyClass : Node  // ❌ No auto-injection
{
    [Inject] private IService _service;
}
```

---

### Issue: "Service of type 'X' is not registered"

**Solution:** Make sure you called `Register<T>()` in `InstallBindings()`:

```csharp
public override void InstallBindings(DiContainerBuilder builder)
{
    builder.Register<IMyService>()
        .To<MyService>()
        .AsSingleton();  // ✅ Must terminate with lifetime method
}
```

---

## Not Yet Implemented (Godot Limitations)

- **Constructor Injection** — Constructor parameter resolution (use Factory instead)
- **Prefab Pooling** — Automatic pool management (manage manually with factories)
- **Scoped Lifetime** — Per-scene container scoping (planned for v1.1)
- **Conditional Binding** — Platform/build-config conditional registration (use if statements)
- **Decorators/Wrappers** — Auto-wrapping interceptors (manual approach instead)

---

## Checklist for Full Migration

- [ ] Replace `[Zenjex]` with `[Inject]` everywhere
- [ ] Replace `ZenjexBehaviour` with `InjectableBehaviour`
- [ ] Replace `OnAwake()` with `OnEnter()`
- [ ] Replace `ProjectRootInstaller` with `RootInstaller`
- [ ] Rename `.AsSingle()` to `.AsSingleton()`
- [ ] Convert `IEnumerator` coroutines to `async Task`
- [ ] Update keyed binding syntax: `[Inject] MyField` → `[Inject(Key = "name")]`
- [ ] Replace Zenject-specific patterns with Godot equivalents
- [ ] Test injection in all services
- [ ] Verify initialization order with `OnInitializables`
- [ ] Handle Godot-specific patterns (Export, GetNode, etc.)

---

## Getting Help

- **README.md** — API reference and basic usage
- **Examples.cs** — Complete working example with patterns
- **Source Code** — All classes are well-commented

Happy migrating! 🚀
