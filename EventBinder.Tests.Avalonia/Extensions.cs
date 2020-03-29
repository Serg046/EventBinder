using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Rendering;
using Avalonia.VisualTree;
using Moq;

namespace EventBinder.Tests.Avalonia
{
    public static class Extensions
    {
        public static void RaiseClickEvent(this Button button)
            => button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent, button));

        public static void RaiseDetachedFromVisualTreeEvent(this IVisual element)
        {
	        var visualTreeAttachmentEventArgs = new VisualTreeAttachmentEventArgs(element, Mock.Of<IRenderRoot>());
	        element.GetType().InvokeMember("OnDetachedFromVisualTreeCore",
		        BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.InvokeMethod,
		        null, element, new object[] {visualTreeAttachmentEventArgs}, null);
        }

        public static void RaiseAttachedToVisualTreeEvent(this IVisual element)
        {
	        var visualTreeAttachmentEventArgs = new VisualTreeAttachmentEventArgs(element, Mock.Of<IRenderRoot>());
	        element.GetType().InvokeMember("OnAttachedToVisualTreeCore",
		        BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.InvokeMethod,
		        null, element, new object[] { visualTreeAttachmentEventArgs }, null);
        }
    }
}
