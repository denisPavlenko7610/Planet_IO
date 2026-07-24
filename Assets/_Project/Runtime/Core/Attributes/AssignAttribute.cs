using System;
using UnityEngine;

namespace PlanetIO.Core.Attributes
{
    public enum AssignMode : byte
    {
        Local,
        Parent,
        Children,
        Scene
    }

    [AttributeUsage(AttributeTargets.Field)]
    public sealed class AssignAttribute : PropertyAttribute
    {
        public AssignAttribute(AssignMode mode = AssignMode.Local)
        {
            Mode = mode;
        }

        public AssignMode Mode { get; }
    }
}
