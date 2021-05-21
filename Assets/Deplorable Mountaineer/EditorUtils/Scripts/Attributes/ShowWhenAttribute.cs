#region

using System;
using UnityEngine;

#endregion

namespace Deplorable_Mountaineer.EditorUtils.Attributes {
    /// <summary>
    ///     Attribute used to show or hide the Field depending on certain conditions
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class ShowWhenAttribute : PropertyAttribute {
        public readonly object ComparationValue;
        public readonly object[] ComparationValueArray;
        public readonly string ConditionFieldName;

        /// <summary>
        ///     Attribute used to show or hide the Field depending on certain conditions
        /// </summary>
        /// <param name="conditionFieldName">Name of the bool condition Field</param>
        public ShowWhenAttribute(string conditionFieldName){
            this.ConditionFieldName = conditionFieldName;
        }

        /// <summary>
        ///     Attribute used to show or hide the Field depending on certain conditions
        /// </summary>
        /// <param name="conditionFieldName">Name of the Field to compare (bool, enum, int or float)</param>
        /// <param name="comparationValue">Value to compare</param>
        public ShowWhenAttribute(string conditionFieldName, object comparationValue = null){
            this.ConditionFieldName = conditionFieldName;
            this.ComparationValue = comparationValue;
        }

        /// <summary>
        ///     Attribute used to show or hide the Field depending on certain conditions
        /// </summary>
        /// <param name="conditionFieldName">Name of the Field to compare (bool, enum, int or float)</param>
        /// <param name="comparationValueArray">Array of values to compare (only for enums)</param>
        public ShowWhenAttribute(string conditionFieldName,
            object[] comparationValueArray = null){
            this.ConditionFieldName = conditionFieldName;
            this.ComparationValueArray = comparationValueArray;
        }
    }
}