using System;
using UnityEngine;

namespace Code.MainGame
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property |
                    AttributeTargets.Class | AttributeTargets.Struct, Inherited = true)]
    public class ConditionalHideAttribute : PropertyAttribute
    {
        public string ConditionalSourceField;
        public int EnumIndex;

        public ConditionalHideAttribute(string boolVariableName)
        {
            ConditionalSourceField = boolVariableName;
        }

        public ConditionalHideAttribute(string enumVariableName, int enumIndex)
        {
            ConditionalSourceField = enumVariableName;
            this.EnumIndex = enumIndex;
        }

    }
}