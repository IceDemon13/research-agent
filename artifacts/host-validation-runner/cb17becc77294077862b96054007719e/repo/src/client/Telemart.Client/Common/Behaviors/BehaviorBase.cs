using System;
using System.Windows;
using DevExpress.Mvvm.UI.Interactivity;

namespace Telemart.Client.Common.Behaviors
{
    public abstract class BehaviorBase<T> : Behavior<T>
        where T : FrameworkElement
    {
        private bool isHookedUp;
        private bool isSetup;
        private WeakReference weakTarget;

        protected override void OnChanged()
        {
            T target = AssociatedObject;

            if (target != null)
            {
                HookupBehavior(target);
            }
            else
            {
                UnHookupBehavior();
            }
        }

        protected virtual void OnCleanup()
        {
        }

        protected virtual void OnSetup()
        {
        }

        private void CleanupBehavior()
        {
            if (!isSetup)
            {
                return;
            }

            isSetup = false;

            OnCleanup();
        }

        private void HookupBehavior(T target)
        {
            if (isHookedUp)
            {
                return;
            }

            weakTarget = new WeakReference(target);

            isHookedUp = true;

            target.Unloaded += OnTargetUnloaded;
            target.Loaded += OnTargetLoaded;
        }

        private void OnTargetLoaded(object sender, RoutedEventArgs e)
        {
            SetupBehavior();
        }

        private void OnTargetUnloaded(object sender, RoutedEventArgs e)
        {
            CleanupBehavior();
        }

        private void SetupBehavior()
        {
            if (isSetup)
            {
                return;
            }

            isSetup = true;

            OnSetup();
        }

        private void UnHookupBehavior()
        {
            if (!isHookedUp)
            {
                return;
            }

            isHookedUp = false;

            T target = AssociatedObject ?? (T)weakTarget.Target;

            if (target != null)
            {
                target.Unloaded -= OnTargetUnloaded;
                target.Loaded -= OnTargetLoaded;
            }
        }
    }
}