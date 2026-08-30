using Tomk.Engine.Core;

namespace Tomk.Engine.Renderer;

public sealed class RenderSystem : IEngineSystem
{
    public string Name => "Renderer";

    public void Initialize(EngineContext context)
    {
        Console.WriteLine("Renderer initialized for 3D viewport.");
    }

    public void Update(float deltaTime)
    {
        Console.WriteLine($"Renderer frame tick: {deltaTime:0.000}s");
    }

    public void Shutdown()
    {
        Console.WriteLine("Renderer shutdown.");
    }
}
