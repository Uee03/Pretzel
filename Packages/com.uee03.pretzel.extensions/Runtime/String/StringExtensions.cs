using System;
using System.Text;
using System.Text.RegularExpressions;

namespace Pretzel.Extensions
{
    public static class StringExtensions
    {
        private const RegexOptions Options = RegexOptions.CultureInvariant;

        // Built on first use so that simple string checks do not pay for regex construction.
        private static Regex _emailRegex;
        private static Regex _phoneRegex;
        private static Regex _specialCharsRegex;
        private static Regex _nonAlphanumericRegex;
        private static Regex _whiteSpaceRunRegex;
        private static Regex _specialCharsExceptPunctuationRegex;
        private static Regex _alphanumericRegex;

        private static Regex EmailRegex => _emailRegex ??= new Regex(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$", Options);
        private static Regex PhoneRegex => _phoneRegex ??= new Regex(@"^\+?[1-9]\d{0,3}-?\d{1,4}-?\d{1,4}$", Options);
        private static Regex SpecialCharsRegex => _specialCharsRegex ??= new Regex(@"[^a-zA-Z0-9\s]", Options);
        private static Regex NonAlphanumericRegex => _nonAlphanumericRegex ??= new Regex(@"[^a-zA-Z0-9]+", Options);
        private static Regex WhiteSpaceRunRegex => _whiteSpaceRunRegex ??= new Regex(@"\s{2,}", Options);
        private static Regex SpecialCharsExceptPunctuationRegex => _specialCharsExceptPunctuationRegex ??= new Regex(@"[^\w\s!@&()'\-,\.:\\""]", Options);
        private static Regex AlphanumericRegex => _alphanumericRegex ??= new Regex(@"^[a-zA-Z0-9]+$", Options);

        #region NULL AND EMPTY CHECKS

        /// <summary>
        /// Check if a string is null or empty.
        /// </summary>
        /// <param name="value">The string to check</param>
        /// <param name="trim">When true, a string that only contains white space also counts as empty</param>
        public static bool IsNullOrEmpty(this string value, bool trim = false)
        {
            return trim ? string.IsNullOrWhiteSpace(value) : string.IsNullOrEmpty(value);
        }

        /// <summary>
        /// Check if a string is null, empty or only contains white space.
        /// </summary>
        public static bool IsNullOrWhiteSpace(this string value)
        {
            return string.IsNullOrWhiteSpace(value);
        }

        #endregion

        #region ENCODING

        /// <summary>
        /// Convert plain text to a byte array.
        /// </summary>
        /// <param name="value">The plain text to convert</param>
        /// <param name="encodingType">The string encoding format, default is UTF8</param>
        /// <returns>The bytes of the string, or an empty array when the string is null or empty</returns>
        public static byte[] ToBytes(this string value, EncodingType encodingType = EncodingType.UTF8)
        {
            return string.IsNullOrEmpty(value) ? Array.Empty<byte>() : encodingType.ToEncoding().GetBytes(value);
        }

        /// <summary>
        /// Convert plain text to a base 64 string.
        /// </summary>
        /// <param name="value">The plain text to convert</param>
        /// <param name="encodingType">The string encoding format, default is UTF8</param>
        public static string ToBase64(this string value, EncodingType encodingType = EncodingType.UTF8)
        {
            return string.IsNullOrEmpty(value) ? string.Empty : Convert.ToBase64String(value.ToBytes(encodingType));
        }

        /// <summary>
        /// Convert a base 64 string to plain text.
        /// </summary>
        /// <param name="base64">The base 64 string to convert</param>
        /// <param name="decodingType">The string decoding format, default is UTF8</param>
        public static string FromBase64(this string base64, EncodingType decodingType = EncodingType.UTF8)
        {
            return string.IsNullOrEmpty(base64) ? string.Empty : Convert.FromBase64String(base64).FromBytes(decodingType);
        }

        /// <summary>
        /// Convert a base 64 string to a byte array.
        /// </summary>
        public static byte[] FromBase64ToBytes(this string base64)
        {
            return string.IsNullOrEmpty(base64) ? Array.Empty<byte>() : Convert.FromBase64String(base64);
        }

        #endregion

        #region PATTERN MATCHING

        /// <summary>
        /// Check if a string matches a regular expression pattern.
        /// </summary>
        public static bool MatchesPattern(this string input, string pattern)
        {
            return !string.IsNullOrWhiteSpace(input) && Regex.IsMatch(input, pattern);
        }

        /// <summary>
        /// Check if a string contains anything other than letters, digits and white space.
        /// </summary>
        public static bool HasSpecialChars(this string value)
        {
            return !string.IsNullOrEmpty(value) && SpecialCharsRegex.IsMatch(value);
        }

        /// <summary>
        /// Remove everything except letters, digits and white space.
        /// </summary>
        /// <param name="value">The string to adjust</param>
        /// <param name="replacement">Text that replaces each removed character</param>
        public static string RemoveSpecialChars(this string value, string replacement = "")
        {
            return string.IsNullOrEmpty(value) ? value : SpecialCharsRegex.Replace(value, replacement);
        }

        /// <summary>
        /// Keep only letters and digits. White space is removed too.
        /// </summary>
        /// <param name="value">The string to adjust</param>
        /// <param name="replacement">Text that replaces each run of removed characters</param>
        public static string ToAlphanumeric(this string value, string replacement = "")
        {
            return string.IsNullOrEmpty(value) ? value : NonAlphanumericRegex.Replace(value, replacement);
        }

        /// <summary>
        /// Remove special characters but keep letters, digits, white space and the punctuation ! @ &amp; ( ) ' - , . : \ and ".
        /// Runs of white space are collapsed afterwards.
        /// </summary>
        /// <param name="value">The string to adjust</param>
        /// <param name="replacement">Text that replaces each removed character</param>
        public static string RemoveSpecialCharsKeepPunctuation(this string value, string replacement = "")
        {
            return string.IsNullOrEmpty(value) ? value : SpecialCharsExceptPunctuationRegex.Replace(value, replacement).InnerTrim();
        }

        /// <summary>
        /// Trim the ends of a string and collapse every run of white space into a single space.
        /// </summary>
        public static string InnerTrim(this string value)
        {
            return string.IsNullOrEmpty(value) ? value : WhiteSpaceRunRegex.Replace(value.Trim(), " ");
        }

        #endregion

        #region VALIDATION

        /// <summary>
        /// Check if a string only contains letters and digits.
        /// </summary>
        public static bool IsAlphanumeric(this string value)
        {
            return !string.IsNullOrEmpty(value) && AlphanumericRegex.IsMatch(value);
        }

        /// <summary>
        /// Check if a string is an absolute http or https URL.
        /// </summary>
        public static bool IsValidUrl(this string url)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out Uri uri) && (string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) || string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Check if a string looks like a phone number, such as +27-82-1234.
        /// </summary>
        public static bool IsValidPhoneNumber(this string phoneNumber)
        {
            return !string.IsNullOrEmpty(phoneNumber) && PhoneRegex.IsMatch(phoneNumber);
        }

        /// <summary>
        /// Check if a string looks like an email address. This is a format check and does not confirm the mailbox exists.
        /// </summary>
        public static bool IsValidEmail(this string email)
        {
            return !string.IsNullOrEmpty(email) && EmailRegex.IsMatch(email);
        }

        #endregion
    }
}
