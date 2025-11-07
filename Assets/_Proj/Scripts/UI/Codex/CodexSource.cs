using System.Collections.Generic;
using UnityEngine;

public class CodexSource : MonoBehaviour, ICodexSource
{
    [Header("Database")]
    public CodexDatabase codexDatabase;   // 하나만 받는다

    private readonly IResourceLoader _loader = new ResourcesLoader();

    public IReadOnlyList<CodexEntry> GetByCategory(CodexCategory category)
    {
        var list = new List<CodexEntry>();
        if (codexDatabase == null || codexDatabase.codexList == null)
            return list;

        foreach (var data in codexDatabase.codexList)
        {
            // CodexData 의 codex_type 을 CodexCategory 로 변환
            var dataCategory = ConvertTypeToCategory(data.codex_type);
            if (dataCategory != category)
                continue;

            // 아이콘 로드
            //Codex에 Icon 삭제되서 오류떠서 임시로 변경
            //향후 체크하시고 수정필요하면 수정해주세요
            Sprite icon = data.GetCodexIcon(_loader);

            string name = data.codex_name;
            string desc = data.codex_lore;

            list.Add(new CodexEntry(
                data.item_id,
                name,
                desc,
                icon,
                dataCategory
            ));
        }

        return list;
    }

    private CodexCategory ConvertTypeToCategory(CodexType type)
    {
        switch (type)
        {
            case CodexType.animal: return CodexCategory.Animal;
            case CodexType.deco: return CodexCategory.Deco;
            case CodexType.costume: return CodexCategory.Costume;
            case CodexType.artifact: return CodexCategory.Artifact;
            case CodexType.home: return CodexCategory.Home;  // 새로 추가
            default: return CodexCategory.Animal;
        }
    }
}
