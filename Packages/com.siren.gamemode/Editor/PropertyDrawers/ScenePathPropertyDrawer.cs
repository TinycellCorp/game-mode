using GameMode.PropertyAttributes;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameMode.Editor
{
    [CustomPropertyDrawer(typeof(ScenePathAttribute))]
    public class ScenePathPropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var root = new VisualElement();

            if (property.propertyType != SerializedPropertyType.String)
            {
                root.Add(new PropertyField(property));
                Debug.LogWarning($"scene path field is not string ({property.displayName})");
                return root;
            }

            var field = new ObjectField(property.displayName)
            {
                objectType = typeof(SceneAsset),
            };
            var path = property.stringValue;
            if (!string.IsNullOrEmpty(path))
            {
                field.SetValueWithoutNotify(AssetDatabase.LoadAssetAtPath<SceneAsset>(path));
            }

            field.RegisterValueChangedCallback(e =>
            {
                Undo.RecordObject(property.serializedObject.targetObject, "scene path");
                if (e.newValue is SceneAsset sceneAsset)
                {
                    var scenePath = AssetDatabase.GetAssetPath(sceneAsset);
                    property.stringValue = scenePath;
                }
                else
                {
                    property.stringValue = null;
                }

                property.serializedObject.ApplyModifiedProperties();
            });
            root.Add(field);

            return root;
        }
    }
}