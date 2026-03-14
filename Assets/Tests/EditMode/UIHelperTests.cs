using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using MatchThree.Core;

namespace MatchThree.Tests
{
    [TestFixture]
    public class UIHelperTests
    {
        [TearDown]
        public void TearDown()
        {
            // Clean up any EventSystems created during tests
            foreach (var es in Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None))
                Object.DestroyImmediate(es.gameObject);
            // Clean up any canvases
            foreach (var c in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
                Object.DestroyImmediate(c.gameObject);
        }

        [Test]
        public void EnsureEventSystem_CreatesOneWhenMissing()
        {
            Assert.IsNull(Object.FindAnyObjectByType<EventSystem>());

            UIHelper.EnsureEventSystem();

            Assert.IsNotNull(Object.FindAnyObjectByType<EventSystem>());
        }

        [Test]
        public void EnsureEventSystem_DoesNotDuplicateIfAlreadyExists()
        {
            UIHelper.EnsureEventSystem();
            UIHelper.EnsureEventSystem();

            var all = Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
            Assert.AreEqual(1, all.Length);
        }

        [Test]
        public void EnsureEventSystem_IncludesStandaloneInputModule()
        {
            UIHelper.EnsureEventSystem();

            var es = Object.FindAnyObjectByType<EventSystem>();
            Assert.IsNotNull(es.GetComponent<StandaloneInputModule>());
        }

        [Test]
        public void CreateCanvas_ReturnsCanvasWithRequiredComponents()
        {
            var canvas = UIHelper.CreateCanvas("TestCanvas");

            Assert.IsNotNull(canvas);
            Assert.IsNotNull(canvas.GetComponent<UnityEngine.UI.CanvasScaler>());
            Assert.IsNotNull(canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>());
            Assert.AreEqual(RenderMode.ScreenSpaceOverlay, canvas.renderMode);
        }

        [Test]
        public void CreateText_SetsPropertiesCorrectly()
        {
            var canvas = UIHelper.CreateCanvas();
            var text = UIHelper.CreateText(canvas.transform, "Hello", 24, Color.red, TextAnchor.UpperLeft);

            Assert.AreEqual("Hello", text.text);
            Assert.AreEqual(24, text.fontSize);
            Assert.AreEqual(Color.red, text.color);
            Assert.AreEqual(TextAnchor.UpperLeft, text.alignment);
            Assert.IsNotNull(text.font);
        }

        [Test]
        public void CreateButton_HasImageAndButtonComponents()
        {
            var canvas = UIHelper.CreateCanvas();
            bool clicked = false;
            var button = UIHelper.CreateButton(canvas.transform, "Click Me", 20, () => clicked = true);

            Assert.IsNotNull(button);
            Assert.IsNotNull(button.GetComponent<UnityEngine.UI.Image>());

            // Simulate click
            button.onClick.Invoke();
            Assert.IsTrue(clicked);
        }

        [Test]
        public void CreateButton_HasLabelText()
        {
            var canvas = UIHelper.CreateCanvas();
            var button = UIHelper.CreateButton(canvas.transform, "Test Label", 18, () => { });

            var text = button.GetComponentInChildren<UnityEngine.UI.Text>();
            Assert.IsNotNull(text);
            Assert.AreEqual("Test Label", text.text);
        }
    }
}
