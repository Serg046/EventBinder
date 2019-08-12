# EventBinder

[![Build status](https://ci.appveyor.com/api/projects/status/2k5lfrim0dxbekuy?svg=true)](https://ci.appveyor.com/project/Serg046/eventbinder) [![NuGet version](https://badge.fury.io/nu/EventBinder.svg)](https://www.nuget.org/packages/EventBinder)

## Getting started

Just install the nuget package. If it does not work and you get XamlParseException, then apply `[assembly: EventBinder.AssemblyReference]` attribute in your AssemblyInfo.cs.

## ICommand binding

Do you want to bind your command to any event without ugly EventTrigger, custom UI controls, huge mvvm frameworks, etc?
Try `EventBinding`:

```csharp
public class ViewModel
{
    public ViewModel() => Test = new Command();
    public Command Test { get; }

    public class Command : ICommand
    {
        public void Execute(object parameter) => Debug.WriteLine(parameter);
        public bool CanExecute(object parameter) => true;
        public event EventHandler CanExecuteChanged;
    }
}
```
```xaml
<!--xmlns:e="clr-namespace:EventBinder;assembly=EventBinder"-->
<Rectangle e:Bind.Command="{e:EventBinding Test, MouseLeftButtonDown}" Fill="LightGray"
           e:Bind.CommandParameter="{Binding ElementName=CurrentWindow, Path=ActualHeight}"/>
```

## Multiple events binding

Do you want to bind the same command to multiple events? Just separate them with comma delimiter:

```xaml
<!--xmlns:e="clr-namespace:EventBinder;assembly=EventBinder"-->
<Rectangle e:Bind.Command="{e:EventBinding Test, MouseLeftButtonDown\,MouseRightButtonDown}" Fill="LightGray"
           e:Bind.CommandParameter="{Binding ElementName=CurrentWindow, Path=ActualHeight}"/>
```

## Action binding

You do not need ICommand implementation. You can bind `Action<object>` or `Action` instead:

```csharp
public class ViewModel
{
    public Action<object> Test => parameter => Debug.WriteLine(parameter);
    public Action Test2 => () => Debug.WriteLine("Hey");
}
```

```xaml
<!--xmlns:e="clr-namespace:EventBinder;assembly=EventBinder"-->
<Rectangle e:Bind.PrmAction="{e:EventBinding Test, MouseLeftButtonDown\,MouseRightButtonDown}" Fill="LightGray"
           e:Bind.ActionParameter="{Binding ElementName=CurrentWindow, Path=ActualHeight}"/>
<Rectangle e:Bind.Action="{e:EventBinding Test2, MouseDown}" Fill="LightGray" />
```

## Task binding

If you need asynchronous action but `async () => await yourTask` does not fit because you cannot wait for completion, then bind `Func<object, Task>` or `Func<Task>`:

```csharp
public class ViewModel
{
    public Func<object, Task> Test => async parameter =>
    {
        await Task.Delay(1);
        Debug.WriteLine(parameter);
    };
    
    public Func<Task> Test2 => async () =>
    {
        await Task.Delay(1);
        Debug.WriteLine("Hey");
    };
}
```

```xaml
<!--xmlns:e="clr-namespace:EventBinder;assembly=EventBinder"-->
<Rectangle e:Bind.AwaitablePrmAction="{e:EventBinding Test, MouseLeftButtonDown\,MouseRightButtonDown}" Fill="LightGray"
           e:Bind.AwaitableActionParameter="{Binding ElementName=CurrentWindow, Path=ActualHeight}"/>
<Rectangle e:Bind.AwaitableAction="{e:EventBinding Test2, MouseDown}" Fill="LightGray" />
```
