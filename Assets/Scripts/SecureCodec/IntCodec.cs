using System;
public struct IntCodec : ISecureCodec<int> {
    public int Lanes => 1;
    public void ToLanes(in int v, Span<int> lanes){ lanes[0] = v; }
    public int FromLanes(ReadOnlySpan<int> lanes){ return lanes[0]; }
}