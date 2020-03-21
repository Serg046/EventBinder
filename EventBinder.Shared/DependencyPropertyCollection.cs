using System.Collections.Generic;
#if AVALONIA
	using DepProperty = Avalonia.AvaloniaProperty;
#else
	using DepProperty = System.Windows.DependencyProperty;
#endif

namespace EventBinder
{
    internal class DependencyPropertyCollection
    {
#if AVALONIA
        private readonly List<DepProperty> _depProperties = new List<DepProperty>();
#else
        private readonly List<DepProperty> _depProperties = new List<DepProperty>();
#endif
        private readonly string _propertyName;
        public DependencyPropertyCollection(string propertyName) => _propertyName = propertyName;

        public DepProperty this[int index]
        {
            get
            {
                var idx = index + 1;
                if (_depProperties.Count < idx)
                {
                    for (var i = _depProperties.Count; i < idx; i++)
                    {
#if AVALONIA
                        var property = DepProperty.RegisterAttached<DependencyPropertyCollection, Avalonia.AvaloniaObject, object>(_propertyName + i);
#else
                        var property = DepProperty.RegisterAttached(_propertyName + i, typeof(object),
	                        typeof(DependencyPropertyCollection), new System.Windows.PropertyMetadata());
#endif
                        _depProperties.Add(property);
                    }
                }
                return _depProperties[index];
            }
        }
    }
}
