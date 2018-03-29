using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Threading;

namespace SaveToGameWpf.Logic.OrganisationItems
{
    /// <summary>
    /// Класс для работы с окнами программы
    /// </summary>
    public static class WindowManager
    {
        private static readonly Dictionary<string, Window> WindowsDict = new Dictionary<string, Window>();

        /// <summary>
        /// Creates (is not already created) and activates a window
        /// </summary>
        /// <typeparam name="T">Window type</typeparam>
        /// <param name="ownerWindow">Window owner</param>
        /// <param name="createNew">Function that creates a new window (invoked only if not already created)</param>
        public static void ActivateWindow<T>(Window ownerWindow = null, Func<T> createNew = null) where T : Window
        {
            ActivateWindow(typeof(T), ownerWindow, createNew);
        }

        /// <summary>
        /// Creates (is not already created) and activates a window
        /// </summary>
        /// <param name="windowType">Window type</param>
        /// <param name="ownerWindow">Window owner</param>
        /// <param name="createNew">Function that creates a new window (invoked only if not already created)</param>
        public static void ActivateWindow(Type windowType, Window ownerWindow = null, Func<Window> createNew = null)
        {
            string type = windowType.FullName ?? string.Empty;

            Window window;

            if (!WindowsDict.TryGetValue(type, out window))
            {
                window = createNew?.Invoke() ?? (Window) Activator.CreateInstance(windowType);
                window.Closing += ChildWindowOnClosing;
                WindowsDict.Add(type, window);
            }
            else
            {
                if (!window.IsLoaded)
                {
                    window = createNew?.Invoke() ?? (Window) Activator.CreateInstance(windowType);
                    window.Closing += ChildWindowOnClosing;
                    WindowsDict[type] = window;
                }
            }

            if (ownerWindow != null)
                window.Owner = ownerWindow;

            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() =>
            {
                if (!window.IsLoaded)
                    window.Show();

                window.Activate();
                window.Focus();

                if (window.WindowState == WindowState.Minimized)
                    window.WindowState = WindowState.Normal;
            }));
        }

        private static void ChildWindowOnClosing(object sender, CancelEventArgs cancelEventArgs)
        {
            if (!cancelEventArgs.Cancel)
                RemoveFromList(sender.GetType());
        }

        /// <summary>
        /// Закрывает окно, которое ранее было активировано методом <see cref="ActivateWindow"/>
        /// </summary>
        /// <typeparam name="T">Тип окна</typeparam>
        public static void CloseWindow<T>() where T : Window
        {
            CloseWindow(typeof(T));
        }

        /// <summary>
        /// Закрывает окно, которое ранее было активировано методом <see cref="ActivateWindow"/>
        /// </summary>
        /// <param name="windowType">Тип окна</param>
        public static void CloseWindow(Type windowType)
        {
            if (WindowsDict.TryGetValue(windowType.FullName ?? string.Empty, out Window window) && window.IsLoaded)
            {
                window.Close();
                RemoveFromList(windowType);
            }
        }

        public static void RemoveFromList<T>() where T : Window
        {
            RemoveFromList(typeof(T));
        }

        public static void RemoveFromList(Type windowType)
        {
            WindowsDict.Remove(windowType.FullName ?? string.Empty);
        }

        public static void EnableWindow<T>() where T : Window
        {
            EnableWindow(typeof(T));
        }

        public static void EnableWindow(Type windowType)
        {
            if (WindowsDict.TryGetValue(windowType.FullName ?? string.Empty, out Window window) && window.IsLoaded)
                window.IsEnabled = true;
        }

        public static void DisableWindow<T>() where T : Window
        {
            DisableWindow(typeof(T));
        }

        public static void DisableWindow(Type windowType)
        {
            if (WindowsDict.TryGetValue(windowType.FullName ?? string.Empty, out Window window) && window.IsLoaded)
                window.IsEnabled = false;
        }

        public static T GetActiveWindow<T>() where T : Window
        {
            return WindowsDict.TryGetValue(typeof(T).FullName ?? string.Empty, out Window result) ? (T)result : null;
        }
    }
}
