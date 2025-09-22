using System;

public interface ISecureCodec<T>
{
    /// 사용 레인(32비트 슬롯) 수: int/float/bool=1, Vector2=2, Vector3=3, Vector4=4
    int Lanes { get; }

    /// 값을 32비트 정수 레인들로 직렬화 (비트 동일보존)
    void ToLanes(in T value, Span<int> lanes);

    /// 레인들을 값으로 역직렬화
    T FromLanes(ReadOnlySpan<int> lanes);
}
