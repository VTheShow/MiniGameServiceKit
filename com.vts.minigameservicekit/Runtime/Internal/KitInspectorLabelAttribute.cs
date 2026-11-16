using System;
using UnityEngine;

namespace VTS.MiniGameKit
{
    /// <summary>
    /// 字段在 Inspector 显示的自定义名称（包内副本，独立于游戏工程的同名 InspectorLabelAttribute，
    /// 两者共存互不干扰；Drawer 见 Runtime/Editor/KitInspectorLabelDrawer.cs）
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class KitInspectorLabelAttribute : PropertyAttribute
    {
        public string name;
        public KitInspectorLabelAttribute(string name)
        {
            this.name = name;
        }
    }
}
