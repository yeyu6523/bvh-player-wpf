using BvhPlayer.Core;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Vec = System.Numerics.Vector3;

namespace BvhPlayer.App;

/// <summary>Native, hardware-accelerated WPF 3D. Meshes and materials are shared and frozen;
/// only per-joint transforms change while playing.</summary>
public sealed class SkeletonView : Grid
{
    private readonly Viewport3D viewport = new();
    private readonly PerspectiveCamera camera = new() { FieldOfView = 40, NearPlaneDistance = 0.02, FarPlaneDistance = 2000 };
    private readonly Model3DGroup skeleton = new();
    private readonly Model3DGroup floor = new();
    private readonly List<MatrixTransform3D> joints = [];
    private readonly List<MatrixTransform3D> bones = [];
    private readonly List<int> boneIndices = [];
    private readonly List<Material> originalMaterials = [];
    private readonly List<GeometryModel3D> jointModels = [];
    private readonly MeshGeometry3D sphere = MakeSphere();
    private readonly MeshGeometry3D cylinder = MakeCylinder();
    private readonly Material left = MakeMaterial("#54D8DA");
    private readonly Material right = MakeMaterial("#FFBE74");
    private readonly Material center = MakeMaterial("#DEE7F2");
    private readonly Material selected = MakeMaterial("#8DFFAE");
    private BvhDocument? clip;
    private double scale = 1, groundY, extent = 3;
    private double yaw = 25, pitch = 12, distance = 9;
    private Point3D target = new(0, 1.35, 0);
    private Point last;
    private bool orbiting, panning;
    private bool follow = true;

    public SkeletonView()
    {
        ClipToBounds = true;
        Background = Brushes.Transparent;
        viewport.Camera = camera;
        IsHitTestVisible = true;
        var world = new Model3DGroup();
        world.Children.Add(new AmbientLight(Color.FromRgb(137, 150, 170)));
        world.Children.Add(new DirectionalLight(Colors.White, new Vector3D(-1, -2, -3)));
        world.Children.Add(new DirectionalLight(Color.FromRgb(130, 170, 210), new Vector3D(1, -1, 2)));
        world.Children.Add(floor); world.Children.Add(skeleton);
        viewport.Children.Add(new ModelVisual3D { Content = world });
        Children.Add(viewport);
        BuildFloor();
        UpdateCamera();
        MouseDown += OnMouseDown;
        MouseMove += OnMouseMove;
        MouseUp += (_, e) => { orbiting = panning = false; ReleaseMouseCapture(); e.Handled = true; };
        LostMouseCapture += (_, _) => orbiting = panning = false;
        MouseWheel += (_, e) => { distance = Math.Clamp(distance * Math.Pow(0.88, e.Delta / 120.0), 1.2, 1000); UpdateCamera(); e.Handled = true; };
    }

    public void LoadClip(BvhDocument document)
    {
        clip = document;
        var bounds = PoseEvaluator.Bounds(document);
        double height = Math.Max(bounds.Max.Y - bounds.Min.Y, 0.001);
        scale = 2.8 / height;
        groundY = bounds.Min.Y;
        extent = Math.Max(bounds.Max.X - bounds.Min.X, bounds.Max.Z - bounds.Min.Z) * scale;
        skeleton.Children.Clear(); joints.Clear(); bones.Clear(); boneIndices.Clear();
        jointModels.Clear(); originalMaterials.Clear();
        foreach (var j in document.Joints)
        {
            Material material = SideMaterial(j, document);
            var transform = new MatrixTransform3D();
            var model = new GeometryModel3D(sphere, material) { Transform = transform, BackMaterial = material };
            joints.Add(transform); jointModels.Add(model); originalMaterials.Add(material); skeleton.Children.Add(model);
            if (j.ParentIndex >= 0)
            {
                var bone = new MatrixTransform3D();
                skeleton.Children.Add(new GeometryModel3D(cylinder, material) { Transform = bone, BackMaterial = material });
                bones.Add(bone); boneIndices.Add(joints.Count - 1);
            }
        }
        ResetCamera();
        Render(0, true, true);
    }

    private Material SideMaterial(BvhJoint joint, BvhDocument document)
    {
        int i = document.Joints.IndexOfJoint(joint);
        while (i >= 0)
        {
            string name = document.Joints[i].Name.ToLowerInvariant();
            if (name.Contains("left", StringComparison.Ordinal) || name.StartsWith("l_", StringComparison.Ordinal) || (name.StartsWith('l') && name.Length > 1 && char.IsUpper(document.Joints[i].Name[1]))) return left;
            if (name.Contains("right", StringComparison.Ordinal) || name.StartsWith("r_", StringComparison.Ordinal) || (name.StartsWith('r') && name.Length > 1 && char.IsUpper(document.Joints[i].Name[1]))) return right;
            i = document.Joints[i].ParentIndex;
        }
        return center;
    }

