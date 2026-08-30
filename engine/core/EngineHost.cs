namespace Tomk.Engine.Core;

public interface IEngineSystem
{
    string Name { get; }
    void Initialize(EngineContext context);
    void Update(float deltaTime);
    void Shutdown();
}

public sealed class EngineContext
{
    public string EngineName { get; }
    public string? ProjectPath { get; private set; }

    public EngineContext(string engineName)
    {
        EngineName = engineName;
    }

    public void SetProject(string projectPath)
    {
        ProjectPath = projectPath;
    }
}

public sealed class EngineHost
{
    private readonly List<IEngineSystem> _systems = new();
    private readonly EngineContext _context;

    public EngineHost(string engineName)
    {
        _context = new EngineContext(engineName);
    }

    public void RegisterSystem(IEngineSystem system)
    {
        _systems.Add(system);
    }

    public void LoadProject(string projectPath)
    {
        _context.SetProject(projectPath);
        Console.WriteLine($"Loaded project: {projectPath}");
    }

    public void Run()
    {
        foreach (var system in _systems)
        {
            system.Initialize(_context);
        }

        Console.WriteLine($"{_context.EngineName} runtime started.");

        const float fixedDelta = 1.0f / 60.0f;
        foreach (var system in _systems)
        {
            system.Update(fixedDelta);
        }

        foreach (var system in Enumerable.Reverse(_systems))
        {
            system.Shutdown();
        }
    }
}
