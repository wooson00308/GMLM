using System;
using UnityEngine;

[Serializable]
public struct Secure<T, TCodec> where TCodec : struct, ISecureCodec<T>
{
    // 최대 4레인 지원 (int/float/bool/Vector2/3/4 커버)
    private int enc0, enc1, enc2, enc3;
    private int key0, key1, key2, key3;
    private int fake0, fake1, fake2, fake3;
    private int salt;
    private byte lanes; // 실제 사용 레인 수

    // UnityEngine.Random.Range는 MonoBehaviour 필드 초기화(생성자) 타이밍에 사용할 수 없어
    // 간단한 xorshift32 기반 PRNG를 사용한다. (타입 정적 상태이므로 호출마다 값 갱신)
    private static uint _rngState = (uint)(System.Environment.TickCount ^ (int)DateTime.UtcNow.Ticks);
    private static int Rng(){
        uint x = _rngState;
        x ^= x << 13;
        x ^= x >> 17;
        x ^= x << 5;
        _rngState = x != 0 ? x : 0x9E3779B9u;
        return unchecked((int)x);
    }
    private static void Flag() => AntiCheatSignals.Flag($"Secure<{typeof(T).Name}>_Tamper");

    public Secure(in T v)
    {
        var codec = new TCodec();
        int n = codec.Lanes;
        lanes = (byte)n;
        salt = (int)DateTime.UtcNow.Ticks;

        // 임시 버퍼(스택)로 레인화
        Span<int> raw = stackalloc int[4];
        codec.ToLanes(in v, raw);

        // 각 레인 암호화 + 미러 생성
        key0 = key1 = key2 = key3 = 0;
        enc0 = enc1 = enc2 = enc3 = 0;
        fake0 = fake1 = fake2 = fake3 = 0;

        if (n > 0){ key0 = Rng(); enc0 = raw[0] ^ key0; fake0 = (raw[0] ^ 0x5A5A5A5A) + salt; }
        if (n > 1){ key1 = Rng(); enc1 = raw[1] ^ key1; fake1 = (raw[1] ^ 0x5A5A5A5A) + salt; }
        if (n > 2){ key2 = Rng(); enc2 = raw[2] ^ key2; fake2 = (raw[2] ^ 0x5A5A5A5A) + salt; }
        if (n > 3){ key3 = Rng(); enc3 = raw[3] ^ key3; fake3 = (raw[3] ^ 0x5A5A5A5A) + salt; }
    }

    public T Value {
        get {
            var codec = new TCodec();
            int n = lanes;
            Span<int> raw = stackalloc int[4];

            int r0=0,r1=0,r2=0,r3=0;
            if (n > 0){ r0 = enc0 ^ key0; if (((r0 ^ 0x5A5A5A5A) + salt) != fake0) Flag(); raw[0]=r0; }
            if (n > 1){ r1 = enc1 ^ key1; if (((r1 ^ 0x5A5A5A5A) + salt) != fake1) Flag(); raw[1]=r1; }
            if (n > 2){ r2 = enc2 ^ key2; if (((r2 ^ 0x5A5A5A5A) + salt) != fake2) Flag(); raw[2]=r2; }
            if (n > 3){ r3 = enc3 ^ key3; if (((r3 ^ 0x5A5A5A5A) + salt) != fake3) Flag(); raw[3]=r3; }

            // 즉시 키 롤링(재암호화)
            if (n > 0){ key0 = Rng(); enc0 = r0 ^ key0; }
            if (n > 1){ key1 = Rng(); enc1 = r1 ^ key1; }
            if (n > 2){ key2 = Rng(); enc2 = r2 ^ key2; }
            if (n > 3){ key3 = Rng(); enc3 = r3 ^ key3; }

            return codec.FromLanes(raw);
        }
        set {
            var codec = new TCodec();
            int n = codec.Lanes;
            lanes = (byte)n;
            Span<int> raw = stackalloc int[4];
            codec.ToLanes(in value, raw);

            if (n > 0){ key0 = Rng(); enc0 = raw[0] ^ key0; fake0 = (raw[0]^0x5A5A5A5A)+salt; }
            if (n > 1){ key1 = Rng(); enc1 = raw[1] ^ key1; fake1 = (raw[1]^0x5A5A5A5A)+salt; }
            if (n > 2){ key2 = Rng(); enc2 = raw[2] ^ key2; fake2 = (raw[2]^0x5A5A5A5A)+salt; }
            if (n > 3){ key3 = Rng(); enc3 = raw[3] ^ key3; fake3 = (raw[3]^0x5A5A5A5A)+salt; }
        }
    }

    public static implicit operator T(Secure<T, TCodec> s) => s.Value;
    public static implicit operator Secure<T, TCodec>(T v) => new Secure<T, TCodec>(v);
}
