using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Loggers;

namespace EventBinder.PerformanceTests
{
	public class BenchmarkConfig : ManualConfig
	{
		public BenchmarkConfig()
		{
			Add(new Logger());
			Add(DefaultColumnProviders.Instance);
		}

		private class Logger : ILogger
		{
			private bool ShouldWrite(LogKind logKind) => logKind != LogKind.Default && logKind != LogKind.Info;

			public void Write(LogKind logKind, string text)
			{
				if (ShouldWrite(logKind))
				{
					ConsoleLogger.Default.Write(logKind, text);
				}
			}

			public void WriteLine(LogKind logKind, string text)
			{
				if (ShouldWrite(logKind))
				{
					ConsoleLogger.Default.WriteLine(logKind, text);
				}
			}

			public void WriteLine() => ConsoleLogger.Default.WriteLine();
			public void Flush() => ConsoleLogger.Default.Flush();
		}
	}
}
