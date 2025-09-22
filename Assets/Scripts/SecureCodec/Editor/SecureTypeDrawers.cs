#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

// Drawers for Secure wrapper types. These only compile in the Unity Editor.

namespace SecureCodec.Editor
{
    internal static class SecureDrawerUtil
    {
        public static void DrawInt(Rect position, GUIContent label, SerializedProperty property, System.Func<Object, SecureInt> getter, System.Action<Object, SecureInt> setter)
        {
            EditorGUI.BeginProperty(position, label, property);
            var targets = property.serializedObject.targetObjects;
            if (targets == null || targets.Length == 0)
            {
                EditorGUI.EndProperty();
                return;
            }
            var first = targets[0];
            var current = getter(first);
            int newVal = EditorGUI.IntField(position, label, current.Value);
            if (newVal != current.Value)
            {
                foreach (var obj in targets)
                {
                    var s = getter(obj);
                    s.Value = newVal;
                    setter(obj, s);
                    EditorUtility.SetDirty(obj);
                }
            }
            EditorGUI.EndProperty();
        }

        public static void DrawFloat(Rect position, GUIContent label, SerializedProperty property, System.Func<Object, SecureFloat> getter, System.Action<Object, SecureFloat> setter)
        {
            EditorGUI.BeginProperty(position, label, property);
            var targets = property.serializedObject.targetObjects;
            var first = targets[0];
            var current = getter(first);
            float newVal = EditorGUI.FloatField(position, label, current.Value);
            if (!Mathf.Approximately(newVal, current.Value))
            {
                foreach (var obj in targets)
                {
                    var s = getter(obj);
                    s.Value = newVal;
                    setter(obj, s);
                    EditorUtility.SetDirty(obj);
                }
            }
            EditorGUI.EndProperty();
        }

        public static void DrawDouble(Rect position, GUIContent label, SerializedProperty property, System.Func<Object, SecureDouble> getter, System.Action<Object, SecureDouble> setter)
        {
            EditorGUI.BeginProperty(position, label, property);
            var targets = property.serializedObject.targetObjects;
            var first = targets[0];
            var current = getter(first);
            double newVal = EditorGUI.DoubleField(position, label, current.Value);
            if (newVal != current.Value)
            {
                foreach (var obj in targets)
                {
                    var s = getter(obj);
                    s.Value = newVal;
                    setter(obj, s);
                    EditorUtility.SetDirty(obj);
                }
            }
            EditorGUI.EndProperty();
        }

        public static void DrawBool(Rect position, GUIContent label, SerializedProperty property, System.Func<Object, SecureBool> getter, System.Action<Object, SecureBool> setter)
        {
            EditorGUI.BeginProperty(position, label, property);
            var targets = property.serializedObject.targetObjects;
            var first = targets[0];
            var current = getter(first);
            bool newVal = EditorGUI.Toggle(position, label, current.Value);
            if (newVal != current.Value)
            {
                foreach (var obj in targets)
                {
                    var s = getter(obj);
                    s.Value = newVal;
                    setter(obj, s);
                    EditorUtility.SetDirty(obj);
                }
            }
            EditorGUI.EndProperty();
        }

        public static void DrawVector2(Rect position, GUIContent label, SerializedProperty property, System.Func<Object, SecureVector2> getter, System.Action<Object, SecureVector2> setter)
        {
            EditorGUI.BeginProperty(position, label, property);
            var targets = property.serializedObject.targetObjects;
            var first = targets[0];
            var current = getter(first);
            Vector2 newVal = EditorGUI.Vector2Field(position, label, current.Value);
            if (newVal != current.Value)
            {
                foreach (var obj in targets)
                {
                    var s = getter(obj);
                    s.Value = newVal;
                    setter(obj, s);
                    EditorUtility.SetDirty(obj);
                }
            }
            EditorGUI.EndProperty();
        }

