using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Windows;
using System.Windows.Controls;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using Xunit;

namespace EventBinder.Tests
{
	public class PerformanceTests
	{
		[Fact]
		public void GenerateHandler_OptimizedImpl_WorksFaster()
		{
			var benchmark = BenchmarkRunner.Run<EventHandler>(new ManualConfig().With(ConsoleLogger.Default));
			var results = benchmark.Reports.GroupBy(c => c.BenchmarkCase.Parameters[nameof(EventHandler.Iterations)]);
			foreach (var result in results)
			{
				var nonOptimized = result.Single(r => r.BenchmarkCase.Descriptor
					.WorkloadMethodDisplayInfo == nameof(EventHandler.NonOptimized));
				var optimized = result.Single(r => r.BenchmarkCase.Descriptor
					.WorkloadMethodDisplayInfo == nameof(EventHandler.Optimized));

				var ratio = nonOptimized.ResultStatistics.Median / optimized.ResultStatistics.Median;
				Assert.True(ratio > (int)result.Key, $"Expected {result.Key} but actual {ratio}");
			}
		}

		public class EventHandler
		{
			private readonly ModuleBuilder _moduleBuilder;
			private readonly Type _handlerType;
			private readonly EventBindingExtension _binding;
			private readonly EventHandlerGenerator _generator;

			public EventHandler()
			{
				_moduleBuilder = AppDomain.CurrentDomain
					.DefineDynamicAssembly(new AssemblyName(EventBindingExtension.ASSEMBLY_NAME), AssemblyBuilderAccess.RunAndSave)
					.DefineDynamicModule(EventBindingExtension.ASSEMBLY_NAME);
				_handlerType = typeof(RoutedEventHandler);
				_binding = new EventBindingExtension("Invoke");
				_generator = CreateGenerator();
			}

			private EventHandlerGenerator CreateGenerator()
			{
				return new EventHandlerGenerator(_moduleBuilder);
			}

			[Params(9, 17)]
			public int Iterations { get; set; }

			[IterationCleanup]
			public void IterationCleanup()
			{
				// Release memory, reduce side effect
			}

			[Benchmark, STAThread]
			public List<Delegate> NonOptimized()
			{
				var button = new Button { DataContext = new Action(() => { }) };
				var handlers = new List<Delegate>();
				for (var i = 0; i < Iterations; i++)
				{
					handlers.Add(CreateGenerator().GenerateHandler(_handlerType, _binding, button));
				}
				return handlers;
			}

			[Benchmark, STAThread]
			public List<Delegate> Optimized()
			{
				var button = new Button { DataContext = new Action(() => { }) };
				var handlers = new List<Delegate>();
				for (var i = 0; i < Iterations; i++)
				{
					CreateGenerator();
					handlers.Add(_generator.GenerateHandler(_handlerType, _binding, button));
				}
				return handlers;
			}
		}
	}
}
