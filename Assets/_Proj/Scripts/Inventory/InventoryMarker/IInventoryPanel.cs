public interface IInventoryPanel
{
    /// <summary>
    /// 패널의 내용을 현재 데이터 상태로 재구성/새로고침
    /// (버튼 바인딩, 슬롯 갱신, 풀링 재사용 등 내부 정책은 자유)
    /// </summary>
    void Rebuild();
}
