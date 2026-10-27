using System;
using System.Collections.Generic;
using Pretzel.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace Pretzel.Samples.UIHelpers
{
    /// <summary>
    /// A main menu with an options screen, a modal dialog and toasts, built from <c>GameUIHelpers.uxml</c>.
    /// Shows <see cref="UIScreenStack"/>, <see cref="ToastService"/>, <see cref="SafeAreaElement"/> and disposable callbacks.
    /// </summary>
    public sealed class GameUIHelpersController : UIControllerBase
    {
        private readonly List<IDisposable> _bindings = new();
        private UIScreenStack _screens;
        private ToastService _toasts;

        protected override void OnBind(VisualElement root)
        {
            VisualElement menu = root.QRequired<VisualElement>("menu");
            VisualElement options = root.QRequired<VisualElement>("options-screen");
            VisualElement dialog = root.QRequired<VisualElement>("dialog");
            options.FadeOutNow();
            dialog.FadeOutNow();

            _screens = new UIScreenStack();
            _screens.Push(menu, animate: false);
            _toasts = new ToastService(root.QRequired<VisualElement>("toasts"));

            _bindings.Add(root.QRequired<Button>("play").OnClick(() => _toasts.Show("Starting...", 2f, ToastKind.Success)));
            _bindings.Add(root.QRequired<Button>("options").OnClick(() => _screens.Push(options)));
            _bindings.Add(root.QRequired<Button>("options-back").OnClick(() => _screens.Pop()));
            _bindings.Add(root.QRequired<Button>("quit-dialog").OnClick(() => _screens.Push(dialog, modal: true)));
            _bindings.Add(root.QRequired<Button>("dialog-no").OnClick(() => _screens.Pop()));
            _bindings.Add(root.QRequired<Button>("dialog-yes").OnClick(() =>
            {
                _screens.Pop();
                _toasts.Show("Quit is disabled in this sample.", 2.5f, ToastKind.Warning);
            }));
            _bindings.Add(root.QRequired<Slider>("volume").OnValueChanged(evt => AudioListener.volume = evt.newValue));
            _bindings.Add(root.RegisterDisposable<NavigationCancelEvent>(_ => _screens.Pop()));
        }

        protected override void OnUnbind(VisualElement root)
        {
            foreach (IDisposable binding in _bindings)
            {
                binding.Dispose();
            }

            _bindings.Clear();
            _toasts?.Clear();
        }
    }
}
