using System;
public struct DoubleCodec : ISecureCodec<double> {
    public int Lanes => 2; // double은 64비트라서 2개 레인 필요
    
    public void ToLanes(in double v, Span<int> lanes){
        long bits = System.BitConverter.DoubleToInt64Bits(v);
        lanes[0] = (int)(bits & 0xFFFFFFFF);         // 하위 32비트
        lanes[1] = (int)((bits >> 32) & 0xFFFFFFFF); // 상위 32비트
    }
    
    public double FromLanes(ReadOnlySpan<int> lanes){
        long bits = ((long)lanes[1] << 32) | ((long)lanes[0] & 0xFFFFFFFF);
        return System.BitConverter.Int64BitsToDouble(bits);
    }
}
