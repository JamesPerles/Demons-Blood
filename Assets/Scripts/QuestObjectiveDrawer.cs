using UnityEngine;
using UnityEditor;
[CustomPropertyDrawer(typeof(QuestObjective))]
public class QuestObjectiveDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        SerializedProperty description = property.FindPropertyRelative("description");
        SerializedProperty type = property.FindPropertyRelative("type");
        SerializedProperty flagKey = property.FindPropertyRelative("flagKey");
        SerializedProperty targetEnemy = property.FindPropertyRelative("targetEnemy");
        SerializedProperty targetItem = property.FindPropertyRelative("targetItem");
        SerializedProperty requiredCount = property.FindPropertyRelative("requiredCount");
        SerializedProperty choiceGoldReward = property.FindPropertyRelative("choiceGoldReward");
        SerializedProperty choiceExpReward = property.FindPropertyRelative("choiceExpReward");
        SerializedProperty choiceItemRewards = property.FindPropertyRelative("choiceItemRewards");
        SerializedProperty choiceEquipmentRewards = property.FindPropertyRelative("choiceEquipmentRewards");
        SerializedProperty hasOptionalObjectives = property.FindPropertyRelative("hasOptionalObjectives");
        SerializedProperty optionalObjectives = property.FindPropertyRelative("optionalObjectives");
        bool isChoiceStage = IsParentStageChoiceStage(property);
        float spacing = EditorGUIUtility.standardVerticalSpacing;
        float y = position.y;
        void Draw(SerializedProperty prop)
        {
            float h = EditorGUI.GetPropertyHeight(prop,true);
            Rect r = new Rect(position.x, y, position.width, h);
            EditorGUI.PropertyField(r, prop, true);
            y += h + spacing;
        }
        Draw(description);
        Draw(type);
        Draw(flagKey);
        Draw(targetEnemy);
        Draw(targetItem);
        Draw(requiredCount);
        if(isChoiceStage)
        {
            Draw(choiceGoldReward);
            Draw(choiceExpReward);
            Draw(choiceItemRewards);
            Draw(choiceEquipmentRewards);
        }
        Draw(hasOptionalObjectives);
        if(hasOptionalObjectives.boolValue)
        {
            Draw(optionalObjectives);
        }
        EditorGUI.EndProperty();
    }
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
       SerializedProperty description = property.FindPropertyRelative("description");
        SerializedProperty type = property.FindPropertyRelative("type");
        SerializedProperty flagKey = property.FindPropertyRelative("flagKey");
        SerializedProperty targetEnemy = property.FindPropertyRelative("targetEnemy");
        SerializedProperty targetItem = property.FindPropertyRelative("targetItem");
        SerializedProperty requiredCount = property.FindPropertyRelative("requiredCount");
        SerializedProperty choiceGoldReward = property.FindPropertyRelative("choiceGoldReward");
        SerializedProperty choiceExpReward = property.FindPropertyRelative("choiceExpReward");
        SerializedProperty choiceItemRewards = property.FindPropertyRelative("choiceItemRewards");
        SerializedProperty choiceEquipmentRewards = property.FindPropertyRelative("choiceEquipmentRewards");
        SerializedProperty hasOptionalObjectives = property.FindPropertyRelative("hasOptionalObjectives");
        SerializedProperty optionalObjectives = property.FindPropertyRelative("optionalObjectives");
        bool isChoiceStage = IsParentStageChoiceStage(property);
        float spacing = EditorGUIUtility.standardVerticalSpacing;
        float total = 0f;
        total += EditorGUI.GetPropertyHeight(description, true) + spacing; 
        total += EditorGUI.GetPropertyHeight(type, true) + spacing;
        total += EditorGUI.GetPropertyHeight(flagKey, true) + spacing;
        total += EditorGUI.GetPropertyHeight(targetEnemy, true) + spacing;
        total += EditorGUI.GetPropertyHeight(targetItem, true) + spacing;
        total += EditorGUI.GetPropertyHeight(requiredCount, true) + spacing;
        if(isChoiceStage)
        {
            total += EditorGUI.GetPropertyHeight(choiceGoldReward, true) + spacing;
            total += EditorGUI.GetPropertyHeight(choiceExpReward, true) + spacing;
            total += EditorGUI.GetPropertyHeight(choiceItemRewards, true) + spacing;
            total += EditorGUI.GetPropertyHeight(choiceEquipmentRewards, true) + spacing;
        }
        total += EditorGUI.GetPropertyHeight(hasOptionalObjectives, true) + spacing;
        if(hasOptionalObjectives.boolValue)
        {
            total += EditorGUI.GetPropertyHeight(optionalObjectives, true) + spacing;
        }
        return total;
    }
    bool IsParentStageChoiceStage(SerializedProperty objectiveProperty)
    {
        string path = objectiveProperty.propertyPath;
        int cut = path.LastIndexOf(".objectives.Array.data[");
        if(cut < 0) return false;
        string stagePath = path.Substring(0, cut);
        SerializedProperty stageProp = objectiveProperty.serializedObject.FindProperty(stagePath);
        if(stageProp == null) return false;
        SerializedProperty isChoiceStageProp = stageProp.FindPropertyRelative("isChoiceStage");
        return isChoiceStageProp != null && isChoiceStageProp.boolValue;
    }
}