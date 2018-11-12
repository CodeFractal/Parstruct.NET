using System;

namespace Parstruct.NET
{
    public class Definition
    {
        private string _default;
        public string Name { get; set; }
        public object[] Contains { get; set; }
        public object[] First { get; set; }
        public object Repeats { get; set; }
        public object Separator { get; set; }
        public int Min { get; set; }
        public int Max { get; set; } = Int32.MaxValue;
        public ENestingType Nest { get; set; } = ENestingType.None;

        internal bool Required { get; set; } = true;
        public string Default {
            get => _default;
            set {
                _default = value;
                Required = false;
            }
        }
    }
}
