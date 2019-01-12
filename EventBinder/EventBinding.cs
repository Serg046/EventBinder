using System;
using System.Windows.Data;

namespace EventBinder
{
    public class EventBinding : Binding
    {
        private string _eventPath;

        public EventBinding() : base() {}
        public EventBinding(string path, string eventPath) : base(path) => EventPath = eventPath ?? throw new ArgumentNullException(nameof(eventPath));

        public string EventPath
        {
            get => _eventPath;
            set => _eventPath = value ?? throw new ArgumentNullException(nameof(EventPath));
        }
    }
}