    public void Configure(bool lockRoot, bool showGround)
    {
        bool changed = follow != lockRoot;
        follow = lockRoot;
        if (showGround && floor.Children.Count == 0) BuildFloor();
        else if (!showGround) floor.Children.Clear();
        if (changed) ResetCamera();
    }

    public void Render(double framePosition, bool smooth, bool loop)
    {
        if (clip == null) return;
        var pose = PoseEvaluator.Evaluate(clip, framePosition, smooth, loop);
        var converted = new Point3D[pose.Length];
        var root = pose[0];
        for (int i = 0; i < pose.Length; i++)
        {
            Vec p = pose[i];
            converted[i] = new Point3D((p.X - (follow ? root.X : 0)) * scale,
                (p.Y - groundY) * scale, (p.Z - (follow ? root.Z : 0)) * scale);
            double r = clip.Joints[i].IsEndSite ? 0.024 : i == 0 ? 0.057 : 0.039;
            var m = Matrix3D.Identity; m.Scale(new Vector3D(r, r, r));
            m.Translate(new Vector3D(converted[i].X, converted[i].Y, converted[i].Z));
            joints[i].Matrix = m;
        }
        for (int b = 0; b < bones.Count; b++)
        {
            int i = boneIndices[b];
            bones[b].Matrix = CylinderBetween(converted[clip.Joints[i].ParentIndex], converted[i], 0.021);
        }
    }

    public void SelectJoint(int index)
    {
        for (int i = 0; i < jointModels.Count; i++)
        { jointModels[i].Material = i == index ? selected : originalMaterials[i]; jointModels[i].BackMaterial = jointModels[i].Material; }
    }

    public void ResetCamera()
    {
        yaw = 25; pitch = 12;
        distance = follow ? 9 : Math.Max(9, extent * 1.7);
        if (!follow && clip != null)
        {
            var bounds = PoseEvaluator.Bounds(clip);
            target = new Point3D((bounds.Min.X + bounds.Max.X) * scale / 2, 1.35, (bounds.Min.Z + bounds.Max.Z) * scale / 2);
        }
        else target = new Point3D(0, 1.35, 0);
        UpdateCamera();
    }
    public void SetView(bool side) { yaw = side ? 90 : 0; pitch = 4; UpdateCamera(); }

