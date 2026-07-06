using Newtonsoft.Json;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;


namespace PartFinderMicroServices_BusinessLogicLayer.Infrastructure
{
    public static class StringExtensions
    {
        public static string NormaliseForHtmlId(this string value)
        {
            var normalized = value.Normalize(NormalizationForm.FormD);
            var resultBuilder = new StringBuilder();
            foreach (var character in normalized)
            {
                var category = CharUnicodeInfo.GetUnicodeCategory(character);
                if (category == UnicodeCategory.LowercaseLetter
                    || category == UnicodeCategory.UppercaseLetter
                    || category == UnicodeCategory.SpaceSeparator)
                    resultBuilder.Append(character);
            }
            return Regex.Replace(resultBuilder.ToString(), @"\s+", "");
        }
        public static bool IsNot(this string value, string checkvalue)
        {
            return value != checkvalue;
        }

        /// <summary>
        ///     Checks whether the given Email-Parameter is a valid E-Mail address.
        /// </summary>
        /// <param name="email">Parameter-string that contains an E-Mail address.</param>
        /// <returns>
        ///     True, when Parameter-string is not null and
        ///     contains a valid E-Mail address;
        ///     otherwise false.
        /// </returns>
        public static bool IsEmail(this string email)
        {
            // Regular expression, which is used to validate an E-Mail address.
            string pattern =
                @"^(([\w-]+\.)+[\w-]+|([a-zA-Z]{1}|[\w-]{2,}))@"
                + @"((([0-1]?[0-9]{1,2}|25[0-5]|2[0-4][0-9])\.([0-1]?
				[0-9]{1,2}|25[0-5]|2[0-4][0-9])\."
                + @"([0-1]?[0-9]{1,2}|25[0-5]|2[0-4][0-9])\.([0-1]?
				[0-9]{1,2}|25[0-5]|2[0-4][0-9])){1}|"
                + @"([a-zA-Z]+[\w-]+\.)+[a-zA-Z]{2,4})$";

            // updated pattern with pattern from register user input (changed by amjad to accept + in the email address
            pattern =
                @"^([a-zA-Z0-9_\-\.]+)([\+a-zA-Z0-9_\-\.]+)@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([a-zA-Z0-9\-]+\.)+))([a-zA-Z]{2,4}|[0-9]{1,3})(\]?)$";

            if (email != null) return Regex.IsMatch(email, pattern);
            else return false;
        }
        public static string GetValidEmails(this string email)
        {
            var listOfEmail = email.Split(',');
            var validEmails = "";
            foreach (var emails in listOfEmail)
            {
                if (emails != null && emails.IsEmail())
                    validEmails = validEmails.IsNotNullOrEmpty() ? validEmails + "," + emails : validEmails + emails;
            }
            return validEmails.IsNullOrEmpty() ? "dontuse@test.com" : validEmails;
        }

        /// <summary>
        ///     If item is null or empty then return this as the alternative
        /// </summary>
        /// <param name="value"></param>
        /// <param name="replacement"></param>
        /// <returns></returns>
        public static string IsNullEmptyThenReturnThis(this string value, string replacement)
        {
            if (string.IsNullOrEmpty(value))
            {
                return replacement;
            }
            return value;
        }

        public static bool IsNullOrEmpty(this string value)
        {
            return string.IsNullOrEmpty(value);
        }

        public static bool IsNullOrWhiteSpace(this string value)
        {
            return string.IsNullOrWhiteSpace(value);
        }

        public static bool IsNotNullOrEmpty(this string value)
        {
            return !string.IsNullOrEmpty(value);
        }

        public static string RemoveAccent(this string txt)
        {
            byte[] bytes = Encoding.GetEncoding("Cyrillic").GetBytes(txt);
            return Encoding.ASCII.GetString(bytes);
        }

        public static string ToJSON(this object obj)
        {
            return JsonConvert.SerializeObject(obj);
        }

        public static bool IsNumeric(this string input)
        {
            if (input.IsNullOrEmpty()) return false;
            long retNum;
            return long.TryParse(input, NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out retNum);
        }

        private static string DuplicateTicksForSql(this string s)
        {
            return s.Replace("'", "''");
        }

        /// <summary>
        /// Reduces any amount of whitespace in a row to one, will not remove leading or trailing whitespace
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string ReduceWhitespace(this string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;
            var newString = new StringBuilder();
            bool previousIsWhitespace = false;
            for (int i = 0; i < value.Length; i++)
            {
                if (char.IsWhiteSpace(value[i]))
                {
                    if (previousIsWhitespace)
                    {
                        continue;
                    }
                    previousIsWhitespace = true;
                }
                else
                {
                    previousIsWhitespace = false;
                }
                newString.Append(value[i]);
            }
            return newString.ToString();
        }

        /// <summary>
        /// Provides an easy way to build a string of several parts
        /// </summary>
        /// <param name="value"></param>
        /// <param name="addString"></param>
        /// <param name="separator"></param>
        /// <returns></returns>
        public static string FormattedConcat(this string value, string addString, string separator = ", ")
        {
            if (string.IsNullOrWhiteSpace(addString))
                return value;
            if (string.IsNullOrWhiteSpace(value))
                return addString;
            return string.Concat(value.Trim(), separator, addString.Trim());
        }

        /// <summary>
        /// Provides an easy way to build a string of several parts, adds the given string to the start
        /// </summary>
        /// <param name="value"></param>
        /// <param name="addString"></param>
        /// <param name="separator"></param>
        /// <returns></returns>
        public static string FormattedConcatPrepend(this string value, string addString, string separator = ", ")
        {
            if (string.IsNullOrWhiteSpace(addString))
                return value;
            if (string.IsNullOrWhiteSpace(value))
                return addString;
            return string.Concat(addString.Trim(), separator, value.Trim());
        }

        /// <summary>
        /// Returns true if the string is no null or only whitespace and is entirely in upper case
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool IsAllCaps(this string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;
            return string.Equals(value, value.ToUpper());
        }

        /// <summary>
        /// Returns a string with typical replacements or equivalents of null, set to null
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string NaToNull(this string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;
            var lowerVal = value.ToLower().Trim();
            if (string.Equals("na", lowerVal) || string.Equals("n/a", lowerVal) || string.Equals("n.a", lowerVal) ||
                string.Equals("n-a", lowerVal) || string.Equals("null", lowerVal) || string.Equals("not applicable", lowerVal) ||
                string.Equals("no answer", lowerVal) || string.Equals("not available", lowerVal) || string.Equals("tbc", lowerVal) ||
                string.Equals("tbd", lowerVal))
                return null;
            return value;
        }

        /// <summary>
        /// Compares the strings with all whitespace removed and ToLower
        /// </summary>
        /// <param name="value"></param>
        /// <param name="otherVal"></param>
        /// <returns></returns>
        public static bool EqualContent(this string value, string otherVal)
        {
            var valueNull = string.IsNullOrWhiteSpace(value);
            var otherNull = string.IsNullOrWhiteSpace(otherVal);
            if (valueNull && otherNull)
                return true;
            if (valueNull || otherNull)
                return false;
            var alteredValue = value.ToLower().Replace(" ", "");
            var alteredOther = otherVal.ToLower().Replace(" ", "");
            return alteredValue.Equals(alteredOther);
        }

        public static bool Similar(this string value, string otherVal)
        {
            if (value == null && otherVal == null)
                return true;
            if (value == null || otherVal == null)
                return false;

            return true;
        }

        public static string ToNotation(this string origin)
        {
            if (string.IsNullOrEmpty(origin))
                return origin;
            var value = origin.ReduceWhitespace().ToLower().Replace("%20", " ").Replace(' ', '_').Replace("/", "");
            return value;
        }

        public static string BuildUrlFromHost(this string host, bool isSecured = true, string port = null)
        {
            return string.Concat(isSecured ? "https://" : "http://", host, port ?? "");
        }

        /// <summary>
        /// Use the current culture info to convert a string to Title Case
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string ToTitleCase(this string value)
        {
            var cultureInfo = Thread.CurrentThread.CurrentCulture;
            return cultureInfo.TextInfo.ToTitleCase(value.ToLower());
        }

        public static string Base64Encode(string plainText)
        {
            var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            return Convert.ToBase64String(plainTextBytes);
        }
    }


}