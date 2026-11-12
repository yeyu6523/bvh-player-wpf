using System.Globalization;
using System.Numerics;

namespace BvhPlayer.Core;

public enum BvhChannel { Xposition, Yposition, Zposition, Xrotation, Yrotation, Zrotation }

public sealed record BvhJoint(string Name, int ParentIndex, Vector3 Offset,
    BvhChannel[] Channels, int ChannelOffset, bool IsEndSite);

public sealed class BvhDocument
{
    public string Name { get; }
    public IReadOnlyList<BvhJoint> Joints { get; }
    public IReadOnlyList<float[]> Frames { get; }
    public int ChannelCount { get; }
    public int FrameCount => Frames.Count;
    public double FrameTime { get; }
    public double Duration => FrameCount * FrameTime;
    public double FramesPerSecond => 1 / FrameTime;
    public int AnimatedJointCount => Joints.Count(j => !j.IsEndSite);

    internal BvhDocument(string name, BvhJoint[] joints, float[][] frames, int channels, double frameTime)
        => (Name, Joints, Frames, ChannelCount, FrameTime) = (name, joints, frames, channels, frameTime);

    public static BvhDocument Load(string path)
    {
        if (!File.Exists(path)) throw new FileNotFoundException("找不到 BVH 文件。", path);
        if (new FileInfo(path).Length > 256L * 1024 * 1024)
            throw new FormatException("文件超过 256 MB，请使用较小的 BVH 文件。");
        return Parse(File.ReadAllText(path), Path.GetFileName(path));
    }

    public static BvhDocument Parse(string source, string name = "Untitled.bvh")
        => new Parser(source, name).Read();

    // A token reader avoids building a second, multi-million-element array of strings.
    private sealed class Parser(string text, string name)
    {
        private int cursor;
        private int line = 1;
        private readonly List<BvhJoint> joints = [];
        private int channelCount;

        private ReadOnlySpan<char> Next()
        {
            while (cursor < text.Length && (char.IsWhiteSpace(text[cursor]) || text[cursor] == '\uFEFF'))
            { if (text[cursor++] == '\n') line++; }
            if (cursor == text.Length) return [];
            int start = cursor++;
            if (text[start] is not ('{' or '}'))
                while (cursor < text.Length && !char.IsWhiteSpace(text[cursor]) && text[cursor] is not ('{' or '}')) cursor++;
            return text.AsSpan(start, cursor - start);
        }

        private FormatException Error(string message) => new($"第 {line} 行：{message}");
        private string Word()
        {
            var token = Next();
            if (token.IsEmpty) throw Error("文件意外结束。");
            return token.ToString();
        }
        private void Expect(string expected)
        {
            string actual = Word();
            if (!actual.Equals(expected, StringComparison.OrdinalIgnoreCase))
                throw Error($"应为 {expected}，实际为 {actual}。");
        }
        private double Number()
        {
            var token = Next();
            if (!double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out double value) || !double.IsFinite(value))
                throw Error($"无效的有限数值“{token.ToString()}”。");
            return value;
        }
        private float Scalar()
        {
            double value = Number();
            if (Math.Abs(value) > 1e12) throw Error("数值超出可显示范围（绝对值须不大于 10¹²）。");
            return (float)value;
        }
        private int Count(int minimum, int maximum)
        {
            double value = Number();
            if (value < minimum || value > maximum || value != Math.Truncate(value))
                throw Error($"计数须为 {minimum} 到 {maximum} 的整数。");
            return (int)value;
        }
        private void Header(string value)
        {
            string word = Word();
            if (word.Equals(value + ":", StringComparison.OrdinalIgnoreCase)) return;
            if (word.Equals(value, StringComparison.OrdinalIgnoreCase)) { Expect(":"); return; }
            throw Error($"应为 {value}:，实际为 {word}。");
        }

        public BvhDocument Read()
        {
            Expect("HIERARCHY"); Expect("ROOT");
            ReadJoint(Word(), -1, false, 0);
            Expect("MOTION"); Header("Frames");
            int count = Count(1, 2_000_000);
            Expect("Frame"); Header("Time");
            double dt = Number();
            if (dt <= 0 || dt > 3600) throw Error("Frame Time 必须大于 0 且不大于 3600 秒。");
            if (channelCount == 0 || (long)count * channelCount > 64_000_000)
                throw Error("运动通道为空或数据超过 6400 万个数值。");
            var frames = new float[count][];
            for (int f = 0; f < count; f++)
            {
                frames[f] = new float[channelCount];
                for (int c = 0; c < channelCount; c++) frames[f][c] = Scalar();
            }
            if (!Next().IsEmpty) throw Error("MOTION 数值多于 Frames × CHANNELS 声明的数量。");
            return new BvhDocument(name, joints.ToArray(), frames, channelCount, dt);
        }

        private void ReadJoint(string jointName, int parent, bool end, int depth)
        {
            if (depth > 256 || joints.Count >= 4096) throw Error("骨骼层级过深或关节超过 4096 个。");
            if (jointName is "{" or "}") throw Error("关节缺少名称。");
            Expect("{"); Expect("OFFSET");
            var offset = new Vector3(Scalar(), Scalar(), Scalar());
            int index = joints.Count;
            int start = channelCount;
            BvhChannel[] channels = [];
            if (!end)
            {
                Expect("CHANNELS");
                channels = new BvhChannel[Count(0, 6)];
                for (int i = 0; i < channels.Length; i++)
                {
                    string token = Word();
                    if (!Enum.TryParse(token, true, out BvhChannel channel) || !Enum.IsDefined(channel) ||
                        !token.Equals(channel.ToString(), StringComparison.OrdinalIgnoreCase) || channels.Take(i).Contains(channel))
                        throw Error($"无效或重复的通道 {token}。");
                    channels[i] = channel;
                }
                channelCount += channels.Length;
            }
            joints.Add(new BvhJoint(jointName, parent, offset, channels, start, end));
            while (true)
            {
                string token = Word();
                if (token == "}") break;
                if (end) throw Error("End Site 中只能包含 OFFSET。");
                if (token.Equals("JOINT", StringComparison.OrdinalIgnoreCase)) ReadJoint(Word(), index, false, depth + 1);
                else if (token.Equals("End", StringComparison.OrdinalIgnoreCase))
                { Expect("Site"); ReadJoint(jointName + " / End", index, true, depth + 1); }
                else throw Error($"未知层级标记 {token}。");
            }
        }
    }
}
