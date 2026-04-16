using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace SpringJam.Tests.EditMode
{
    public sealed class DialogueOverlayAssetTests
    {
        private const string OverlayPath = "UI/DialogueOverlay";
        private const string PanelSettingsPath = "UI/DialoguePanelSettings";

        [Test]
        public void DialogueOverlayResources_LoadUiToolkitAssets()
        {
            Assert.That(Resources.Load<VisualTreeAsset>(OverlayPath), Is.Not.Null);
            Assert.That(Resources.Load<StyleSheet>(OverlayPath), Is.Not.Null);
            Assert.That(Resources.Load<PanelSettings>(PanelSettingsPath), Is.Not.Null);
        }

        [Test]
        public void DialogueOverlayTemplate_ContainsEditableBindings()
        {
            VisualTreeAsset overlayAsset = Resources.Load<VisualTreeAsset>(OverlayPath);

            Assert.That(overlayAsset, Is.Not.Null);

            TemplateContainer overlayTree = overlayAsset.CloneTree();
            Assert.That(overlayTree.Q<VisualElement>("prompt-shell"), Is.Not.Null);
            Assert.That(overlayTree.Q<VisualElement>("dialogue-shell"), Is.Not.Null);
            Assert.That(overlayTree.Q<Label>("prompt-label"), Is.Not.Null);
            Assert.That(overlayTree.Q<Label>("speaker-label"), Is.Not.Null);
            Assert.That(overlayTree.Q<Label>("body-label"), Is.Not.Null);
            Assert.That(overlayTree.Q<Label>("footer-label"), Is.Not.Null);
            Assert.That(overlayTree.Q<VisualElement>("journal-shell"), Is.Not.Null);
            Assert.That(overlayTree.Q<Label>("journal-title"), Is.Not.Null);
            Assert.That(overlayTree.Q<VisualElement>("sky-band"), Is.Not.Null);
            Assert.That(overlayTree.Q<VisualElement>("sun-track"), Is.Not.Null);
            Assert.That(overlayTree.Q<VisualElement>("sun-disc"), Is.Not.Null);
            Assert.That(overlayTree.Q<VisualElement>("petal-strip"), Is.Not.Null);
            Assert.That(overlayTree.Q<Label>("time-hint-label"), Is.Not.Null);
            Assert.That(overlayTree.Q<VisualElement>("task-list"), Is.Not.Null);
        }
    }
}