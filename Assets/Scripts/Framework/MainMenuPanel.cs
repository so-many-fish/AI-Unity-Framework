// 文件路径: Assets/Scripts/Framework/MainMenuPanel.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using GameFramework.Managers;

namespace GameFramework.UI
{
    public class MainMenuPanel : UIPanel
    {
        // === 自动绑定区域 ===

        // 1. 按名称绑定：Prefab中必须有名为 "StartButton" 的节点
        [UIBind]
        private Button StartButton;

        // 2. 指定路径绑定：当名字重复或层级很深时使用
        [UIBind("Buttons/SettingsButton")]
        private Button _settingsBtn;

        [UIBind]
        private Button QuitButton;

        [UIBind]
        private TextMeshProUGUI TitleText;

        // ===================

        // 替代 Start()，在此处添加事件监听
        protected override void OnInit()
        {
            base.OnInit();

            // 此时所有 [UIBind] 的字段都已被赋值
            StartButton.onClick.AddListener(OnPlayClicked);
            _settingsBtn.onClick.AddListener(OnSettingsClicked);
            QuitButton.onClick.AddListener(OnQuitClicked);

            TitleText.text = "My Game Title";
        }

        protected override void OnShow()
        {
            base.OnShow();

            // 播放进场动画
            PlayIntroAnimation();
        }

        private void PlayIntroAnimation()
        {
            // 重置状态
            StartButton.transform.localScale = Vector3.zero;
            _settingsBtn.transform.localScale = Vector3.zero;
            QuitButton.transform.localScale = Vector3.zero;

            // 序列动画
            Sequence seq = DOTween.Sequence();
            seq.Append(TitleText.transform.DOPunchScale(Vector3.one * 0.2f, 0.5f));
            seq.Append(StartButton.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack));
            seq.Join(_settingsBtn.transform.DOScale(1f, 0.3f).SetDelay(0.1f).SetEase(Ease.OutBack));
            seq.Join(QuitButton.transform.DOScale(1f, 0.3f).SetDelay(0.2f).SetEase(Ease.OutBack));
        }

        private async void OnPlayClicked()
        {
            // 进入游戏场景
            await Managers.SceneManager.Instance.LoadSceneAsync("GameScene");
            // 关闭自己
            Close();
            // 打开 HUD
           // await Managers.UIManager.Instance.ShowPanelAsync<GameHUDPanel>("GameHUDPanel", Managers.UILayer.Normal);
        }

        private async void OnSettingsClicked()
        {
            // 叠加打开设置面板
            //await Managers.UIManager.Instance.ShowPanelAsync<SettingsPanel>("SettingsPanel", Managers.UILayer.Popup);
        }

        private void OnQuitClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}