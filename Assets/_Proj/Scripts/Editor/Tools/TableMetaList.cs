using System.Collections.Generic;

[System.Serializable]
public class TableMetaList
{
    public List<TableMetaEntry> entries;
}
//json으로 사용하기 위한 랩핑 코드

[System.Serializable]
public class TableMetaEntry
{
    public string name;
    public string type;
}
//데이터테이블 클래스 구조