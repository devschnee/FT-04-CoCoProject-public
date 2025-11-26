using UnityEditor;
using UnityEngine;

// OtherAnimalAnimationController.cs 에서 각 동물 타입마다 인스펙터에 이쁘게 나오게 하고 싶어서 만?듦?

[CustomPropertyDrawer(typeof(ShowIfAnimalAttribute))]
public class ShowIfAnimalDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (ShouldShow(property))
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }
        else
        {
            // 높이 0 -> Inspector에서 안 보이게
            return 0f;
        }
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (!ShouldShow(property))
        {
            // 그리지 않음
            return;
        }

        EditorGUI.PropertyField(position, property, label, true);
    }

    // animalType 값이 Attribute 에 지정된 값과 같으면 true

    private bool ShouldShow(SerializedProperty property)
    {
        var showIf = (ShowIfAnimalAttribute)attribute;

        // 이 Property가 들어있는 오브젝트의 SerializedObject
        SerializedObject sObj = property.serializedObject;

        // 같은 클래스 안에 animalType 이라고 이름 붙인 프로퍼티가 있어야 한다고 가정
        SerializedProperty animalTypeProp = sObj.FindProperty("animalType");
        if (animalTypeProp == null)
        {
            // 못 찾으면 그냥 항상 보이게 / 에러 방지용
            return true;
        }

        AnimalType currentType = (AnimalType)animalTypeProp.enumValueIndex;
        return currentType == showIf.animalType;
    }
}
