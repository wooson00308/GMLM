using System;
using UnityEngine;

/// <summary>
/// 편의용 Secure 타입 래퍼들 - 전역에서 사용 가능한 실제 타입들
/// </summary>

// 기본 타입들
[Serializable]
public struct SecureInt
{
    private Secure<int, IntCodec> _value;
    
    public SecureInt(int value) { _value = new Secure<int, IntCodec>(value); }
    public int Value { get => _value.Value; set => _value.Value = value; }
    
    public static implicit operator int(SecureInt s) => s._value.Value;
    public static implicit operator SecureInt(int v) => new SecureInt(v);
    
    public override string ToString() => _value.Value.ToString();
}

[Serializable]
public struct SecureFloat
{
    private Secure<float, FloatCodec> _value;
    
    public SecureFloat(float value) { _value = new Secure<float, FloatCodec>(value); }
    public float Value { get => _value.Value; set => _value.Value = value; }
    
    public static implicit operator float(SecureFloat s) => s._value.Value;
    public static implicit operator SecureFloat(float v) => new SecureFloat(v);
    
    public override string ToString() => _value.Value.ToString();
}

[Serializable]
public struct SecureDouble
{
    private Secure<double, DoubleCodec> _value;
    
    public SecureDouble(double value) { _value = new Secure<double, DoubleCodec>(value); }
    public double Value { get => _value.Value; set => _value.Value = value; }
    
    public static implicit operator double(SecureDouble s) => s._value.Value;
    public static implicit operator SecureDouble(double v) => new SecureDouble(v);
    
    public override string ToString() => _value.Value.ToString();
}

[Serializable]
public struct SecureBool
{
    private Secure<bool, BoolCodec> _value;
    
    public SecureBool(bool value) { _value = new Secure<bool, BoolCodec>(value); }
    public bool Value { get => _value.Value; set => _value.Value = value; }
    
    public static implicit operator bool(SecureBool s) => s._value.Value;
    public static implicit operator SecureBool(bool v) => new SecureBool(v);
    
    public override string ToString() => _value.Value.ToString();
}

// Vector 타입들  
[Serializable]
public struct SecureVector2
{
    private Secure<Vector2, Vector2Codec> _value;
    
    public SecureVector2(Vector2 value) { _value = new Secure<Vector2, Vector2Codec>(value); }
    public Vector2 Value { get => _value.Value; set => _value.Value = value; }
    
    public static implicit operator Vector2(SecureVector2 s) => s._value.Value;
    public static implicit operator SecureVector2(Vector2 v) => new SecureVector2(v);
    
    public override string ToString() => _value.Value.ToString();
}

[Serializable]
public struct SecureVector3
{
    private Secure<Vector3, Vector3Codec> _value;
    
    public SecureVector3(Vector3 value) { _value = new Secure<Vector3, Vector3Codec>(value); }
    public Vector3 Value { get => _value.Value; set => _value.Value = value; }
    
    public static implicit operator Vector3(SecureVector3 s) => s._value.Value;
    public static implicit operator SecureVector3(Vector3 v) => new SecureVector3(v);
    
    public override string ToString() => _value.Value.ToString();
}

[Serializable]
public struct SecureVector4
{
    private Secure<Vector4, Vector4Codec> _value;
    
    public SecureVector4(Vector4 value) { _value = new Secure<Vector4, Vector4Codec>(value); }
    public Vector4 Value { get => _value.Value; set => _value.Value = value; }
    
    public static implicit operator Vector4(SecureVector4 s) => s._value.Value;
    public static implicit operator SecureVector4(Vector4 v) => new SecureVector4(v);
    
    public override string ToString() => _value.Value.ToString();
}

/// <summary>
/// 사용 예시:
/// 
/// // 기존 방식
/// Secure&lt;int, IntCodec&gt; health = 100;
/// Secure&lt;Vector3, Vector3Codec&gt; position = Vector3.zero;
/// 
/// // 새로운 간단한 방식 (실제 타입 클래스)
/// SecureInt health = 100;
/// SecureFloat damage = 25.5f;
/// SecureDouble preciseValue = 3.14159265359;
/// SecureBool isAlive = true;
/// SecureVector2 screenPos = new Vector2(100, 200);
/// SecureVector3 worldPos = Vector3.zero;
/// SecureVector4 color = Color.white;
/// 
/// // 모든 기능 동일하게 사용 가능 + 전역에서 인식됨
/// health.Value = 150;           // 직접 값 변경
/// int currentHealth = health;   // 암시적 변환
/// float actualDamage = damage * 2f; // 자동 변환 및 연산
/// 
/// // 장점:
/// // - using 별칭 반복 불필요
/// // - IntelliSense 완벽 지원
/// // - 디버깅 시 타입 명확
/// // - Unity Inspector에서 표시 가능
/// </summary>
