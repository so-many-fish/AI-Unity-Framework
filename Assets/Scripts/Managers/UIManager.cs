// 文件路径: Assets/Scripts/Managers/UIManager.cs
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System;

namespace GameFramework.Managers
{
    // 定义UI层级
    public enum UILayer
    {
        Background = 0,
        Normal = 1,     // 普通窗口 (主菜单, 背包)
        Popup = 2,      // 弹窗 (确认框, 设置)
        System = 3      // 系统级 (Loading, 顶层遮罩)
    }

    public class UIManager : Singleton<UIManager>
    {
        private Canvas _uiCanvas;
        private Dictionary<UILayer, Transform> _layerParents = new();
        private Dictionary<string, UIPanel> _panels = new();

        public async UniTask InitializeAsync()
        {
            // 1. 加载主 Canvas 预制体 (Addressables Key: "UICanvas")
            var canvasPrefab = await ResourceManager.Instance.LoadAssetAsync<GameObject>("UICanvas");
            var canvasGO = Instantiate(canvasPrefab);
            canvasGO.name = "UI_Root";
            _uiCanvas = canvasGO.GetComponent<Canvas>();
            DontDestroyOnLoad(canvasGO);

            // 2. 创建层级节点
            CreateLayers(canvasGO.transform);

            Debug.Log("UI系统初始化完成");
        }

        private void CreateLayers(Transform root)
        {
            foreach (UILayer layer in Enum.GetValues(typeof(UILayer)))
            {
                GameObject layerGO = new GameObject(layer.ToString());
                var rect = layerGO.AddComponent<RectTransform>();
                layerGO.transform.SetParent(root, false);

                // 铺满全屏
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;

                _layerParents[layer] = layerGO.transform;
            }
        }

        /// <summary>
        /// 打开面板
        /// </summary>
        /// <param name="panelKey">Addressables Key</param>
        /// <param name="layer">指定层级</param>
        public async UniTask<T> ShowPanelAsync<T>(string panelKey, UILayer layer = UILayer.Normal) where T : UIPanel
        {
            T panel;

            if (_panels.TryGetValue(panelKey, out var existingPanel))
            {
                panel = existingPanel as T;
                // 确保层级正确（可选：如果面板允许换层）
                panel.transform.SetParent(_layerParents[layer], false);
            }
            else
            {
                // 实例化到指定层级
                var panelGO = await ResourceManager.Instance.InstantiateAsync(panelKey, _layerParents[layer]);
                panel = panelGO.GetComponent<T>();

                // ★★★ 关键：实例化后立即初始化，触发反射绑定 ★★★
                panel.Initialize();

                _panels[panelKey] = panel;
            }

            // 移动到同层级最前
            panel.transform.SetAsLastSibling();
            panel.Show();

            return panel;
        }

        public void HidePanel(string panelKey)
        {
            if (_panels.TryGetValue(panelKey, out var panel))
            {
                panel.Hide();
            }
        }
    }
}