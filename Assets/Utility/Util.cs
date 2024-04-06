using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Text;

static class Util
{
    /// <summary>
    /// Returns the description attribute of an enum value, if it exists.
    /// </summary>
    /// <param name="value">Enum value to return the description of.</param>
    /// <returns>A string taken from the Enum's description attribute, or an empty string, if the attribute wasn't specified.</returns>
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


    private static char[] _invalidChars;
    private static Dictionary<char, char> _lookalikeReplacements;

    /// <summary>Replaces characters in <c>name</c> that are not allowed in 
    /// file names with the specified replacement character, or lookalike unicode characters, if enabled.</summary>
    /// <param name="name">Text to make into a valid filename. The same string is returned if it is valid already.</param>
    /// <param name="genericReplacement">Replacement character, or null to remove bad characters without replacement.</param>
    /// <param name="lookalikeReplacement">Whether to replace eligible characters with the non-ASCII lookalike characters.</param>
    /// <returns>A string that can be used as a filename. If the output string would otherwise be empty, returns "_".</returns>
    public static string ToValidFileName(this string name, char? genericReplacement = '_', bool lookalikeReplacement = false)
    {
        // Empty name
        if (string.IsNullOrEmpty(name))
            return (genericReplacement ?? '_').ToString();

        // Handle reserved names
        if (name == "CON" || name == "PRN" || name == "AUX" || name == "NUL" || name == "COM1" || name == "COM2" || name == "COM3" || name == "COM4" || name == "COM5" || name == "COM6" || name == "COM7" || name == "COM8" || name == "COM9" || name == "LPT1" || name == "LPT2" || name == "LPT3" || name == "LPT4" || name == "LPT5" || name == "LPT6" || name == "LPT7" || name == "LPT8" || name == "LPT9")
            return (genericReplacement ?? '_') + name;

        // Replace invalid characters
        _invalidChars ??= Path.GetInvalidFileNameChars();
        if (lookalikeReplacement)
            _lookalikeReplacements ??= new Dictionary<char, char> { 
                { '"',  '\u201D' }, // ” right double quotation mark
                { '\'', '\u2019' }, // ’ right single quotation mark
                { '/',  '\u29F8' }, // ⧸ big solidus
                { '\\', '\u29F9' }, // ⧹ big reverse solidus
                { '<',  '\u02c2' }, // ˂ modifier letter left arrowhead
                { '>',  '\u02c3' }, // ˃ modifier letter right arrowhead
                { '*',  '\u2217' }, // ∗ asterisk operator?
                { '|',  '\u2223' }, // ∣ divides
                { ':',  '\u2236' }, // ∶ ratio

                // whitespaces
                { '\0', ' ' },      
                { '\f', ' ' }, 
                { '\t', ' ' },
                { '\n', ' ' },
                { '\r', ' ' },
                { '\v', ' ' }
            };

        int i;
        int replaceIndex = name.IndexOfAny(_invalidChars, i = 0);
        if (replaceIndex == -1) // Nothing to replace
            return name;

        StringBuilder sb = new(name.Length);

        do
        {
            sb.Append(name, i, replaceIndex - i); // append the part before the invalid character

            if (lookalikeReplacement && _lookalikeReplacements.TryGetValue(name[replaceIndex], out char lookalike))
                sb.Append(lookalike);
            else if (genericReplacement.HasValue)
                sb.Append(genericReplacement.Value);
            
            i = replaceIndex + 1;
            replaceIndex = name.IndexOfAny(_invalidChars, i);
        } while (replaceIndex != -1);

        sb.Append(name, i, name.Length - i);

        return sb.ToString();
    }

    public static string SurroundWithQuotes(this string text)
    {
        return $"\"{text}\"";
    }
}