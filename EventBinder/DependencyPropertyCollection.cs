using System.Collections.Generic;
using System.Windows;

namespace EventBinder
{
    internal class DependencyPropertyCollection
    {
        private readonly List<DependencyProperty> _depProperties = new List<DependencyProperty>();
        private readonly string _propertyName;
        public DependencyPropertyCollection(string propertyName) => _propertyName = propertyName;

        public DependencyProperty this[int index]
        {
            get
            {
                var idx = index + 1;
                if (_depProperties.Count < idx)
                {
                    for (var i = _depProperties.Count; i < idx; i++)
                    {
                        var property = DependencyProperty.RegisterAttached(_propertyName + i,
                            typeof(object), typeof(DependencyPropertyCollection), new PropertyMetadata());
                        _depProperties.Add(property);
                    }
                }
                return _depProperties[index];
            }
        }
    }
}
