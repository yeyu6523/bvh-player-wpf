using BvhPlayer.Core;
using System.Globalization;
using System.Numerics;
using System.Security.Cryptography;
using System.Text.Json;

int passed = 0, failed = 0;
var records = new List<object>();
void Test(string name, Action action)
{
    try { action(); passed++; Console.WriteLine("PASS " + name); }
    catch (Exception ex) { failed++; Console.WriteLine("FAIL " + name + ": " + ex.Message); }
}
void Check(bool okay, string message = "Assertion failed") { if (!okay) throw new Exception(message); }
void Near(double a, double b, double tolerance = 1e-5) => Check(Math.Abs(a - b) < tolerance, $"Expected {b}, got {a}");
void VectorNear(Vector3 a, Vector3 b, double tolerance = 0.0001) { Near(a.X, b.X, tolerance); Near(a.Y, b.Y, tolerance); Near(a.Z, b.Z, tolerance); }
void Reject(string source) { try { BvhDocument.Parse(source); } catch (FormatException) { return; } throw new Exception("Malformed file was accepted"); }
const string basic = "HIERARCHY ROOT Hips { OFFSET 0 0 0 CHANNELS 6 Xposition Yposition Zposition Zrotation Xrotation Yrotation JOINT Knee { OFFSET 0 -2 0 CHANNELS 3 Zrotation Xrotation Yrotation End Site { OFFSET 0 -1 0 } } } MOTION Frames: 2 Frame Time: 0.1 1 4 2 0 0 0 0 0 0 2 4 2 90 0 0 0 0 0";
var sample = BvhDocument.Parse(basic);
Test("Hierarchy, parent indices, End Site, frame/channel counts", () =>
{
    Check(sample.Joints.Count == 3 && sample.FrameCount == 2 && sample.ChannelCount == 9);
    Check(sample.Joints[1].ParentIndex == 0 && sample.Joints[2].ParentIndex == 1 && sample.Joints[2].IsEndSite);
    Check(sample.Joints[1].ChannelOffset == 6 && sample.Joints[2].Channels.Length == 0);
    Near(sample.Duration, .2); Near(sample.FramesPerSecond, 10);
});
Test("Forward kinematics: parent rotation and End Site", () =>
{
    var p = PoseEvaluator.Evaluate(sample, 0); VectorNear(p[0], new(1, 4, 2)); VectorNear(p[2], new(1, 1, 2));
    p = PoseEvaluator.Evaluate(sample, 1); VectorNear(p[0], new(2, 4, 2)); VectorNear(p[2], new(5, 4, 2));
});
Test("Declared Z-X order is not X-Z order", () =>
{
    var d = BvhDocument.Parse("HIERARCHY ROOT Root { OFFSET 0 0 0 CHANNELS 2 Zrotation Xrotation End Site { OFFSET 0 1 0 } } MOTION Frames: 1 Frame Time: .1 90 90");
    VectorNear(PoseEvaluator.Evaluate(d, 0)[1], new(0, 0, 1));
});
Test("Non-root translation channels and root OFFSET", () =>
{
    var d = BvhDocument.Parse("HIERARCHY ROOT Root { OFFSET 1 2 3 CHANNELS 1 Zrotation JOINT B { OFFSET 1 0 0 CHANNELS 1 Xposition End Site { OFFSET 1 0 0 } } } MOTION Frames: 1 Frame Time: .1 90 2");
    VectorNear(PoseEvaluator.Evaluate(d, 0)[2], new(1, 6, 3));
});
Test("Colon in joint names, braces without spaces, BOM, scientific notation", () =>
{
    var d = BvhDocument.Parse("\uFEFFHIERARCHY\nROOT mixamorig:Hips{OFFSET 1e0 0 0 CHANNELS 1 Xposition End Site{OFFSET 0 1 0}}MOTION Frames : 1 Frame Time : 1e-2 2e0");
    Check(d.Joints[0].Name == "mixamorig:Hips"); VectorNear(PoseEvaluator.Evaluate(d, 0)[1], new(3, 1, 0));
});
Test("Quaternion interpolation takes the short arc across 360 degrees", () =>
{
    var d = BvhDocument.Parse("HIERARCHY ROOT Root { OFFSET 0 0 0 CHANNELS 1 Zrotation End Site { OFFSET 1 0 0 } } MOTION Frames: 2 Frame Time: .1 350 10");
    VectorNear(PoseEvaluator.Evaluate(d, .5)[1], new(1, 0, 0));
    VectorNear(PoseEvaluator.Evaluate(d, 1.5, true, true)[1], new(1, 0, 0));
    VectorNear(PoseEvaluator.Evaluate(d, .5, false)[1], new(MathF.Cos(-MathF.PI / 18), MathF.Sin(-MathF.PI / 18), 0));
});
Test("Single frame and clamp", () =>
{
    var d = BvhDocument.Parse("HIERARCHY ROOT Root { OFFSET 0 0 0 CHANNELS 1 Xposition } MOTION Frames: 1 Frame Time: .02 7");
    VectorNear(PoseEvaluator.Evaluate(d, 100, true, true)[0], new(7, 0, 0));
    VectorNear(PoseEvaluator.Evaluate(d, -10)[0], new(7, 0, 0));
});
foreach (var invalid in new[] {
    ("truncated motion", basic[..^2]), ("extra motion", basic + " 1"),
    ("zero frame time", basic.Replace("Time: 0.1", "Time: 0")),
    ("zero frames", basic.Replace("Frames: 2", "Frames: 0")),
    ("NaN", basic.Replace("1 4 2", "NaN 4 2")),
    ("infinity", basic.Replace("1 4 2", "Infinity 4 2")),
    ("unknown channel", basic.Replace("Xposition", "Wposition")),
    ("duplicate channel", basic.Replace("Yposition", "Xposition")),
    ("missing brace", basic.Replace("} } } MOTION", "} } MOTION")),
    ("fractional frames", basic.Replace("Frames: 2", "Frames: 2.5")) })
    Test("Reject " + invalid.Item1, () => Reject(invalid.Item2));

