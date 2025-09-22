using System;
public struct Vector2Codec : ISecureCodec<UnityEngine.Vector2> {
    public int Lanes => 2;
    public void ToLanes(in UnityEngine.Vector2 v, Span<int> lanes){
        lanes[0] = System.BitConverter.SingleToInt32Bits(v.x);
        lanes[1] = System.BitConverter.SingleToInt32Bits(v.y);
    }
    public UnityEngine.Vector2 FromLanes(ReadOnlySpan<int> lanes){
        return new UnityEngine.Vector2(
            System.BitConverter.Int32BitsToSingle(lanes[0]),
            System.BitConverter.Int32BitsToSingle(lanes[1]));
    }
}
