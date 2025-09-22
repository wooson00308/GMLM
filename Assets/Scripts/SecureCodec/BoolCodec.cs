using System;
public struct BoolCodec : ISecureCodec<bool> {
    public int Lanes => 1;
    public void ToLanes(in bool v, Span<int> lanes){ lanes[0] = v ? 1 : 0; }
    public bool FromLanes(ReadOnlySpan<int> lanes){ return lanes[0] != 0; }
}