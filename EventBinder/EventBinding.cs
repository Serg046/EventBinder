using System;
using System.Windows.Data;

namespace EventBinder
{
    public class EventBinding : Binding
    {
        public EventBinding(string eventPath, string methodPath) : base()
        {
            EventPath = eventPath ?? throw new ArgumentNullException(nameof(eventPath));
            MethodPath = methodPath;
            Arguments = new object[0];
        }

        public EventBinding(string eventPath, string methodPath, object arg) : base()
        {
            EventPath = eventPath ?? throw new ArgumentNullException(nameof(eventPath));
            MethodPath = methodPath;
            Arguments = new[] {arg};
        }

        public EventBinding(string eventPath, string methodPath, object arg1, object arg2) : base()
        {
            EventPath = eventPath ?? throw new ArgumentNullException(nameof(eventPath));
            MethodPath = methodPath;
            Arguments = new[] { arg1, arg2 };
        }

        public EventBinding(string eventPath, string methodPath, object arg1, object arg2, object arg3) : base()
        {
            EventPath = eventPath ?? throw new ArgumentNullException(nameof(eventPath));
            MethodPath = methodPath;
            Arguments = new[] { arg1, arg2, arg3 };
        }

        public EventBinding(string eventPath, string methodPath, object arg1, object arg2, object arg3, object arg4) : base()
        {
            EventPath = eventPath ?? throw new ArgumentNullException(nameof(eventPath));
            MethodPath = methodPath;
            Arguments = new[] { arg1, arg2, arg3, arg4 };
        }

        public EventBinding(string eventPath, string methodPath, object arg1, object arg2, object arg3, object arg4, object arg5) : base()
        {
            EventPath = eventPath ?? throw new ArgumentNullException(nameof(eventPath));
            MethodPath = methodPath;
            Arguments = new[] { arg1, arg2, arg3, arg4, arg5 };
        }

        public EventBinding(string eventPath, string methodPath, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6) : base()
        {
            EventPath = eventPath ?? throw new ArgumentNullException(nameof(eventPath));
            MethodPath = methodPath;
            Arguments = new[] { arg1, arg2, arg3, arg4, arg5, arg6 };
        }

        public string EventPath { get; }
        public string MethodPath { get; }
        public object[] Arguments { get; }
    }
}
