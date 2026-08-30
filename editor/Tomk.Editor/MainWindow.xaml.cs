using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Threading;

namespace Tomk.Editor;

public partial class MainWindow : Window
{
    private readonly ObservableCollection<SceneObject> _sceneObjects = new();
    private readonly Dictionary<Model3D, SceneObject> _modelToObject = new();
    private readonly Dictionary<Model3D, GizmoHit> _gizmoHits = new();
    private readonly DispatcherTimer _playTimer = new();
    private Point3D _sceneTarget = new(0, 0.8, 0);
    private Point _lastMouse;
    private Point _mouseDownPoint;
    private bool _isOrbitingScene;
    private bool _isPanningScene;
    private bool _isTransformDragging;
    private bool _isUpdatingInspector;
    private bool _isPlaying;
    private TransformTool _activeTool = TransformTool.Select;
    private Axis? _activeAxis;
    private string _currentProjectName = "SampleFps";
    private double _cameraYaw = 45;
    private double _cameraPitch = 24;
    private double _cameraDistance = 8;
    private int _objectCounter = 1;

    public MainWindow()
    {
        InitializeComponent();

        HierarchyList.ItemsSource = _sceneObjects;
        ScriptBox.Text = """
class PlayerController : Component {
    walkSpeed: float = 5.0;
    runSpeed: float = 9.0;

    fn update(delta: float) {
        let move = Input.axis("Horizontal", "Vertical");
        entity.move(move * walkSpeed * delta);
    }
}
""";

        _playTimer.Interval = TimeSpan.FromMilliseconds(16);
        _playTimer.Tick += PlayTimer_Tick;

        AddSceneObject("Ground", SceneObjectType.Plane, 0, -0.55, 0, 12, 1, 12);
        AddSceneObject("Player Cube", SceneObjectType.Cube, 0, 0, 0, 1, 1, 1);
        AddSceneObject("Target Sphere", SceneObjectType.Sphere, 2.2, 0.15, 0.8, 0.8, 0.8, 0.8);

        HierarchyList.SelectedIndex = 1;
        SetActiveTool(TransformTool.Select);
        UpdateCameras();
        RebuildViewports();
        CreateProjectStructure(_currentProjectName);
        RefreshProjectFiles();
        Log("Tomk Engine Editor started.");
        Log("Scene supports click selection, XYZ transform gizmo, inspector editing, and Game View.");
    }

    private void AddSceneObject(string name, SceneObjectType type, double x, double y, double z, double sx, double sy, double sz)
    {
        _sceneObjects.Add(new SceneObject
        {
            Name = name,
            Type = type,
            X = x,
            Y = y,
            Z = z,
            ScaleX = sx,
            ScaleY = sy,
            ScaleZ = sz
        });
    }

    private void RebuildViewports()
    {
        SceneViewport.Children.Clear();
        GameViewport.Children.Clear();
        _modelToObject.Clear();
        _gizmoHits.Clear();

        GameViewport.Camera = new PerspectiveCamera
        {
            Position = new Point3D(0, 2.2, -6.5),
            LookDirection = new Vector3D(0, -0.2, 1),
            UpDirection = new Vector3D(0, 1, 0),
            FieldOfView = 62
        };

        AddLights(SceneViewport);
        AddLights(GameViewport);

        foreach (var sceneObject in _sceneObjects)
        {
            var sceneModel = BuildModel(sceneObject, sceneObject == HierarchyList.SelectedItem);
            _modelToObject[sceneModel] = sceneObject;
            SceneViewport.Children.Add(new ModelVisual3D { Content = sceneModel });
            GameViewport.Children.Add(new ModelVisual3D { Content = BuildModel(sceneObject, false) });
        }

        if (HierarchyList.SelectedItem is SceneObject selected)
        {
            SceneViewport.Children.Add(new ModelVisual3D { Content = BuildGizmo(selected, _activeTool) });
        }
    }

    private static void AddLights(Viewport3D viewport)
    {
        var lights = new Model3DGroup();
        lights.Children.Add(new AmbientLight(Color.FromRgb(80, 86, 96)));
        lights.Children.Add(new DirectionalLight(Color.FromRgb(235, 241, 255), new Vector3D(-0.35, -0.8, -0.45)));
        viewport.Children.Add(new ModelVisual3D { Content = lights });
    }

