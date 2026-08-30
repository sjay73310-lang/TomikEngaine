using Tomk.Engine.Core;

namespace Tomk.Engine.Animation;

public sealed class RigSystem : IEngineSystem
{
    public string Name => "Rig And Animation";

    public void Initialize(EngineContext context)
    {
        Console.WriteLine("Rig system ready for skeletons, bones, and animation clips.");
    }

    public void Update(float deltaTime)
    {
    }

    public void Shutdown()
    {
    }
}
