using System;
public struct FloatCodec : ISecureCodec<float> {
    public int Lanes => 1;
    public void ToLanes(in float v, Span<int> lanes){
        lanes[0] = System.BitConverter.SingleToInt32Bits(v);
    }
    public float FromLanes(ReadOnlySpan<int> lanes){
        return System.BitConverter.Int32BitsToSingle(lanes[0]);
    }
}