Test("Play, pause, speed, resume, stop", () =>
{
    var c = new PlaybackClock(); c.Load(sample); c.Advance(.03); Near(c.Position, 0);
    c.Play(); c.Advance(.05); Near(c.Position, .05); c.Pause(); c.Advance(2); Near(c.Position, .05);
    c.Speed = 2; c.Play(); c.Advance(.025); Near(c.Position, .1); Check(c.CurrentFrame == 1);
    c.Speed = .5; c.Advance(.1); Near(c.Position, .15); c.Stop(); Check(!c.IsPlaying); Near(c.Position, 0);
});
Test("Loop wraps large elapsed times", () =>
{
    var c = new PlaybackClock(); c.Load(sample); c.Play(); c.Advance(1.05); Near(c.Position, .05); Check(c.IsPlaying);
});
Test("No loop stops at last frame and can restart", () =>
{
    var c = new PlaybackClock { Loop = false }; c.Load(sample); c.Play(); c.Advance(2);
    Check(!c.IsPlaying && c.CurrentFrame == 1); Near(c.Position, .2); c.Play(); Near(c.Position, 0); Check(c.IsPlaying);
});
Test("Seek, step, and frame boundary clamps", () =>
{
    var c = new PlaybackClock(); c.Load(sample); c.Play(); c.Step(1); Check(c.CurrentFrame == 1 && !c.IsPlaying);
    c.Step(1); Check(c.CurrentFrame == 1); c.SeekFrame(-99); Check(c.CurrentFrame == 0); c.SeekFrame(999); Check(c.CurrentFrame == 1);
});
Test("Numeric parsing ignores current locale", () =>
{
    var before = CultureInfo.CurrentCulture;
    try { CultureInfo.CurrentCulture = new CultureInfo("de-DE"); Near(BvhDocument.Parse(basic).FrameTime, .1); }
    finally { CultureInfo.CurrentCulture = before; }
});
Test("Invalid speed and elapsed time are rejected", () =>
{
    var c = new PlaybackClock();
    foreach (double s in new[] { 0, 3.1, double.NaN })
    { bool rejected = false; try { c.Speed = s; } catch (ArgumentOutOfRangeException) { rejected = true; } Check(rejected); }
    bool negative = false; try { c.Advance(-1); } catch (ArgumentOutOfRangeException) { negative = true; } Check(negative);
});

