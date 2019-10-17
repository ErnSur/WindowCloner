using UnityEditor;
using UnityEngine;
using System;
using System.Reflection;
using UnityEditor.ShortcutManagement;
using System.Collections.Generic;

namespace QuickEye.Utility
{
    public static class WindowCloner
    {
        private static Dictionary<Type, EditorWindowLocker> _cachedLockers = new Dictionary<Type, EditorWindowLocker>();

#if UNITY_2019_1_OR_NEWER
        [Shortcut("Window/Clone window", KeyCode.D, ShortcutModifiers.Shift | ShortcutModifiers.Alt)]
#else
        [MenuItem("Window/Clone window #&d")]
#endif
        public static void CloneWindow()
        {
            var window = EditorWindow.focusedWindow;
            var newWindow = UnityEngine.Object.Instantiate(window);

            var windowType = newWindow.GetType();

            if (!_cachedLockers.TryGetValue(windowType, out var locker))
            {
                if (EditorWindowLocker.TryGetWindowLocker(windowType, out locker))
                {
                    _cachedLockers[windowType] = locker;
                }
            }

            locker?.LockWindow(newWindow, true);

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

            public Type WindowType { get; }

            public static bool TryGetWindowLocker(Type windowType, out EditorWindowLocker locker)
            {
                if (typeof(EditorWindow).IsAssignableFrom(windowType) && GetLockTrackerField(windowType) != null)
                {
                    locker = new EditorWindowLocker(windowType);
                    return true;
                }

                locker = null;
                return false;
            }

            //ToDo: SceneHierarchyWindow needs special handling
            private static FieldInfo GetLockTrackerField(Type windowType)
            {
                return windowType.GetField(_inspectorWindowLockTrackerFieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            }

            private EditorWindowLocker(Type windowType)
            {
                WindowType = windowType;
                _lockTrackerField = GetLockTrackerField(WindowType);
                _isLockedProperty = _lockTrackerField.FieldType.GetProperty(_lockTrackerLockedPropertyName, BindingFlags.NonPublic | BindingFlags.Instance);
            }

            public void LockWindow(EditorWindow window, bool isLocked)
            {
                _isLockedProperty.SetValue(_lockTrackerField.GetValue(window), isLocked);
            }
        }
    }
}