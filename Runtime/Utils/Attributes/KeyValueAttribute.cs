using UnityEngine;

namespace PBUnityMultiplayer.Runtime.Utils.Attributes
{
    public class KeyValueAttribute : PropertyAttribute
    {
        public readonly string PropertyName;

        public KeyValueAttribute(string propertyName)
        {
            PropertyName = propertyName;
        }
    }
}