// Independently compute column-vector matrices in double precision for comparison with
// the production row-vector Matrix4x4 implementation. This is not a call back into LocalMatrix.
double[,] Identity() { var m = new double[4, 4]; for (int i = 0; i < 4; i++) m[i, i] = 1; return m; }
double[,] Mul(double[,] a, double[,] b)
{
    var c = new double[4, 4];
    for (int i = 0; i < 4; i++) for (int j = 0; j < 4; j++) for (int k = 0; k < 4; k++) c[i, j] += a[i, k] * b[k, j];
    return c;
}
Vector3[] ReferencePose(BvhDocument d, int f)
{
    var world = new List<double[,]>(); var result = new Vector3[d.Joints.Count];
    foreach (var j in d.Joints)
    {
        var m = Identity(); m[0, 3] = j.Offset.X; m[1, 3] = j.Offset.Y; m[2, 3] = j.Offset.Z;
        for (int k = 0; k < j.Channels.Length; k++)
        {
            var c = Identity(); var ch = j.Channels[k]; double v = d.Frames[f][j.ChannelOffset + k];
            int axis = ch.ToString()[0] - 'X';
            if (ch.ToString().EndsWith("position", StringComparison.Ordinal)) c[axis, 3] = v;
            else
            {
                double a = v * Math.PI / 180, cos = Math.Cos(a), sin = Math.Sin(a);
                int u = (axis + 1) % 3, w = (axis + 2) % 3;
                c[u, u] = c[w, w] = cos; c[u, w] = -sin; c[w, u] = sin;
            }
            m = Mul(m, c);
        }
        if (j.ParentIndex >= 0) m = Mul(world[j.ParentIndex], m);
        result[world.Count] = new Vector3((float)m[0, 3], (float)m[1, 3], (float)m[2, 3]); world.Add(m);
    }
    return result;
}

var inputFiles = new List<string>();
foreach (string arg in args)
{
    if (Directory.Exists(arg)) inputFiles.AddRange(Directory.GetFiles(arg, "*.bvh"));
    else if (File.Exists(arg)) inputFiles.Add(arg);
    else { failed++; Console.WriteLine("FAIL Missing input " + arg); }
}
foreach (string path in inputFiles.Distinct())
{
    Test("Integration " + Path.GetFileName(path), () =>
    {
        var watch = System.Diagnostics.Stopwatch.StartNew();
        var d = BvhDocument.Load(path);
        var frames = new[] { 0, d.FrameCount / 4, d.FrameCount / 2, d.FrameCount - 1 }.Distinct();
        foreach (int frame in frames)
        {
            var actual = PoseEvaluator.Evaluate(d, frame, false); var expected = ReferencePose(d, frame);
            for (int i = 0; i < actual.Length; i++) VectorNear(actual[i], expected[i], .025);
        }
        // Every frame must be evaluable and finite, including interpolated poses.
        for (int frame = 0; frame < d.FrameCount; frame++)
        foreach (var point in PoseEvaluator.Evaluate(d, frame + .25, true, true))
            Check(float.IsFinite(point.X) && float.IsFinite(point.Y) && float.IsFinite(point.Z));
        var clock = new PlaybackClock { Loop = false }; clock.Load(d); clock.Speed = 3; clock.Play(); clock.Advance(d.Duration);
        Check(!clock.IsPlaying && clock.CurrentFrame == d.FrameCount - 1);
        records.Add(new { file = Path.GetFileName(path), sha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant(), frames = d.FrameCount, joints = d.AnimatedJointCount, channels = d.ChannelCount, frameTime = d.FrameTime, duration = d.Duration, testMs = watch.ElapsedMilliseconds });
    });
}
Directory.CreateDirectory("TestResults");
File.WriteAllText("TestResults/core-results.json", JsonSerializer.Serialize(new { passed, failed, inputs = records }, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine($"RESULT {passed} passed, {failed} failed");
return failed == 0 ? 0 : 1;
