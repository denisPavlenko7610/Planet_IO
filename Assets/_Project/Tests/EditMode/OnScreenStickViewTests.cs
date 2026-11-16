using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace PlanetIO.Tests
{
    public sealed class OnScreenStickViewTests
    {
        private sealed class TestWindow : EditorWindow { }

        [UnityTest]
        public IEnumerator LeftJoystick_FixedAnchorAndFloatingFollowWork() => VerifyModes(false);

        [UnityTest]
        public IEnumerator RightJoystick_FixedAnchorAndFloatingFollowWork() => VerifyModes(true);

        private static IEnumerator VerifyModes(bool swapped)
        {
            TestWindow window = ScriptableObject.CreateInstance<TestWindow>();
            GameObject owner = new("Stick input test");
            owner.SetActive(false);
            System.Type type = typeof(StickResponse).Assembly.GetType("PlanetIO.OnScreenStickView", true);
            Component view = owner.AddComponent(type);
            try
            {
                window.position = new Rect(0, 0, 1600, 900);
                VisualElement root = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/_Project/UI/Hud.uxml").CloneTree();
                root.style.width = 1600;
                root.style.height = 900;
                root.EnableInClassList("controls-swapped", swapped);
                window.rootVisualElement.Add(root);
                window.ShowUtility();
                yield return null;
                yield return null;

                VisualElement zone = root.Q("stick-zone");
                VisualElement stick = root.Q("stick");
                VisualElement knob = root.Q("stick-knob");
                SetField(view, "_zone", zone);
                SetField(view, "_stick", stick);
                SetField(view, "_knob", knob);
                Assert.That(zone.layout.width, Is.GreaterThan(500));
                Vector2 center = zone.WorldToLocal(stick.LocalToWorld(stick.contentRect.center));
                Assert.That(swapped ? center.x > zone.layout.width / 2 : center.x < zone.layout.width / 2, Is.True);

                Invoke(view, "SetFixedJoystick", true);
                Vector2 finger = center + new Vector2(swapped ? -240 : 240, -200);
                using (PointerDownEvent down = PointerDownEvent.GetPooled(new Event
                       { type = EventType.MouseDown, button = 0, mousePosition = zone.LocalToWorld(finger) }))
                {
                    Invoke(view, "OnPointerDown", down);
                }

                Assert.That(Vector2.Distance(GetField<Vector2>(view, "_anchor"), center), Is.LessThan(0.01f));
                Invoke(view, "Drive", finger + new Vector2(90, -90));
                Assert.That(Vector2.Distance(GetField<Vector2>(view, "_anchor"), center), Is.LessThan(0.01f));
                Vector2 offset = new(knob.style.translate.value.x.value, knob.style.translate.value.y.value);
                Assert.That(offset.magnitude, Is.EqualTo(78).Within(0.01f));
                Assert.That(stick.style.left.keyword, Is.EqualTo(StyleKeyword.Null));
                Assert.That(stick.style.top.keyword, Is.EqualTo(StyleKeyword.Null));

                Invoke(view, "SetFixedJoystick", false);
                Assert.That(GetField<int>(view, "_pointerId"), Is.EqualTo(-1));
                Assert.That(zone.HasPointerCapture(0), Is.False);
                Assert.That(knob.style.translate.value.x.value, Is.Zero);
                Assert.That(knob.style.translate.value.y.value, Is.Zero);

                using (PointerDownEvent down = PointerDownEvent.GetPooled(new Event
                       { type = EventType.MouseDown, button = 0, mousePosition = zone.LocalToWorld(finger) }))
                {
                    Invoke(view, "OnPointerDown", down);
                }

                Vector2 floatingStart = GetField<Vector2>(view, "_anchor");
                Assert.That(Vector2.Distance(floatingStart, finger), Is.LessThan(0.01f));
                Invoke(view, "Drive", floatingStart + new Vector2(0, -180));
                Assert.That(Vector2.Distance(GetField<Vector2>(view, "_anchor"), floatingStart), Is.GreaterThan(50));
                Invoke(view, "SetFixedJoystick", true);
                Assert.That(GetField<int>(view, "_pointerId"), Is.EqualTo(-1));
                Assert.That(stick.ClassListContains("stick-active"), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(owner);
                window.Close();
            }
        }

        private static void Invoke(Component view, string method, object value) =>
            view.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Invoke(view, new[] { value });

        private static void SetField(Component view, string name, object value) =>
            view.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(view, value);

        private static T GetField<T>(Component view, string name) =>
            (T)view.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(view);
    }
}
