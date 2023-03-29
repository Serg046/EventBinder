# EventBinder

Do you want to bind your action to any event without ugly EventTrigger, custom UI controls, huge mvvm frameworks, etc? Try `EventBinding` which will allow you to bind your method directly to any event, including your own events, without a need to wrap the method in ICommand container.

[![NuGet Downloads](https://img.shields.io/nuget/dt/EventBinder.svg?label=downloads)](https://www.nuget.org/packages/EventBinder) [![EventBinder](https://img.shields.io/badge/EventBinder-WPF-brightgreen)](https://www.nuget.org/packages/EventBinder) [![Build status](https://ci.appveyor.com/api/projects/status/2k5lfrim0dxbekuy?svg=true)](https://ci.appveyor.com/project/Serg046/eventbinder) [![NuGet version](https://badge.fury.io/nu/EventBinder.svg)](https://www.nuget.org/packages/EventBinder) [![.NET Framework 3](https://img.shields.io/badge/.NET%20%20Framework-3+-brightgreen)](https://www.nuget.org/packages/EventBinder) [![.NET Core 3](https://img.shields.io/badge/.NET%20%20Core-3+-brightgreen)](https://www.nuget.org/packages/EventBinder)  
[![NuGet Downloads](https://img.shields.io/nuget/dt/EventBinder.Avalonia.svg?label=downloads)](https://www.nuget.org/packages/EventBinder.Avalonia) [![EventBinder.Avalonia](https://img.shields.io/badge/EventBinder-Avalonia-brightgreen)](https://www.nuget.org/packages/EventBinder.Avalonia) [![Build status](https://ci.appveyor.com/api/projects/status/2k5lfrim0dxbekuy?svg=true)](https://ci.appveyor.com/project/Serg046/eventbinder) [![NuGet version](https://badge.fury.io/nu/EventBinder.Avalonia.svg)](https://www.nuget.org/packages/EventBinder.Avalonia) [![.NET Core 2](https://img.shields.io/badge/.NET%20%20Core-2+-brightgreen)](https://www.nuget.org/packages/EventBinder.Avalonia) [![Avalonia 0.9.0](https://img.shields.io/badge/Avalonia-0.9.0+%20/%2011.0.0--preview1+-brightgreen)](https://www.nuget.org/packages/EventBinder.Avalonia)
## Getting started

Just install the nuget package. In case of `XamlParseException`, try to apply `[assembly: EventBinder.AssemblyReference]` attribute in your AssemblyInfo.cs or somewhere at global space (e.g. Program.cs).

## Features
- Binding to methods without ICommand
- Binding to methods with return types
- Binding to async methods
- Binding to nested objects using `.` delimiter (properties and fields are supported)
- Passing user parameters of int, double, decimal or string type
- Passing event parameters using `$` sign and position number (`$0`, `$1`, etc)
- Passing the current `{Binding}` as a parameter

You can find most of the features in the example below:
```csharp
public class ViewModel
{
    public MetadataViewModel Metadata { get; } = new MetadataViewModel();

    public async Task ShowMessage(string msg, decimal centenary, double year)
    {
        await Task.Delay(0);
        MessageBox.Show(msg + centenary + year);
    }

    public class MetadataViewModel
    {
        public void ShowInfo(Window window, double windowWidth, ViewModel viewModel, object sender, MouseButtonEventArgs eventArgs)
        {
            var sb = new StringBuilder("Window width: ")
                .AppendLine(windowWidth.ToString())
                .Append("View model type: ").AppendLine(viewModel.GetType().Name)
                .Append("Sender type: ").AppendLine(sender.GetType().Name)
                .Append("Clicked button: ").AppendLine(eventArgs.ChangedButton.ToString())
                .Append("Mouse X: ").AppendLine(eventArgs.GetPosition(window).X.ToString())
                .Append("Mouse Y: ").AppendLine(eventArgs.GetPosition(window).Y.ToString());
            MessageBox.Show(sb.ToString());
        }
    }
}
```
#### XAML-side binding
```xaml
<!-- Please note that the assembly for Avalonia is different. It is assembly=EventBinder.Avalonia. -->
<Window xmlns:e="clr-namespace:EventBinder;assembly=EventBinder" Name="Wnd">
    <Rectangle Fill="LightGray" Name="Rct"
        MouseLeftButtonDown="{e:EventBinding ShowMessage, `Happy `, 20m, 20.0 }"
        MouseRightButtonDown="{e:EventBinding Metadata.ShowInfo, {Binding ElementName=Wnd},
            {Binding ElementName=Wnd, Path=ActualWidth}, {Binding}, $0, $1 }" />
</Window>
```
#### C#-side binding
```csharp
EventBinding.Bind(Rct, nameof(Rct.MouseLeftButtonDown),
    nameof(ViewModel.ShowMessage),
    "`Happy `", 20m, 20.0);
EventBinding.Bind(Rct, nameof(Rct.MouseRightButtonDown),
    nameof(ViewModel.Metadata) + "." + nameof(ViewModel.Metadata.ShowInfo),
    new Binding { ElementName = nameof(Wnd)},
    new Binding("ActualWidth") { ElementName = nameof(Wnd) },
    new Binding(),
    "$0", "$1");
```
