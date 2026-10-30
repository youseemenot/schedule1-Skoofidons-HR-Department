using System;
using System.Collections.Generic;

namespace SkoofidonsHRDepartment
{
    internal static class MachineGrowthContext
    {
        [ThreadStatic] private static List<Frame> frames;

        internal sealed record Frame(MachineTimerKind Kind, nint Pointer, double Rate);
        internal readonly struct Scope
        {
            internal readonly List<Frame> Frames;
            internal readonly Frame Frame;
            internal readonly int Depth;
            internal Scope(List<Frame> frames, Frame frame, int depth)
            { Frames = frames; Frame = frame; Depth = depth; }
        }

        internal static Scope Enter(MachineTimerKind kind, nint pointer, double rate)
        {
            var stack = frames ??= new List<Frame>();
            var frame = new Frame(kind, pointer, rate);
            int depth = stack.Count;
            stack.Add(frame);
            return new Scope(stack, frame, depth);
        }

        // Restore the depth from before this callback, even if a nested callback
        // failed before its own cleanup. Token identity protects later callbacks
        // from duplicate, foreign-thread and stale scene finalizers.
        internal static void Exit(Scope scope)
        {
            var stack = frames;
            if (stack == null || !ReferenceEquals(stack, scope.Frames) || stack.Count <= scope.Depth ||
                !ReferenceEquals(stack[scope.Depth], scope.Frame)) return;
            stack.RemoveRange(scope.Depth, stack.Count - scope.Depth);
        }

        internal static void Clear() => frames = null;

        internal static bool TryScale(MachineTimerKind kind, nint pointer, float previous, float requested, out float scaled)
        {
            scaled = requested;
            if (frames == null || frames.Count == 0) return false;
            Frame frame = frames[^1];
            return frame.Kind == kind && frame.Pointer == pointer && frame.Rate != 1 &&
                MachineTimerPolicy.TryScaleGrowth(previous, requested, frame.Rate, out scaled);
        }
    }
}
