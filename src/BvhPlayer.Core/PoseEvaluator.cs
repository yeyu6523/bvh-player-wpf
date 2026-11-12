using System.Numerics;

namespace BvhPlayer.Core;

public static class PoseEvaluator
{
    /// <summary>BVH uses column-vector T(offset)*C1*C2*… . System.Numerics uses row vectors,
    /// so prepend every declared channel and compose local * parentWorld.</summary>
    public static Matrix4x4 LocalMatrix(BvhJoint joint, float[] frame)
    {
        Matrix4x4 local = Matrix4x4.CreateTranslation(joint.Offset);
        for (int c = 0; c < joint.Channels.Length; c++)
        {
            float value = frame[joint.ChannelOffset + c];
            float radians = value * (MathF.PI / 180);
            Matrix4x4 channel = joint.Channels[c] switch
            {
                BvhChannel.Xposition => Matrix4x4.CreateTranslation(value, 0, 0),
                BvhChannel.Yposition => Matrix4x4.CreateTranslation(0, value, 0),
                BvhChannel.Zposition => Matrix4x4.CreateTranslation(0, 0, value),
                BvhChannel.Xrotation => Matrix4x4.CreateRotationX(radians),
                BvhChannel.Yrotation => Matrix4x4.CreateRotationY(radians),
                BvhChannel.Zrotation => Matrix4x4.CreateRotationZ(radians),
                _ => throw new InvalidOperationException("Unknown BVH channel")
            };
            local = channel * local;
        }
        return local;
    }

    public static Vector3[] Evaluate(BvhDocument clip, double framePosition, bool interpolate = true, bool loop = false)
    {
        if (!double.IsFinite(framePosition)) throw new ArgumentOutOfRangeException(nameof(framePosition));
        double f = loop ? ((framePosition % clip.FrameCount) + clip.FrameCount) % clip.FrameCount
            : Math.Clamp(framePosition, 0, clip.FrameCount - 1);
        int a = (int)Math.Floor(f);
        int b = a + 1 < clip.FrameCount ? a + 1 : loop ? 0 : a;
        float blend = interpolate ? (float)(f - a) : 0;
        var matrices = new Matrix4x4[clip.Joints.Count];
        var positions = new Vector3[clip.Joints.Count];
        for (int j = 0; j < clip.Joints.Count; j++)
        {
            var joint = clip.Joints[j];
            Matrix4x4 local = LocalMatrix(joint, clip.Frames[a]);
            if (blend > 0 && b != a)
            {
                var next = LocalMatrix(joint, clip.Frames[b]);
                Matrix4x4.Decompose(local, out _, out Quaternion qa, out Vector3 ta);
                Matrix4x4.Decompose(next, out _, out Quaternion qb, out Vector3 tb);
                local = Matrix4x4.CreateFromQuaternion(Quaternion.Slerp(qa, qb, blend));
                local.Translation = Vector3.Lerp(ta, tb, blend);
            }
            matrices[j] = joint.ParentIndex < 0 ? local : local * matrices[joint.ParentIndex];
            positions[j] = matrices[j].Translation;
        }
        return positions;
    }

    public static (Vector3 Min, Vector3 Max) Bounds(BvhDocument clip)
    {
        var min = new Vector3(float.PositiveInfinity);
        var max = new Vector3(float.NegativeInfinity);
        int samples = Math.Min(clip.FrameCount, 120);
        for (int i = 0; i < samples; i++)
        {
            double f = samples == 1 ? 0 : (double)i * (clip.FrameCount - 1) / (samples - 1);
            foreach (var p in Evaluate(clip, f, false))
            { min = Vector3.Min(min, p); max = Vector3.Max(max, p); }
        }
        return (min, max);
    }
}