    private void UpdateCamera()
    {
        double y = yaw * Math.PI / 180, p = pitch * Math.PI / 180;
        var delta = new Vector3D(Math.Sin(y) * Math.Cos(p), Math.Sin(p), Math.Cos(y) * Math.Cos(p)) * distance;
        camera.Position = target + delta; camera.LookDirection = -delta; camera.UpDirection = new Vector3D(0, 1, 0);
    }
    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        Focus(); last = e.GetPosition(this);
        orbiting = e.ChangedButton == MouseButton.Left; panning = e.ChangedButton == MouseButton.Right || e.ChangedButton == MouseButton.Middle;
        if (orbiting || panning) { CaptureMouse(); e.Handled = true; }
    }
    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (!orbiting && !panning) return;
        Point now = e.GetPosition(this); Vector movement = now - last; last = now;
        if (orbiting) { yaw -= movement.X * 0.4; pitch = Math.Clamp(pitch + movement.Y * 0.35, -80, 80); }
        else
        {
            var rightAxis = Vector3D.CrossProduct(camera.LookDirection, camera.UpDirection); rightAxis.Normalize();
            var upAxis = Vector3D.CrossProduct(rightAxis, camera.LookDirection); upAxis.Normalize();
            target += rightAxis * (-movement.X * distance / Math.Max(ActualHeight, 1) * 0.7) + upAxis * (movement.Y * distance / Math.Max(ActualHeight, 1) * 0.7);
        }
        UpdateCamera(); e.Handled = true;
    }

    private void BuildFloor()
    {
        floor.Children.Clear();
        var plane = new MeshGeometry3D
        {
            Positions = new Point3DCollection { new(-150, -0.065, -150), new(-150, -0.065, 150), new(150, -0.065, 150), new(150, -0.065, -150) },
            TriangleIndices = new Int32Collection { 0, 1, 2, 0, 2, 3 }
        };
        plane.Freeze();
        var material = new DiffuseMaterial(new SolidColorBrush(Color.FromRgb(22, 36, 54))); material.Freeze();
        floor.Children.Add(new GeometryModel3D(plane, material) { BackMaterial = material });
        var gridMat = MakeMaterial("#2A3C52", false);
        for (int i = -20; i <= 20; i++)
        {
            AddStaticBone(floor, new Point3D(i * 0.5, -0.05, -10), new Point3D(i * 0.5, -0.05, 10), i == 0 ? 0.005 : 0.0028, gridMat);
            AddStaticBone(floor, new Point3D(-10, -0.05, i * 0.5), new Point3D(10, -0.05, i * 0.5), i == 0 ? 0.005 : 0.0028, gridMat);
        }
        AddStaticBone(floor, new Point3D(0, -0.04, 0), new Point3D(0.65, -0.04, 0), 0.009, MakeMaterial("#B77B77", false));
        AddStaticBone(floor, new Point3D(0, -0.04, 0), new Point3D(0, -0.04, 0.65), 0.009, MakeMaterial("#6C97AD", false));
    }

    private void AddStaticBone(Model3DGroup group, Point3D a, Point3D b, double radius, Material mat)
        => group.Children.Add(new GeometryModel3D(cylinder, mat) { Transform = new MatrixTransform3D(CylinderBetween(a, b, radius)), BackMaterial = mat });

    private static Matrix3D CylinderBetween(Point3D a, Point3D b, double radius)
    {
        Vector3D axis = b - a; double length = axis.Length;
        if (length < 1e-9) return new Matrix3D(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, a.X, a.Y, a.Z, 1);
        axis.Normalize();
        var normal = Vector3D.CrossProduct(Math.Abs(axis.Y) > 0.98 ? new Vector3D(1, 0, 0) : new Vector3D(0, 1, 0), axis); normal.Normalize();
        var tangent = Vector3D.CrossProduct(normal, axis); tangent.Normalize();
        return new Matrix3D(normal.X * radius, normal.Y * radius, normal.Z * radius, 0,
            axis.X * length, axis.Y * length, axis.Z * length, 0,
            tangent.X * radius, tangent.Y * radius, tangent.Z * radius, 0, a.X, a.Y, a.Z, 1);
    }
    private static Material MakeMaterial(string color, bool shiny = true)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)); brush.Freeze();
        var group = new MaterialGroup(); group.Children.Add(new DiffuseMaterial(brush));
        if (shiny) group.Children.Add(new SpecularMaterial(new SolidColorBrush(Color.FromArgb(85, 255, 255, 255)), 35));
        group.Freeze(); return group;
    }
    private static MeshGeometry3D MakeCylinder()
    {
        const int slices = 10;
        var mesh = new MeshGeometry3D();
        for (int i = 0; i <= slices; i++)
        {
            double angle = i * 2 * Math.PI / slices, x = Math.Cos(angle), z = Math.Sin(angle);
            mesh.Positions.Add(new Point3D(x, 0, z)); mesh.Positions.Add(new Point3D(x, 1, z));
            mesh.Normals.Add(new Vector3D(x, 0, z)); mesh.Normals.Add(new Vector3D(x, 0, z));
            if (i == slices) continue;
            int b = i * 2;
            foreach (int k in new[] { b, b + 1, b + 2, b + 1, b + 3, b + 2 }) mesh.TriangleIndices.Add(k);
        }
        mesh.Freeze(); return mesh;
    }
    private static MeshGeometry3D MakeSphere()
    {
        const int rings = 10, slices = 16;
        var mesh = new MeshGeometry3D();
        for (int r = 0; r <= rings; r++)
        for (int s = 0; s <= slices; s++)
        {
            double p = Math.PI * r / rings, t = 2 * Math.PI * s / slices;
            double x = Math.Sin(p) * Math.Cos(t), y = Math.Cos(p), z = Math.Sin(p) * Math.Sin(t);
            mesh.Positions.Add(new Point3D(x, y, z)); mesh.Normals.Add(new Vector3D(x, y, z));
            if (r == rings || s == slices) continue;
            int a = r * (slices + 1) + s, b = a + slices + 1;
            foreach (int i in new[] { a, a + 1, b, a + 1, b + 1, b }) mesh.TriangleIndices.Add(i);
        }
        mesh.Freeze(); return mesh;
    }
}

internal static class JointListExtensions
{
    public static int IndexOfJoint(this IReadOnlyList<BvhJoint> list, BvhJoint joint)
    { for (int i = 0; i < list.Count; i++) if (ReferenceEquals(list[i], joint)) return i; return -1; }
}
