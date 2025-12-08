// 文件路径: Assets/Scripts/Managers/UIPanel.cs
using UnityEngine;
using DG.Tweening;
using System.Reflection;
using GameFramework.UI; // 引用 UIBindAttribute

namespace GameFramework.Managers
{
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class UIPanel : MonoBehaviour
    {
        protected CanvasGroup canvasGroup;
        private bool _isInitialized = false;

        public string PanelName => gameObject.name;

        /// <summary>
        /// 初始化面板：获取组件并执行自动绑定
        /// </summary>
        public virtual void Initialize()
        {
            if (_isInitialized) return;

            // 1. 获取或添加 CanvasGroup
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

            // 2. 执行反射自动绑定
            AutoBindUIComponents();

            _isInitialized = true;

            // 3. 调用子类初始化
            OnInit();
        }

        private void AutoBindUIComponents()
        {
            // 获取所有字段（包括私有）
            var fields = this.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            foreach (var field in fields)
            {
                var bindAttr = field.GetCustomAttribute<UIBindAttribute>();
                if (bindAttr == null) continue;

                // 确定查找路径：如果有指定路径则用路径，否则用字段名
                string targetPath = string.IsNullOrEmpty(bindAttr.Path) ? field.Name : bindAttr.Path;

                // 递归查找节点
                Transform targetTransform = FindChildRecursive(transform, targetPath);

                if (targetTransform == null)
                {
                    Debug.LogError($"[UIBinder] 在 {gameObject.name} 中找不到节点: {targetPath} (字段: {field.Name})");
                    continue;
                }

                // 绑定 GameObject
                if (field.FieldType == typeof(GameObject))
                {
                    field.SetValue(this, targetTransform.gameObject);
                }
                // 绑定 Component (如 Button, TextMeshProUGUI)
                else
                {
                    var component = targetTransform.GetComponent(field.FieldType);
                    if (component != null)
                    {
                        field.SetValue(this, component);
                    }
                    else
                    {
                        Debug.LogError($"[UIBinder] 节点 {targetPath} 上没有组件: {field.FieldType.Name}");
                    }
                }
            }
        }

        private Transform FindChildRecursive(Transform parent, string name)
        {
            if (parent.name.Equals(name, System.StringComparison.OrdinalIgnoreCase)) return parent;

            // 1. 查找直接子节点
            Transform result = parent.Find(name);
            if (result != null) return result;

            // 2. 递归查找
            foreach (Transform child in parent)
            {
                result = FindChildRecursive(child, name);
                if (result != null) return result;
            }
            return null;
        }

        public virtual void Show()
        {
            if (!_isInitialized) Initialize();

            gameObject.SetActive(true);
            canvasGroup.alpha = 0;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;

            OnShow();
        }

        public virtual void Hide()
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            OnHide();
            gameObject.SetActive(false);
        }

        protected void Close()
        {
            Hide();
        }

        // 生命周期钩子
        protected virtual void OnInit() { }

        protected virtual void OnShow()
        {
            // 默认淡入动画
            canvasGroup.DOFade(1, 0.3f);
        }

        protected virtual void OnHide() { }
    }
}