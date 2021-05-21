﻿#if UNITY_EDITOR
#region

using Deplorable_Mountaineer.EditorUtils.Attributes;
using UnityEditor;
using UnityEngine;

#endregion

namespace Deplorable_Mountaineer.EditorUtils.Editor {
    [CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
    public class ReadOnlyPropertyDrawer : PropertyDrawer {
        public override float GetPropertyHeight(SerializedProperty property,
            GUIContent label){
            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        public override void OnGUI(Rect position,
            SerializedProperty property,
            GUIContent label){
            GUI.enabled = false;
            EditorGUI.PropertyField(position, property, label, true);
            GUI.enabled = true;
        }
    }
}
#endif