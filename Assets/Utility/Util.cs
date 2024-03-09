using System;
using System.ComponentModel;
using System.Reflection;

static class Util
{
    public static string ToDescription(this Enum value)
    {
        if (value == null)
        {
            return string.Empty;
        }

        if (!Enum.IsDefined(value.GetType(), value))
        {
            return string.Empty;
        }

        FieldInfo fieldInfo = value.GetType().GetField(value.ToString());
        if (fieldInfo != null)
        {
            DescriptionAttribute[] attributes =
                fieldInfo.GetCustomAttributes(typeof(DescriptionAttribute), false) as DescriptionAttribute[];
            if (attributes != null && attributes.Length > 0)
            {
                return attributes[0].Description;
            }
        }

        return value.ToString();
    }
}