        public static void DrawVector3(Rect position, GUIContent label, SerializedProperty property, System.Func<Object, SecureVector3> getter, System.Action<Object, SecureVector3> setter)
        {
            EditorGUI.BeginProperty(position, label, property);
            var targets = property.serializedObject.targetObjects;
            var first = targets[0];
            var current = getter(first);
            Vector3 newVal = EditorGUI.Vector3Field(position, label, current.Value);
            if (newVal != current.Value)
            {
                foreach (var obj in targets)
                {
                    var s = getter(obj);
                    s.Value = newVal;
                    setter(obj, s);
                    EditorUtility.SetDirty(obj);
                }
            }
            EditorGUI.EndProperty();
        }

        public static void DrawVector4(Rect position, GUIContent label, SerializedProperty property, System.Func<Object, SecureVector4> getter, System.Action<Object, SecureVector4> setter)
        {
            EditorGUI.BeginProperty(position, label, property);
            var targets = property.serializedObject.targetObjects;
            var first = targets[0];
            var current = getter(first);
            Vector4 newVal = EditorGUI.Vector4Field(position, label, current.Value);
            if (newVal != current.Value)
            {
                foreach (var obj in targets)
                {
                    var s = getter(obj);
                    s.Value = newVal;
                    setter(obj, s);
                    EditorUtility.SetDirty(obj);
                }
            }
            EditorGUI.EndProperty();
        }
    }

    [CustomPropertyDrawer(typeof(SecureInt))]
    internal class SecureIntDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SecureDrawerUtil.DrawInt(position, label, property,
                o => (SecureInt)fieldInfo.GetValue(o),
                (o, v) => fieldInfo.SetValue(o, v));
        }
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) => EditorGUIUtility.singleLineHeight;
    }

    [CustomPropertyDrawer(typeof(SecureFloat))]
    internal class SecureFloatDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SecureDrawerUtil.DrawFloat(position, label, property,
                o => (SecureFloat)fieldInfo.GetValue(o),
                (o, v) => fieldInfo.SetValue(o, v));
        }
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) => EditorGUIUtility.singleLineHeight;
    }

    [CustomPropertyDrawer(typeof(SecureDouble))]
    internal class SecureDoubleDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SecureDrawerUtil.DrawDouble(position, label, property,
                o => (SecureDouble)fieldInfo.GetValue(o),
                (o, v) => fieldInfo.SetValue(o, v));
        }
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) => EditorGUIUtility.singleLineHeight;
    }

    [CustomPropertyDrawer(typeof(SecureBool))]
    internal class SecureBoolDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SecureDrawerUtil.DrawBool(position, label, property,
                o => (SecureBool)fieldInfo.GetValue(o),
                (o, v) => fieldInfo.SetValue(o, v));
        }
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) => EditorGUIUtility.singleLineHeight;
    }

    [CustomPropertyDrawer(typeof(SecureVector2))]
    internal class SecureVector2Drawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SecureDrawerUtil.DrawVector2(position, label, property,
                o => (SecureVector2)fieldInfo.GetValue(o),
                (o, v) => fieldInfo.SetValue(o, v));
        }
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) => EditorGUIUtility.singleLineHeight * 2f;
    }

    [CustomPropertyDrawer(typeof(SecureVector3))]
    internal class SecureVector3Drawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SecureDrawerUtil.DrawVector3(position, label, property,
                o => (SecureVector3)fieldInfo.GetValue(o),
                (o, v) => fieldInfo.SetValue(o, v));
        }
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) => EditorGUIUtility.singleLineHeight * 2f;
    }

    [CustomPropertyDrawer(typeof(SecureVector4))]
    internal class SecureVector4Drawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SecureDrawerUtil.DrawVector4(position, label, property,
                o => (SecureVector4)fieldInfo.GetValue(o),
                (o, v) => fieldInfo.SetValue(o, v));
        }
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) => EditorGUIUtility.singleLineHeight * 2f;
    }
}
#endif


