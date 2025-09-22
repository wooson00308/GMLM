using System;
public struct Vector4Codec : ISecureCodec<UnityEngine.Vector4> {
    public int Lanes => 4;
    public void ToLanes(in UnityEngine.Vector4 v, Span<int> lanes){
        lanes[0] = System.BitConverter.SingleToInt32Bits(v.x);
        lanes[1] = System.BitConverter.SingleToInt32Bits(v.y);
        lanes[2] = System.BitConverter.SingleToInt32Bits(v.z);
        lanes[3] = System.BitConverter.SingleToInt32Bits(v.w);
    }
    public UnityEngine.Vector4 FromLanes(ReadOnlySpan<int> lanes){
        return new UnityEngine.Vector4(
            System.BitConverter.Int32BitsToSingle(lanes[0]),
            System.BitConverter.Int32BitsToSingle(lanes[1]),
            System.BitConverter.Int32BitsToSingle(lanes[2]),
            System.BitConverter.Int32BitsToSingle(lanes[3]));
    }
}
