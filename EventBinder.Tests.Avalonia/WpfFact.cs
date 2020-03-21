using Avalonia;
using Avalonia.Platform;
using Moq;
using Xunit;

namespace EventBinder.Tests.Avalonia
{
	public class WpfFactAttribute : FactAttribute
	{
		public WpfFactAttribute()
		{
			if (AvaloniaLocator.Current.GetService<IPlatformThreadingInterface>() == null)
			{
				var pti = new Mock<IPlatformThreadingInterface>();
				pti.Setup(p => p.CurrentThreadIsLoopThread).Returns(true);
				AvaloniaLocator.CurrentMutable.Bind<IPlatformThreadingInterface>().ToConstant(pti.Object);
			}
		}
	}
}