    private static GeometryModel3D BuildModel(SceneObject sceneObject, bool selected)
    {
        var mesh = sceneObject.Type switch
        {
            SceneObjectType.Sphere => MeshFactory.CreateSphere(0.55, 24, 16),
            SceneObjectType.Plane => MeshFactory.CreatePlane(1),
            _ => MeshFactory.CreateCube(1)
        };

        var color = sceneObject.Type switch
        {
            SceneObjectType.Plane => Color.FromRgb(67, 76, 70),
            SceneObjectType.Sphere => Color.FromRgb(87, 157, 214),
            _ => Color.FromRgb(212, 178, 86)
        };

        if (selected)
        {
            color = Color.FromRgb(79, 180, 119);
        }

        var material = new MaterialGroup();
        material.Children.Add(new DiffuseMaterial(new SolidColorBrush(color)));
        material.Children.Add(new SpecularMaterial(new SolidColorBrush(Color.FromRgb(220, 230, 240)), 22));

        var transform = new Transform3DGroup();
        transform.Children.Add(new ScaleTransform3D(sceneObject.ScaleX, sceneObject.ScaleY, sceneObject.ScaleZ));
        transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(1, 0, 0), sceneObject.RotationX)));
        transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), sceneObject.RotationY)));
        transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 1), sceneObject.RotationZ)));
        transform.Children.Add(new TranslateTransform3D(sceneObject.X, sceneObject.Y, sceneObject.Z));

        return new GeometryModel3D(mesh, material)
        {
            BackMaterial = material,
            Transform = transform
        };
    }

    private Model3DGroup BuildGizmo(SceneObject sceneObject, TransformTool tool)
    {
        var group = new Model3DGroup();
        var length = tool == TransformTool.Scale ? 1.8 : 1.55;
        var thickness = tool == TransformTool.Rotate ? 0.045 : 0.035;
        var origin = new Point3D(sceneObject.X, sceneObject.Y, sceneObject.Z);

        AddGizmoPart(group, BuildAxisBar(origin, Axis.X, length, thickness, Color.FromRgb(223, 89, 89)), sceneObject, Axis.X);
        AddGizmoPart(group, BuildAxisBar(origin, Axis.Y, length, thickness, Color.FromRgb(79, 180, 119)), sceneObject, Axis.Y);
        AddGizmoPart(group, BuildAxisBar(origin, Axis.Z, length, thickness, Color.FromRgb(87, 157, 224)), sceneObject, Axis.Z);

        if (tool == TransformTool.Scale)
        {
            AddGizmoPart(group, BuildAxisHandle(origin, Axis.X, length, Color.FromRgb(223, 89, 89)), sceneObject, Axis.X);
            AddGizmoPart(group, BuildAxisHandle(origin, Axis.Y, length, Color.FromRgb(79, 180, 119)), sceneObject, Axis.Y);
            AddGizmoPart(group, BuildAxisHandle(origin, Axis.Z, length, Color.FromRgb(87, 157, 224)), sceneObject, Axis.Z);
        }

        return group;
    }

    private void AddGizmoPart(Model3DGroup group, GeometryModel3D model, SceneObject sceneObject, Axis axis)
    {
        _gizmoHits[model] = new GizmoHit(sceneObject, axis);
        group.Children.Add(model);
    }

    private static GeometryModel3D BuildAxisBar(Point3D origin, Axis axis, double length, double thickness, Color color)
    {
        var material = new DiffuseMaterial(new SolidColorBrush(color));
        var transform = new Transform3DGroup();

        switch (axis)
        {
            case Axis.X:
                transform.Children.Add(new ScaleTransform3D(length, thickness, thickness));
                transform.Children.Add(new TranslateTransform3D(origin.X + length / 2, origin.Y, origin.Z));
                break;
            case Axis.Y:
                transform.Children.Add(new ScaleTransform3D(thickness, length, thickness));
                transform.Children.Add(new TranslateTransform3D(origin.X, origin.Y + length / 2, origin.Z));
                break;
            case Axis.Z:
                transform.Children.Add(new ScaleTransform3D(thickness, thickness, length));
                transform.Children.Add(new TranslateTransform3D(origin.X, origin.Y, origin.Z + length / 2));
                break;
        }

        return new GeometryModel3D(MeshFactory.CreateCube(1), material)
        {
            BackMaterial = material,
            Transform = transform
        };
    }

    private static GeometryModel3D BuildAxisHandle(Point3D origin, Axis axis, double distance, Color color)
    {
        var material = new DiffuseMaterial(new SolidColorBrush(color));
        var transform = new Transform3DGroup();
        transform.Children.Add(new ScaleTransform3D(0.16, 0.16, 0.16));

        var offset = axis switch
        {
            Axis.X => new Vector3D(distance, 0, 0),
            Axis.Y => new Vector3D(0, distance, 0),
            _ => new Vector3D(0, 0, distance)
        };

        transform.Children.Add(new TranslateTransform3D(origin.X + offset.X, origin.Y + offset.Y, origin.Z + offset.Z));

        return new GeometryModel3D(MeshFactory.CreateCube(1), material)
        {
            BackMaterial = material,
            Transform = transform
        };
    }

    private void UpdateCameras()
    {
        var yaw = _cameraYaw * Math.PI / 180;
        var pitch = _cameraPitch * Math.PI / 180;
        var x = _sceneTarget.X + _cameraDistance * Math.Cos(pitch) * Math.Sin(yaw);
        var y = _sceneTarget.Y + _cameraDistance * Math.Sin(pitch);
        var z = _sceneTarget.Z + _cameraDistance * Math.Cos(pitch) * Math.Cos(yaw);

        var cameraPosition = new Point3D(x, y, z);
        SceneViewport.Camera = new PerspectiveCamera
        {
            Position = cameraPosition,
            LookDirection = _sceneTarget - cameraPosition,
            UpDirection = new Vector3D(0, 1, 0),
            FieldOfView = 55
        };
    }

    private void FillInspector(SceneObject? sceneObject)
    {
        _isUpdatingInspector = true;

        if (sceneObject is null)
        {
            NameBox.Text = "";
            TypeBox.Text = "";
            PosXBox.Text = "";
            PosYBox.Text = "";
            PosZBox.Text = "";
            RotXBox.Text = "";
            RotYBox.Text = "";
            RotZBox.Text = "";
            ScaleXBox.Text = "";
            ScaleYBox.Text = "";
            ScaleZBox.Text = "";
        }
        else
        {
            NameBox.Text = sceneObject.Name;
            TypeBox.Text = sceneObject.Type.ToString();
            PosXBox.Text = Format(sceneObject.X);
            PosYBox.Text = Format(sceneObject.Y);
            PosZBox.Text = Format(sceneObject.Z);
            RotXBox.Text = Format(sceneObject.RotationX);
            RotYBox.Text = Format(sceneObject.RotationY);
            RotZBox.Text = Format(sceneObject.RotationZ);
            ScaleXBox.Text = Format(sceneObject.ScaleX);
            ScaleYBox.Text = Format(sceneObject.ScaleY);
            ScaleZBox.Text = Format(sceneObject.ScaleZ);
        }

        _isUpdatingInspector = false;
    }

    private void ApplyInspector()
    {
        if (_isUpdatingInspector || HierarchyList.SelectedItem is not SceneObject selected)
        {
            return;
        }

        selected.Name = string.IsNullOrWhiteSpace(NameBox.Text) ? selected.Name : NameBox.Text.Trim();
        selected.X = ReadDouble(PosXBox, selected.X);
        selected.Y = ReadDouble(PosYBox, selected.Y);
        selected.Z = ReadDouble(PosZBox, selected.Z);
        selected.RotationX = ReadDouble(RotXBox, selected.RotationX);
        selected.RotationY = ReadDouble(RotYBox, selected.RotationY);
        selected.RotationZ = ReadDouble(RotZBox, selected.RotationZ);
        selected.ScaleX = Math.Max(0.05, ReadDouble(ScaleXBox, selected.ScaleX));
        selected.ScaleY = Math.Max(0.05, ReadDouble(ScaleYBox, selected.ScaleY));
        selected.ScaleZ = Math.Max(0.05, ReadDouble(ScaleZBox, selected.ScaleZ));

        HierarchyList.Items.Refresh();
        RebuildViewports();
        StatusText.Text = $"Updated {selected.Name}";
    }

    private void SetActiveTool(TransformTool tool)
    {
        _activeTool = tool;
        ToolHelpText.Text = $"Tool: {tool}  |  W/E/R tools  |  Hold RMB + WASD/QZ fly  |  Del delete";
        StatusText.Text = $"{tool} tool";

        var normal = new SolidColorBrush(Color.FromRgb(48, 54, 64));
        var active = new SolidColorBrush(Color.FromRgb(79, 180, 119));
        SelectToolButton.Background = tool == TransformTool.Select ? active : normal;
        MoveToolButton.Background = tool == TransformTool.Move ? active : normal;
        RotateToolButton.Background = tool == TransformTool.Rotate ? active : normal;
        ScaleToolButton.Background = tool == TransformTool.Scale ? active : normal;

        RebuildViewports();
    }

    private (GizmoHit? Gizmo, SceneObject? Object) PickScene(Point position)
    {
        GizmoHit? pickedGizmo = null;
        SceneObject? pickedObject = null;

        VisualTreeHelper.HitTest(
            SceneViewport,
            null,
            result =>
            {
                if (result is RayHitTestResult rayResult)
                {
                    if (_gizmoHits.TryGetValue(rayResult.ModelHit, out var gizmoHit))
                    {
                        pickedGizmo = gizmoHit;
                        return HitTestResultBehavior.Stop;
                    }

                    if (pickedObject is null && _modelToObject.TryGetValue(rayResult.ModelHit, out var sceneObject))
                    {
                        pickedObject = sceneObject;
                    }
                }

                return HitTestResultBehavior.Continue;
            },
            new PointHitTestParameters(position));

        return (pickedGizmo, pickedObject);
    }

    private void SelectObject(SceneObject sceneObject)
    {
        HierarchyList.SelectedItem = sceneObject;
        FillInspector(sceneObject);
        RebuildViewports();
        StatusText.Text = $"Selected {sceneObject.Name}";
        Log($"Selected {sceneObject.Name} from Scene View.");
    }

    private void TransformSelectedByDrag(Vector delta)
    {
        if (HierarchyList.SelectedItem is not SceneObject selected)
        {
            return;
        }

        switch (_activeTool)
        {
            case TransformTool.Move:
                MoveSelectedOnAxis(selected, delta);
                break;
            case TransformTool.Rotate:
                RotateSelectedOnAxis(selected, delta);
                break;
            case TransformTool.Scale:
                ScaleSelectedOnAxis(selected, delta);
                break;
        }

        FillInspector(selected);
        HierarchyList.Items.Refresh();
        RebuildViewports();
    }

    private void MoveSelectedOnAxis(SceneObject selected, Vector delta)
    {
        switch (_activeAxis)
        {
            case Axis.X:
                selected.X += delta.X * 0.02;
                break;
            case Axis.Y:
                selected.Y -= delta.Y * 0.02;
                break;
            case Axis.Z:
                selected.Z += delta.Y * 0.02;
                break;
            default:
                selected.X += delta.X * 0.015;
                selected.Z += delta.Y * 0.015;
                break;
        }
    }

    private void RotateSelectedOnAxis(SceneObject selected, Vector delta)
    {
        var amount = (Math.Abs(delta.X) > Math.Abs(delta.Y) ? delta.X : -delta.Y) * 0.65;

        switch (_activeAxis)
        {
            case Axis.X:
                selected.RotationX = (selected.RotationX + amount) % 360;
                break;
            case Axis.Y:
                selected.RotationY = (selected.RotationY + amount) % 360;
                break;
            case Axis.Z:
                selected.RotationZ = (selected.RotationZ + amount) % 360;
                break;
            default:
                selected.RotationY = (selected.RotationY + delta.X * 0.45) % 360;
                selected.RotationX = Math.Clamp(selected.RotationX + delta.Y * 0.45, -180, 180);
                break;
        }
    }

    private void ScaleSelectedOnAxis(SceneObject selected, Vector delta)
    {
        var change = (delta.X - delta.Y) * 0.01;

        switch (_activeAxis)
        {
            case Axis.X:
                selected.ScaleX = Math.Max(0.05, selected.ScaleX + change);
                break;
            case Axis.Y:
                selected.ScaleY = Math.Max(0.05, selected.ScaleY + change);
                break;
            case Axis.Z:
                selected.ScaleZ = Math.Max(0.05, selected.ScaleZ + change);
                break;
            default:
                selected.ScaleX = Math.Max(0.05, selected.ScaleX + change);
                selected.ScaleY = Math.Max(0.05, selected.ScaleY + change);
                selected.ScaleZ = Math.Max(0.05, selected.ScaleZ + change);
                break;
        }
    }

    private void PanSceneCamera(Vector delta)
    {
        var camera = SceneViewport.Camera as PerspectiveCamera;
        if (camera is null)
        {
            return;
        }

        var forward = camera.LookDirection;
        forward.Normalize();
        var right = Vector3D.CrossProduct(forward, camera.UpDirection);
        right.Normalize();
        var up = camera.UpDirection;
        up.Normalize();

        var scale = _cameraDistance * 0.0018;
        var movement = (-right * delta.X + up * delta.Y) * scale;
        _sceneTarget += movement;
        UpdateCameras();
    }

    private void MoveSceneCamera(double localX, double localY, double localZ)
    {
        var camera = SceneViewport.Camera as PerspectiveCamera;
        if (camera is null)
        {
            return;
        }

        var forward = camera.LookDirection;
        forward.Normalize();
        var right = Vector3D.CrossProduct(forward, camera.UpDirection);
        right.Normalize();
        var up = camera.UpDirection;
        up.Normalize();

        var movement = (right * localX) + (up * localY) + (forward * localZ);
        _sceneTarget += movement;
        UpdateCameras();
    }

    private void FrameSelected()
    {
        if (HierarchyList.SelectedItem is not SceneObject selected)
        {
            return;
        }

        _sceneTarget = new Point3D(selected.X, selected.Y, selected.Z);
        _cameraDistance = Math.Clamp(Math.Max(selected.ScaleX, Math.Max(selected.ScaleY, selected.ScaleZ)) * 5, 3, 18);
        UpdateCameras();
        StatusText.Text = $"Framed {selected.Name}";
    }

    private static string EngineRootPath()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "tomk.engine.json")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return AppContext.BaseDirectory;
    }

    private string CurrentProjectPath()
    {
        return Path.Combine(EngineRootPath(), "projects", _currentProjectName);
    }

    private void CreateProjectStructure(string projectName)
    {
        _currentProjectName = SanitizeProjectName(projectName);
        ProjectNameBox.Text = _currentProjectName;

        var projectPath = CurrentProjectPath();
        Directory.CreateDirectory(Path.Combine(projectPath, "assets", "models"));
        Directory.CreateDirectory(Path.Combine(projectPath, "assets", "materials"));
        Directory.CreateDirectory(Path.Combine(projectPath, "assets", "textures"));
        Directory.CreateDirectory(Path.Combine(projectPath, "objects"));
        Directory.CreateDirectory(Path.Combine(projectPath, "scenes"));
        Directory.CreateDirectory(Path.Combine(projectPath, "scripts"));

        var projectFile = Path.Combine(projectPath, "project.tomkproject");
        if (!File.Exists(projectFile))
        {
            File.WriteAllText(projectFile, $"project \"{_currentProjectName}\" {{\n    startupScene: \"scenes/main.scene.tomk\";\n}}\n");
        }

        var sceneFile = Path.Combine(projectPath, "scenes", "main.scene.tomk");
        if (!File.Exists(sceneFile))
        {
            File.WriteAllText(sceneFile, SceneSerializer.Serialize(_sceneObjects));
        }
    }

    private void RefreshProjectFiles()
    {
        ProjectFilesList.Items.Clear();
        var projectPath = CurrentProjectPath();
        if (!Directory.Exists(projectPath))
        {
            return;
        }

        foreach (var file in Directory.GetFiles(projectPath, "*", SearchOption.AllDirectories).OrderBy(file => file))
        {
            ProjectFilesList.Items.Add(Path.GetRelativePath(projectPath, file));
        }
    }

    private static string SanitizeProjectName(string projectName)
    {
        var cleaned = string.Join("_", projectName.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries)).Trim();
        return string.IsNullOrWhiteSpace(cleaned) ? "NewTomkProject" : cleaned;
    }

    private static string Format(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);

    private static double ReadDouble(TextBox box, double fallback)
    {
        return double.TryParse(box.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ? value : fallback;
    }

    private void Log(string message)
    {
        ConsoleBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
        ConsoleBox.ScrollToEnd();
    }

    private void HierarchyList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        FillInspector(HierarchyList.SelectedItem as SceneObject);
        RebuildViewports();
    }

    private void Inspector_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!_isUpdatingInspector)
        {
            StatusText.Text = "Inspector changed";
        }
    }

    private void ApplyInspectorButton_Click(object sender, RoutedEventArgs e)
    {
        ApplyInspector();
        Log("Inspector values applied.");
    }

    private void SelectToolButton_Click(object sender, RoutedEventArgs e)
    {
        SetActiveTool(TransformTool.Select);
    }

    private void MoveToolButton_Click(object sender, RoutedEventArgs e)
    {
        SetActiveTool(TransformTool.Move);
    }

    private void RotateToolButton_Click(object sender, RoutedEventArgs e)
    {
        SetActiveTool(TransformTool.Rotate);
    }

    private void ScaleToolButton_Click(object sender, RoutedEventArgs e)
    {
        SetActiveTool(TransformTool.Scale);
    }

    private void AddCubeButton_Click(object sender, RoutedEventArgs e)
    {
        AddObjectFromEditor(SceneObjectType.Cube);
    }

    private void AddSphereButton_Click(object sender, RoutedEventArgs e)
    {
        AddObjectFromEditor(SceneObjectType.Sphere);
    }

    private void AddPlaneButton_Click(object sender, RoutedEventArgs e)
    {
        AddObjectFromEditor(SceneObjectType.Plane);
    }

    private void AddObjectFromEditor(SceneObjectType type)
    {
        var name = $"{type} {_objectCounter++}";
        AddSceneObject(name, type, _objectCounter * 0.6, 0, 0, 1, 1, 1);
        HierarchyList.SelectedIndex = _sceneObjects.Count - 1;
        RebuildViewports();
        Log($"Added {name}.");
    }

    private void DuplicateButton_Click(object sender, RoutedEventArgs e)
    {
        if (HierarchyList.SelectedItem is not SceneObject selected)
        {
            return;
        }

        var duplicate = selected.Clone($"{selected.Name} Copy");
        duplicate.X += 0.8;
        _sceneObjects.Add(duplicate);
        HierarchyList.SelectedItem = duplicate;
        RebuildViewports();
        Log($"Duplicated {selected.Name}.");
    }

    private void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        DeleteSelectedObject();
    }

    private void DeleteSelectedObject()
    {
        if (HierarchyList.SelectedItem is not SceneObject selected)
        {
            return;
        }

        _sceneObjects.Remove(selected);
        HierarchyList.SelectedIndex = _sceneObjects.Count > 0 ? 0 : -1;
        RebuildViewports();
        Log($"Deleted {selected.Name}.");
    }

    private void CreateNewProjectButton_Click(object sender, RoutedEventArgs e)
    {
        CreateProjectFromUi();
    }

    private void CreateNewProjectMenuItem_Click(object sender, RoutedEventArgs e)
    {
        CreateProjectFromUi();
    }

    private void CreateProjectFromUi()
    {
        CreateProjectStructure(ProjectNameBox.Text);
        SaveSceneToCurrentProject("main.scene.tomk");
        RefreshProjectFiles();
        Log($"Project folder ready: {CurrentProjectPath()}");
        StatusText.Text = $"Project: {_currentProjectName}";
    }

    private void NewObjectMenuItem_Click(object sender, RoutedEventArgs e)
    {
        CreateProjectStructure(ProjectNameBox.Text);
        var name = $"Object_{_objectCounter++}";
        var objectPath = Path.Combine(CurrentProjectPath(), "objects", $"{name}.tomkobj");
        File.WriteAllText(objectPath, $"object \"{name}\" {{\n    mesh: \"Cube\";\n    script: \"scripts/{name}.tomk\";\n}}\n");
        AddSceneObject(name, SceneObjectType.Cube, _objectCounter * 0.4, 0, 0, 1, 1, 1);
        HierarchyList.SelectedIndex = _sceneObjects.Count - 1;
        RefreshProjectFiles();
        RebuildViewports();
        Log($"Created object file: {objectPath}");
    }

    private void NewScriptMenuItem_Click(object sender, RoutedEventArgs e)
    {
        CreateProjectStructure(ProjectNameBox.Text);
        var className = $"TomkBehaviour{DateTime.Now:HHmmss}";
        var scriptPath = Path.Combine(CurrentProjectPath(), "scripts", $"{className}.tomk");
        File.WriteAllText(scriptPath, $"class {className} : Component {{\n    fn start() {{\n    }}\n\n    fn update(delta: float) {{\n    }}\n}}\n");
        RefreshProjectFiles();
        Log($"Created script: {scriptPath}");
    }

    private void RefreshProjectFilesButton_Click(object sender, RoutedEventArgs e)
    {
        CreateProjectStructure(ProjectNameBox.Text);
        RefreshProjectFiles();
        Log("Project files refreshed.");
    }

    private void FrameSelectedMenuItem_Click(object sender, RoutedEventArgs e)
    {
        FrameSelected();
    }

    private void ResetCameraMenuItem_Click(object sender, RoutedEventArgs e)
    {
        _sceneTarget = new Point3D(0, 0.8, 0);
        _cameraYaw = 45;
        _cameraPitch = 24;
        _cameraDistance = 8;
        UpdateCameras();
        StatusText.Text = "Camera reset";
    }

    private void PlayButton_Click(object sender, RoutedEventArgs e)
    {
        _isPlaying = true;
        _playTimer.Start();
        ViewTabs.SelectedIndex = 1;
        GameOverlayText.Text = "Game View running";
        StatusText.Text = "Play mode";
        Log("Play mode started.");
    }

    private void PauseButton_Click(object sender, RoutedEventArgs e)
    {
        _isPlaying = false;
        _playTimer.Stop();
        GameOverlayText.Text = "Game View paused";
        StatusText.Text = "Paused";
        Log("Play mode paused.");
    }

    private void SaveSceneButton_Click(object sender, RoutedEventArgs e)
    {
        var scenePath = SaveSceneToCurrentProject("editor.scene.tomk");
        Log($"Scene saved: {scenePath}");
        StatusText.Text = "Scene saved";
    }

    private string SaveSceneToCurrentProject(string fileName)
    {
        CreateProjectStructure(ProjectNameBox.Text);
        var scenePath = Path.Combine(CurrentProjectPath(), "scenes", fileName);
        Directory.CreateDirectory(Path.GetDirectoryName(scenePath)!);
        File.WriteAllText(scenePath, SceneSerializer.Serialize(_sceneObjects));
        RefreshProjectFiles();
        return scenePath;
    }

    private void BuildGameButton_Click(object sender, RoutedEventArgs e)
    {
        Log("Build requested. Use the generated Release exe for the editor, then add exporter pipeline next.");
        StatusText.Text = "Build pipeline ready";
    }

    private void PlayTimer_Tick(object? sender, EventArgs e)
    {
        if (!_isPlaying)
        {
            return;
        }

        foreach (var sceneObject in _sceneObjects.Where(item => item.Type != SceneObjectType.Plane))
        {
            sceneObject.RotationY = (sceneObject.RotationY + 0.6) % 360;
        }

        FillInspector(HierarchyList.SelectedItem as SceneObject);
        RebuildViewports();
    }

    private void SceneViewSurface_MouseDown(object sender, MouseButtonEventArgs e)
    {
        SceneViewSurface.Focus();
        _lastMouse = e.GetPosition(SceneViewport);
        _mouseDownPoint = _lastMouse;

        if (e.ChangedButton == MouseButton.Right)
        {
            _isOrbitingScene = true;
            SceneViewSurface.CaptureMouse();
            e.Handled = true;
            return;
        }

        if (e.ChangedButton == MouseButton.Middle)
        {
            _isPanningScene = true;
            SceneViewSurface.CaptureMouse();
            e.Handled = true;
            return;
        }

        if (e.ChangedButton == MouseButton.Left)
        {
            var hit = PickScene(_lastMouse);
            if (hit.Gizmo is not null)
            {
                SelectObject(hit.Gizmo.SceneObject);
                _activeAxis = hit.Gizmo.Axis;
                _isTransformDragging = _activeTool != TransformTool.Select;
                SceneViewSurface.CaptureMouse();
                StatusText.Text = $"{_activeTool} {hit.Gizmo.Axis}";
            }
            else if (hit.Object is not null)
            {
                SelectObject(hit.Object);
                _activeAxis = null;
                _isTransformDragging = _activeTool != TransformTool.Select;
                SceneViewSurface.CaptureMouse();
            }
            else
            {
                _activeAxis = null;
                StatusText.Text = "Scene View focused";
            }

            e.Handled = true;
        }
    }

    private void SceneViewSurface_MouseMove(object sender, MouseEventArgs e)
    {
        var current = e.GetPosition(SceneViewport);
        var delta = current - _lastMouse;
        _lastMouse = current;

        if (_isOrbitingScene)
        {
            _cameraYaw += delta.X * 0.35;
            _cameraPitch = Math.Clamp(_cameraPitch - delta.Y * 0.25, -75, 75);
            UpdateCameras();
        }
        else if (_isPanningScene)
        {
            PanSceneCamera(delta);
        }
        else if (_isTransformDragging)
        {
            TransformSelectedByDrag(delta);
        }
    }

    private void SceneViewSurface_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left && !_isTransformDragging)
        {
            var clickDelta = e.GetPosition(SceneViewport) - _mouseDownPoint;
            if (Math.Abs(clickDelta.X) < 4 && Math.Abs(clickDelta.Y) < 4)
            {
                var hit = PickScene(e.GetPosition(SceneViewport));
                if (hit.Object is not null)
                {
                    SelectObject(hit.Object);
                }
            }
        }

        _isOrbitingScene = false;
        _isPanningScene = false;
        _isTransformDragging = false;
        _activeAxis = null;
        SceneViewSurface.ReleaseMouseCapture();
    }

    private void SceneViewSurface_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        _cameraDistance = Math.Clamp(_cameraDistance - e.Delta * 0.008, 2.5, 30);
        UpdateCameras();
        e.Handled = true;
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (Keyboard.FocusedElement is TextBox)
        {
            return;
        }

        switch (e.Key)
        {
            case Key.W:
                if (_isOrbitingScene)
                {
                    MoveSceneCamera(0, 0, 0.25);
                }
                else
                {
                    SetActiveTool(TransformTool.Move);
                }
                break;
            case Key.E:
                SetActiveTool(TransformTool.Rotate);
                break;
            case Key.R:
                SetActiveTool(TransformTool.Scale);
                break;
            case Key.Escape:
                SetActiveTool(TransformTool.Select);
                break;
            case Key.Delete:
                DeleteSelectedObject();
                break;
            case Key.A:
                if (_isOrbitingScene)
                {
                    MoveSceneCamera(-0.25, 0, 0);
                }
                break;
            case Key.S:
                if (_isOrbitingScene)
                {
                    MoveSceneCamera(0, 0, -0.25);
                }
                break;
            case Key.D when Keyboard.Modifiers.HasFlag(ModifierKeys.Control):
                DuplicateButton_Click(sender, e);
                break;
            case Key.D:
                if (_isOrbitingScene)
                {
                    MoveSceneCamera(0.25, 0, 0);
                }
                break;
            case Key.Q:
                if (_isOrbitingScene)
                {
                    MoveSceneCamera(0, -0.25, 0);
                }
                break;
            case Key.Z:
                if (_isOrbitingScene)
                {
                    MoveSceneCamera(0, 0.25, 0);
                }
                break;
            case Key.F:
                FrameSelected();
                break;
            case Key.Left:
                NudgeSelected(-0.1, 0, 0);
                break;
            case Key.Right:
                NudgeSelected(0.1, 0, 0);
                break;
            case Key.Up:
                NudgeSelected(0, 0, -0.1);
                break;
            case Key.Down:
                NudgeSelected(0, 0, 0.1);
                break;
            case Key.PageUp:
                NudgeSelected(0, 0.1, 0);
                break;
            case Key.PageDown:
                NudgeSelected(0, -0.1, 0);
                break;
        }
    }

    private void NudgeSelected(double x, double y, double z)
    {
        if (HierarchyList.SelectedItem is not SceneObject selected)
        {
            return;
        }

        selected.X += x;
        selected.Y += y;
        selected.Z += z;
        FillInspector(selected);
        RebuildViewports();
        StatusText.Text = $"Moved {selected.Name}";
    }
}

