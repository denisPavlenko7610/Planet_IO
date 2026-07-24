using System;

namespace PlanetIO.Core.Attributes
{
    [AttributeUsage(AttributeTargets.Method, Inherited = true)]
    public sealed class ButtonAttribute : Attribute
    {
        public ButtonAttribute(string label = null)
        {
            Label = label;
        }

        public string Label { get; }
    }
}
