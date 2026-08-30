namespace Tomk.Engine.Fps;

public sealed class FpsController
{
    public float WalkSpeed { get; set; } = 5.0f;
    public float RunSpeed { get; set; } = 9.0f;
    public float MouseSensitivity { get; set; } = 0.12f;

    public void Move(float x, float z, float deltaTime)
    {
        var speed = WalkSpeed * deltaTime;
        Console.WriteLine($"FPS move vector: {x * speed:0.00}, {z * speed:0.00}");
    }

    public void Look(float mouseX, float mouseY)
    {
        Console.WriteLine($"FPS look delta: {mouseX * MouseSensitivity:0.00}, {mouseY * MouseSensitivity:0.00}");
    }
}