public enum TransformTool
{
    Select,
    Move,
    Rotate,
    Scale
}

public enum Axis
{
    X,
    Y,
    Z
}

public sealed record GizmoHit(SceneObject SceneObject, Axis Axis);

public enum SceneObjectType
{
    Cube,
    Sphere,
    Plane
}

public sealed class SceneObject
{
    public string Name { get; set; } = "Object";
    public SceneObjectType Type { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public double Z { get; set; }
    public double RotationX { get; set; }
    public double RotationY { get; set; }
    public double RotationZ { get; set; }
    public double ScaleX { get; set; } = 1;
    public double ScaleY { get; set; } = 1;
    public double ScaleZ { get; set; } = 1;

    public SceneObject Clone(string name)
    {
        return new SceneObject
        {
            Name = name,
            Type = Type,
            X = X,
            Y = Y,
            Z = Z,
            RotationX = RotationX,
            RotationY = RotationY,
            RotationZ = RotationZ,
            ScaleX = ScaleX,
            ScaleY = ScaleY,
            ScaleZ = ScaleZ
        };
    }
}

public static class SceneSerializer
{
    public static string Serialize(IEnumerable<SceneObject> sceneObjects)
    {
        using var writer = new StringWriter(CultureInfo.InvariantCulture);
        writer.WriteLine("scene \"Editor Scene\" {");

        foreach (var sceneObject in sceneObjects)
        {
            writer.WriteLine($"    entity \"{sceneObject.Name}\" {{");
            writer.WriteLine($"        type: {sceneObject.Type};");
            writer.WriteLine($"        position: Vector3({sceneObject.X:0.###}, {sceneObject.Y:0.###}, {sceneObject.Z:0.###});");
            writer.WriteLine($"        rotation: Vector3({sceneObject.RotationX:0.###}, {sceneObject.RotationY:0.###}, {sceneObject.RotationZ:0.###});");
            writer.WriteLine($"        scale: Vector3({sceneObject.ScaleX:0.###}, {sceneObject.ScaleY:0.###}, {sceneObject.ScaleZ:0.###});");
            writer.WriteLine("    }");
            writer.WriteLine();
        }

        writer.WriteLine("}");
        return writer.ToString();
    }
}

public static class MeshFactory
{
    public static MeshGeometry3D CreateCube(double size)
    {
        var h = size / 2;
        var points = new Point3DCollection
        {
            new(-h, -h, -h), new(h, -h, -h), new(h, h, -h), new(-h, h, -h),
            new(-h, -h, h), new(h, -h, h), new(h, h, h), new(-h, h, h)
        };

        var triangles = new Int32Collection
        {
            0, 2, 1, 0, 3, 2,
            4, 5, 6, 4, 6, 7,
            0, 1, 5, 0, 5, 4,
            2, 3, 7, 2, 7, 6,
            1, 2, 6, 1, 6, 5,
            0, 4, 7, 0, 7, 3
        };

        return new MeshGeometry3D
        {
            Positions = points,
            TriangleIndices = triangles
        };
    }

