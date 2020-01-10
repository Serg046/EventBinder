using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Windows;
using System.Windows.Controls;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using Xunit;

namespace EventBinder.PerformanceTests
{
	public class EventHandlerGeneratorTests
	{
#if NETFRAMEWORK // Fails for .net core with multiple iterations, looks optimized
		[Fact]
		public void GenerateEmptyHandler_OptimizedImpl_WorksFaster()
		{
			var benchmark = BenchmarkRunner.Run<EmptyEventHandler>(new ManualConfig()
				.With(ConsoleLogger.Default).With(DefaultColumnProviders.Instance));
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
#endif

		[Fact]
		public void GenerateHandler_OptimizedImpl_WorksFaster()
		{
			var benchmark = BenchmarkRunner.Run<EventHandler>(new ManualConfig()
				.With(ConsoleLogger.Default).With(DefaultColumnProviders.Instance));
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

		private static ModuleBuilder CreateModuleBuilder()
		{
#if NET30
	        var assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName(EventBindingExtension.ASSEMBLY_NAME), AssemblyBuilderAccess.Run);
#else
			var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(EventBinding.ASSEMBLY_NAME), AssemblyBuilderAccess.Run);
#endif
			return assemblyBuilder.DefineDynamicModule(EventBinding.ASSEMBLY_NAME);
		}

		public class EmptyEventHandler
		{
			private readonly ModuleBuilder _moduleBuilder;
			private readonly Type _handlerType;
			private readonly EventHandlerGenerator _generator;

			public EmptyEventHandler()
			{
				_moduleBuilder = CreateModuleBuilder();
				_handlerType = typeof(RoutedEventHandler);
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
				var handlers = new List<Delegate>();
				for (var i = 0; i < Iterations; i++)
				{
					handlers.Add(CreateGenerator().GenerateEmptyHandler(_handlerType));
				}
				return handlers;
			}

			[Benchmark, STAThread]
			public List<Delegate> Optimized()
			{
				var handlers = new List<Delegate>();
				for (var i = 0; i < Iterations; i++)
				{
					CreateGenerator();
					handlers.Add(_generator.GenerateEmptyHandler(_handlerType));
				}
				return handlers;
			}
		}


		public class EventHandler
		{
			private readonly ModuleBuilder _moduleBuilder;
			private readonly Type _handlerType;
			private readonly EventBinding _binding;
			private readonly EventHandlerGenerator _generator;

			public EventHandler()
			{
				_moduleBuilder = CreateModuleBuilder();
				_handlerType = typeof(RoutedEventHandler);
				_binding = new EventBinding("Invoke");
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
