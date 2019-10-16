using UnityEditor;
using UnityEngine;
using System;
using System.Linq;
using System.Reflection;
using UnityEditor.ShortcutManagement;

namespace QuickEye.Utility
{
    public static class WindowCloner
    {
        private const string _projectWindowTypeName = "UnityEditor.ProjectBrowser";
        private const string _inspectorWindowTypeName = "UnityEditor.InspectorWindow";

        private static EditorWindowLocker[] _windowLockers;

        static WindowCloner()
        {
            _windowLockers = new[]
            {
            new EditorWindowLocker(_projectWindowTypeName),
            new EditorWindowLocker(_inspectorWindowTypeName)
        };
        }

#if UNITY_2019_1_OR_NEWER
    [Shortcut("Window/Clone window", KeyCode.T, ShortcutModifiers.Shift | ShortcutModifiers.Action)]
#else
        [MenuItem("Window/Clone window #%t")]
#endif
        public static void CloneWindow()
        {
            var window = EditorWindow.focusedWindow;
            var newWindow = UnityEngine.Object.Instantiate(window);

            var windowType = newWindow.GetType();
            var locker = _windowLockers.FirstOrDefault(l => l.WindowType == windowType);

            if (locker != null)
            {
                locker.LockWindow(newWindow, true);
            }

            newWindow.Show();
            SetupRect(newWindow, window.position);
        }

        private static void SetupRect(EditorWindow newWindow, Rect prevWindow)
        {
            var newX = Mathf.Clamp(prevWindow.position.x - prevWindow.size.x, 0, float.PositiveInfinity);
            var newY = prevWindow.position.y;
            var newPosition = new Vector2(newX, newY);

            newWindow.position = new Rect
            {
                size = newWindow.position.size,
                position = newPosition
            };
        }

        private class EditorWindowLocker
        {
            private const string _lockTrackerLockedPropertyName = "isLocked";
            private const string _inspectorWindowLockTrackerFieldName = "m_LockTracker";

            private FieldInfo _lockTrackerField;
            private PropertyInfo _isLockedProperty;

            private string _fullTypeName;

            public Type WindowType { get; }

            public EditorWindowLocker(string fullTypeName)
            {
                _fullTypeName = fullTypeName;

                WindowType = typeof(EditorWindow).Assembly.GetType(_fullTypeName);
                _lockTrackerField = WindowType.GetField(_inspectorWindowLockTrackerFieldName, BindingFlags.NonPublic | BindingFlags.Instance);
                _isLockedProperty = _lockTrackerField.FieldType.GetProperty(_lockTrackerLockedPropertyName, BindingFlags.NonPublic | BindingFlags.Instance);
            }

            public void LockWindow(EditorWindow window, bool isLocked)
            {
                _isLockedProperty.SetValue(_lockTrackerField.GetValue(window), isLocked);
            }
        }
    }
}