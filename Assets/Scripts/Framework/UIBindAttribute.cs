// 文件路径: Assets/Scripts/Framework/UIBindAttribute.cs
using System;

namespace GameFramework.UI
{
    [AttributeUsage(AttributeTargets.Field)]
    public class UIBindAttribute : Attribute
    {
        public string Path { get; private set; }

        /// <summary>
        /// 自动绑定UI组件
        /// </summary>
        /// <param name="path">UI节点路径。如果为空，默认查找与字段名相同的节点（忽略大小写）</param>
        public UIBindAttribute(string path = "")
        {
            Path = path;
        }
    }
}