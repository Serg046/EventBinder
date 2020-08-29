using System;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Loggers;

namespace EventBinder.PerformanceTests
{
	public class BenchmarkConfig : ManualConfig
	{
		public BenchmarkConfig()
		{
			AddLogger(new Logger());
			AddColumnProvider(DefaultColumnProviders.Instance);
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
			public string Id { get; } = Guid.NewGuid().ToString();
			public int Priority { get; } = 0;
		}
	}
}
