#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Orca
{ 
    [CustomPropertyDrawer(typeof(DrawRangeIfEnumAttribute))]
    internal class DrawRangeIfEnumPropertyDrawer : PropertyDrawer
    {
        private DrawRangeIfEnumAttribute drawIfEnumAttribute;
        private SerializedProperty targetEnumField;
        private float propertyHeight;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return propertyHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            drawIfEnumAttribute = attribute as DrawRangeIfEnumAttribute;
            targetEnumField = property.serializedObject.FindProperty(drawIfEnumAttribute.targetEnumName);

            if (targetEnumField == null)
            {
                EditorGUILayout.HelpBox(NotFoundError, MessageType.Error);
                propertyHeight = base.GetPropertyHeight(property, label);
                EditorGUI.PropertyField(position, property);

                return;
            }
            if (targetEnumField.propertyType != SerializedPropertyType.Enum)
            {
                EditorGUILayout.HelpBox(NotBoolError, MessageType.Error);
                propertyHeight = base.GetPropertyHeight(property, label);
                EditorGUI.PropertyField(position, property);

                return;
            }

            if (targetEnumField.enumValueIndex != drawIfEnumAttribute.targetEnumValue) propertyHeight = 0f;
            else
            {
                propertyHeight = base.GetPropertyHeight(property, label);

                //EditorGUI.PropertyField(position, property);

                if (property.propertyType == SerializedPropertyType.Float)
                    EditorGUI.Slider(position, property, 0.0f, drawIfEnumAttribute.max, label);
                else if (property.propertyType == SerializedPropertyType.Integer)
                    EditorGUI.IntSlider(position, property, 1, (int)drawIfEnumAttribute.max, label);
                else
                    EditorGUI.PropertyField(position, property);
            }
        }
        private const string NotFoundError = "DrawIfEnum attribute -- failed finding target field.";
        private const string NotBoolError = "DrawIfEnum attribute -- target field is not an enum.";
    }
}
#endif