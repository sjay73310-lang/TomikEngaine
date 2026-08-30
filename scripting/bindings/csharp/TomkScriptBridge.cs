using Tomk.Engine.Core;

namespace Tomk.Engine.Scripting;

public sealed class TomkScriptBridge : IEngineSystem
{
    public string Name => "Tomk Script Bridge";

    public void Initialize(EngineContext context)
    {
        Console.WriteLine("Tomk Script bridge initialized with C#-style engine bindings.");
    }

    public void Update(float deltaTime)
    {
        Console.WriteLine("Tomk Script update hook executed.");
    }

    public void Shutdown()
    {
        Console.WriteLine("Tomk Script bridge shutdown.");
    }
}
