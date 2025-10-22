namespace GMLM.AI
{
    /// <summary>
    /// Blackboard에서 타입 안전한 키를 제공하는 제네릭 클래스
    /// 컴파일 타임에 타입 체크를 통해 런타임 에러를 방지
    /// </summary>
    /// <typeparam name="T">저장할 데이터의 타입</typeparam>
    public class BlackboardKey<T>
    {
        public string Key { get; }
        
        public BlackboardKey(string key)
        {
            Key = key;
        }
        
        public static implicit operator string(BlackboardKey<T> key)
        {
            return key.Key;
        }
        
        public static implicit operator BlackboardKey<T>(string key)
        {
            return new BlackboardKey<T>(key);
        }
    }
}
