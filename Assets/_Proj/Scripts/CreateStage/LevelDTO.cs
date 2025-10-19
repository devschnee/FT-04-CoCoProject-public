using System;
using System.Collections.Generic;
using UnityEngine;

//Data Transfer Object
[Serializable]
public class GridDTO
{
    public int cols, rows; // 가로, 세로 타일 수
    public float tileSize; // 타일 한 칸 월드 크기(ex : 1.0f)
    public Vector3 origin = Vector3.zero; //[x, y, z] 월드 기준 원점
}

public enum EntityMode { Toggle, Once, Hold }
public enum InitFwd { Up, Down, Left, Right } // up, down, left, right. 초기에 바라보는 방향. (문/브리지 배치, 포탑 회전 초기값 등)

[Serializable]
public class EntityDTO
{
    public string type;
    public int x, y; // 그리드 좌표
    public int w = 1, h = 1; // 엔터티 타일 폭/높이
    public string channel; // 회로 (ex : type : "switch", channel : "A" -> type:"door", channel:"A", type:"bridge",channel:"A" 동작)
    
    public EntityMode mode = EntityMode.Toggle;
    public InitFwd initFwd = InitFwd.Right;

    public float fovDeg, range; // 시야각(0이면 미사용으로 간주), 사거리. turret ( type == "turret"일때만 유효)
}

[Serializable]
public class LevelDTO
{
    public string name; // 레벨 이름
    public GridDTO grid; // 그리드 설정
    public List<string> tiles; // 각 줄에 "G, G, G, B, ..." <- 한 행.
    public List<EntityDTO> entities; // 엔터티 목록
}
