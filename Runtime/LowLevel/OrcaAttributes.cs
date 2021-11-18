using UnityEngine;
using System;

namespace Orca
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]
    public class DrawRangeIfEnumAttribute : PropertyAttribute
    {
        public string targetEnumName { get; private set; }
        public int targetEnumValue { get; private set; }
        public float max { get; private set; }
        public DrawRangeIfEnumAttribute(string targetEnumName, int targetEnumValue, float max = 1.0f)
        {
            this.targetEnumName = targetEnumName;
            this.targetEnumValue = targetEnumValue;
            this.max = max;
        }
    }   
}