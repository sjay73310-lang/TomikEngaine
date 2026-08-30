using Tomk.Engine.Core;
using Tomk.Engine.Renderer;
using Tomk.Engine.Scripting;

var host = new EngineHost("Tomk Engine");
host.RegisterSystem(new RenderSystem());
host.RegisterSystem(new TomkScriptBridge());

host.LoadProject("projects/sample-fps");
host.Run();
