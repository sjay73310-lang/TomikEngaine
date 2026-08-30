namespace Tomk.Engine.Editor.Viewport;

public sealed class ViewportPanel
{
    public string ActiveTool { get; private set; } = "Move";

    public void SetTool(string toolName)
    {
        ActiveTool = toolName;
        Console.WriteLine($"Viewport tool selected: {ActiveTool}");
    }

    public void FocusSelection()
    {
        Console.WriteLine("Viewport focused on selected 3D object.");
    }
}
