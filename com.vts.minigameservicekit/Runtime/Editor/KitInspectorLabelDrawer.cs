using System.Collections;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace VTS.MiniGameKit.EditorDrawer
{
    [CustomPropertyDrawer(typeof(KitInspectorLabelAttribute))]
    public class KitInspectorLabelDrawer : PropertyDrawer
    {
        // 带子字段的属性（列表元素、可展开的类）展开时，高度必须覆盖全部子字段，
        // 否则元素行只分到一行高，子字段没有绘制空间（List 元素展开空白的根因）
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, includeChildren: true);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            KitInspectorLabelAttribute labelAttr = (KitInspectorLabelAttribute)attribute;
            string text = labelAttr.name;

            // 2022.3 起写在 List/数组字段上的特性 Drawer 作用于每个元素而非列表本身，
            // 此时给标签补上元素下标，避免所有元素显示同一个名字
            if (fieldInfo != null && typeof(IList).IsAssignableFrom(fieldInfo.FieldType))
            {
                int index = GetElementIndex(property.propertyPath);
                if (index >= 0)
                    text = $"{labelAttr.name} {index}";
            }

            GUIContent customLabel = new(text) { tooltip = label != null ? label.tooltip : string.Empty };

            // 检查字段上是否同时存在 RangeAttribute
            RangeAttribute rangeAttr = fieldInfo?.GetCustomAttribute<RangeAttribute>();
            if (rangeAttr != null)
            {
                // 接管Range 的Drawer，手动绘制Slider并绘制自定义标签
                switch (property.propertyType)
                {
                    case SerializedPropertyType.Float:
                        EditorGUI.Slider(position, property, rangeAttr.min, rangeAttr.max, customLabel);
                        return;
                    case SerializedPropertyType.Integer:
                        EditorGUI.IntSlider(position, property, (int)rangeAttr.min, (int)rangeAttr.max, customLabel);
                        return;
                }
            }

            EditorGUI.PropertyField(position, property, customLabel, includeChildren: true);
        }

        // propertyPath 形如 "products.Array.data[12]"，取末尾的元素下标；非元素路径返回 -1
        private static int GetElementIndex(string path)
        {
            int end = path.LastIndexOf(']');
            if (end < 0)
                return -1;
            int start = path.LastIndexOf('[', end);
            return start >= 0 && int.TryParse(path.Substring(start + 1, end - start - 1), out int index) ? index : -1;
        }
    }
}
