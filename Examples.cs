// ZenjexGodot - Example Usage
// This example demonstrates a complete DI setup in Godot

using Godot;
using ZenjexGodot;

// ============================================================================
// INTERFACES & SERVICES
// ============================================================================

/// <summary>Game configuration interface.</summary>
public interface IGameConfig
{
    string GameTitle { get; }
    int TargetFPS { get; }
}

/// <summary>Player input interface.</summary>
public interface IInputService
{
    Vector2 GetMovementInput();
    bool IsJumpPressed();
}

/// <summary>Logging interface.</summary>
public interface ILogger
{
    void Log(string message);
    void LogError(string message);
}

// ============================================================================
// IMPLEMENTATIONS
// ============================================================================

public partial class GameConfig : IGameConfig
{
    public string GameTitle => "My Godot Game";
    public int TargetFPS => 60;
}

public partial class InputService : InjectableBehaviour, IInputService
{
    public Vector2 GetMovementInput()
    {
        var input = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
        return input;
    }

    public bool IsJumpPressed() => Input.IsActionJustPressed("ui_accept");
}

public partial class ConsoleLogger : ILogger
{
    public void Log(string message) => GD.Print($"[LOG] {message}");
    public void LogError(string message) => GD.PrintErr($"[ERROR] {message}");
}

// ============================================================================
// GAME SERVICE (depends on other services)
// ============================================================================

public partial class GameService : IInitializable
{
    [Inject] private IGameConfig _config;
    [Inject] private ILogger _logger;

    public void Initialize()
    {
        _logger.Log($"Game initialized: {_config.GameTitle} @ {_config.TargetFPS} FPS");
    }

    public string GetStatus()
    {
        return $"Game running at {_config.TargetFPS} FPS";
    }
}

// ============================================================================
// ROOT INSTALLER (composition root)
// ============================================================================

/// <summary>
/// Main application installer. Attach this to a persistent node in your root scene.
/// </summary>
public partial class AppInstaller : RootInstaller
{
    [Export] public bool EnableLogging = true;

    public override void InstallBindings(DiContainerBuilder builder)
    {
        // Register configuration
        var config = new GameConfig();
        builder.Register<IGameConfig>()
            .FromInstance(config)
            .AsSingleton();

        // Register input service
        builder.Register<IInputService>()
            .To<InputService>()
            .AsSingleton();

        // Register logger (only if enabled)
        if (EnableLogging)
        {
            builder.Register<ILogger>()
                .To<ConsoleLogger>()
                .AsSingleton();
        }

        // Register game service
        builder.Register<GameService>()
            .AsSingleton();

        // Register IInitializable services
        builder.Register<IInitializable>()
            .To<GameService>()
            .AsSingleton();

        GD.Print("[AppInstaller] Bindings registered");
    }

    public override async System.Threading.Tasks.Task InstallRuntimeBindings()
    {
        // Async setup: load resources, connect to servers, etc.
        GD.Print("[AppInstaller] Installing runtime bindings...");
        await System.Threading.Tasks.Task.Delay(500);
        GD.Print("[AppInstaller] Runtime bindings installed");
    }

    public override void LaunchGame()
    {
        GD.Print("[AppInstaller] LaunchGame() called - starting your game!");
        // Transition to first scene, start state machine, etc.
    }
}

// ============================================================================
// EXAMPLE COMPONENTS (that use injection)
// ============================================================================

/// <summary>
/// Example component that uses injected dependencies.
/// </summary>
public partial class PlayerController : InjectableBehaviour
{
    [Inject] private IInputService _inputService;
    [Inject] private ILogger _logger;

    private Vector2 _velocity;
    private const float Speed = 200f;

    protected override void OnEnter()
    {
        _logger.Log("PlayerController ready");
    }

    public override void _Process(double delta)
    {
        var input = _inputService.GetMovementInput();
        _velocity = input * Speed;

        Position += (Vector3)(_velocity * (float)delta);

        if (_inputService.IsJumpPressed())
        {
            _logger.Log("Jump!");
        }
    }
}

/// <summary>
/// Another component using injection.
/// </summary>
public partial class HudDisplay : InjectableBehaviour
{
    [Inject] private IGameConfig _config;
    [Inject] private GameService _gameService;

    private Label _statusLabel;

    protected override void OnEnter()
    {
        _statusLabel = GetNode<Label>("StatusLabel");
    }

    public override void _Process(double delta)
    {
        _statusLabel.Text = _gameService.GetStatus();
    }
}

// ============================================================================
// MANUAL INJECTION EXAMPLE (without base class)
// ============================================================================

/// <summary>
/// Example of manual injection without inheriting from InjectableBehaviour.
/// </summary>
public partial class ManualService : Node
{
    [Inject] private ILogger _logger;

    public override void _Ready()
    {
        // Manually trigger injection
        DiContainer.Instance.Inject(this);

        _logger.Log("Manual service ready!");
    }
}

// ============================================================================
// TEST SETUP EXAMPLE
// ============================================================================

/// <summary>
/// Example showing how to set up tests with different bindings.
/// </summary>
public class GameServiceTest
{
    public static void RunTest()
    {
        // Create a builder for testing
        var builder = new DiContainerBuilder();

        // Register test doubles
        builder.Register<ILogger>()
            .FromFactory(() => new ConsoleLogger())
            .AsSingleton();

        // Build container
        // var container = builder.Build();

        // Now you can test with the container
        // var service = container.Resolve<GameService>();
    }
}
