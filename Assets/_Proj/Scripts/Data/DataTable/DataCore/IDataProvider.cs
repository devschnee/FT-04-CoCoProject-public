using UnityEngine;
public interface IDataProvider<TKey, TValue>
{
    //각종 dataProvider 클래스가 상속할 인터페이스
    //id가 int, string인 두가지라 Key, Value식으로 생성
    TValue GetData(TKey id);



}