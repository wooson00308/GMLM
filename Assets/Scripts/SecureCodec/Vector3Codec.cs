using System;
public struct Vector3Codec : ISecureCodec<UnityEngine.Vector3> {
    public int Lanes => 3;
    public void ToLanes(in UnityEngine.Vector3 v, Span<int> lanes){
        lanes[0] = System.BitConverter.SingleToInt32Bits(v.x);
        lanes[1] = System.BitConverter.SingleToInt32Bits(v.y);
        lanes[2] = System.BitConverter.SingleToInt32Bits(v.z);
    }
    public UnityEngine.Vector3 FromLanes(ReadOnlySpan<int> lanes){
        return new UnityEngine.Vector3(
            System.BitConverter.Int32BitsToSingle(lanes[0]),
            System.BitConverter.Int32BitsToSingle(lanes[1]),
            System.BitConverter.Int32BitsToSingle(lanes[2]));
    }
}