    public static MeshGeometry3D CreatePlane(double size)
    {
        var h = size / 2;
        return new MeshGeometry3D
        {
            Positions = new Point3DCollection
            {
                new(-h, 0, -h),
                new(h, 0, -h),
                new(h, 0, h),
                new(-h, 0, h)
            },
            TriangleIndices = new Int32Collection { 0, 1, 2, 0, 2, 3 }
        };
    }

    public static MeshGeometry3D CreateSphere(double radius, int longitudeSegments, int latitudeSegments)
    {
        var mesh = new MeshGeometry3D();

        for (var lat = 0; lat <= latitudeSegments; lat++)
        {
            var theta = lat * Math.PI / latitudeSegments;
            var sinTheta = Math.Sin(theta);
            var cosTheta = Math.Cos(theta);

            for (var lon = 0; lon <= longitudeSegments; lon++)
            {
                var phi = lon * 2 * Math.PI / longitudeSegments;
                var x = radius * Math.Cos(phi) * sinTheta;
                var y = radius * cosTheta;
                var z = radius * Math.Sin(phi) * sinTheta;
                mesh.Positions.Add(new Point3D(x, y, z));
            }
        }

        for (var lat = 0; lat < latitudeSegments; lat++)
        {
            for (var lon = 0; lon < longitudeSegments; lon++)
            {
                var first = lat * (longitudeSegments + 1) + lon;
                var second = first + longitudeSegments + 1;
                mesh.TriangleIndices.Add(first);
                mesh.TriangleIndices.Add(second);
                mesh.TriangleIndices.Add(first + 1);
                mesh.TriangleIndices.Add(second);
                mesh.TriangleIndices.Add(second + 1);
                mesh.TriangleIndices.Add(first + 1);
            }
        }

        return mesh;
    }
}
