/*
 * Date: 2012-12-20
 * Sources used:
 *  http://www.codeproject.com/KB/audio-video/mpegaudioinfo.aspx
 *  http://www.id3.org/Id3v2-00
 *  http://www.id3.org/Id3v2.3.0
 *  http://www.id3.org/id3guide
 *  http://www.id3.org/Id3v2.4.0-structure
 *  http://www.id3.org/Id3v2.4.0-frames
 *  http://www.id3.org/Id3v2.4.0-changes
 *  http://internet.ls-la.net/folklore/url-regexpr.html
 */
using System;
using System.Linq;
using System.Text.RegularExpressions;

using AudioVideoLib.IO;

namespace AudioVideoLib.Tags
{
    /// <summary>
    /// Class used to store an <see cref="Id3v2Tag"/> frame.
    /// </summary>
    /// <remarks>
    /// A frame is a block of information in an <see cref="Id3v2Tag"/>.
    /// </remarks>
    public partial class Id3v2Frame
    {
        private static readonly Regex UrlRegEx =
            new Regex(
                @"(?:http://(?:(?:(?:(?:(?:[a-zA-Z\d](?:(?:[a-zA-Z\d]|-)*[a-zA-Z\d])?)\.)*(?:[a-zA-Z](?:(?:[a-zA-Z\d]|-)*[a-zA-Z\d])?))|(?:(?:\d+)(?:\.(?:\d+)){3}))(?::(?:\d+))?)(?:/(?:(?:(?:(?:[a-zA-Z\d$\-_.+!*'(),]|(?:%[a-fA-F\d]{2}))|[;:@&=])*)(?:/(?:(?:(?:[a-zA-Z\d$\-_.+!*'(),]|(?:%[a-fA-F\d]{2}))|[;:@&=])*))*)(?:\?(?:(?:(?:[a-zA-Z\d$\-_.+!*'(),]|(?:%[a-fA-F\d]{2}))|[;:@&=])*))?)?)|(?:ftp://(?:(?:(?:(?:(?:[a-zA-Z\d$\-_.+!*'(),]|(?:%[a-fA-F\d]{2}))|[;?&=])*)(?::(?:(?:(?:[a-zA-Z\d$\-_.+!*'(),]|(?:%[a-fA-F\d]{2}))|[;?&=])*))?@)?(?:(?:(?:(?:(?:[a-zA-Z\d](?:(?:[a-zA-Z\d]|-)*[a-zA-Z\d])?)\.)*(?:[a-zA-Z](?:(?:[a-zA-Z\d]|-)*[a-zA-Z\d])?))|(?:(?:\d+)(?:\.(?:\d+)){3}))(?::(?:\d+))?))(?:/(?:(?:(?:(?:[a-zA-Z\d$\-_.+!*'(),]|(?:%[a-fA-F\d]{2}))|[?:@&=])*)(?:/(?:(?:(?:[a-zA-Z\d$\-_.+!*'(),]|(?:%[a-fA-F\d]{2}))|[?:@&=])*))*)(?:;type=[AIDaid])?)?)|(?:news:(?:(?:(?:(?:[a-zA-Z\d$\-_.+!*'(),]|(?:%[a-fA-F\d]{2}))|[;/?:&=])+@(?:(?:(?:(?:[a-zA-Z\d](?:(?:[a-zA-Z\d]|-)*[a-zA-Z\d])?)\.)*(?:[a-zA-Z](?:(?:[a-zA-Z\d]|-)*[a-zA-Z\d])?))|(?:(?:\d+)(?:\.(?:\d+)){3})))|(?:[a-zA-Z](?:[a-zA-Z\d]|[_.+-])*)|\*))|(?:nntp://(?:(?:(?:(?:(?:[a-zA-Z\d](?:(?:[a-zA-Z\d]|-)*[a-zA-Z\d])?)\.)*(?:[a-zA-Z](?:(?:[a-zA-Z\d]|-)*[a-zA-Z\d])?))|(?:(?:\d+)(?:\.(?:\d+)){3}))(?::(?:\d+))?)/(?:[a-zA-Z](?:[a-zA-Z\d]|[_.+-])*)(?:/(?:\d+))?)|(?:telnet://(?:(?:(?:(?:(?:[a-zA-Z\d$\-_.+!*'(),]|(?:%[a-fA-F\d]{2}))|[;?&=])*)(?::(?:(?:(?:[a-zA-Z\d$\-_.+!*'(),]|(?:%[a-fA-F\d]{2}))|[;?&=])*))?@)?(?:(?:(?:(?:(?:[a-zA-Z\d](?:(?:[a-zA-Z\d]|-)*[a-zA-Z\d])?)\.)*(?:[a-zA-Z](?:(?:[a-zA-Z\d]|-)*[a-zA-Z\d])?))|(?:(?:\d+)(?:\.(?:\d+)){3}))(?::(?:\d+))?))/?)|(?:gopher://(?:(?:(?:(?:(?:[a-zA-Z\d](?:(?:[a-zA-Z\d]|-)*[a-zA-Z\d])?)\.)*(?:[a-zA-Z](?:(?:[a-zA-Z\d]|-)*[a-zA-Z\d])?))|(?:(?:\d+)(?:\.(?:\d+)){3}))(?::(?:\d+))?)(?:/(?:[a-zA-Z\d$\-_.+!*'(),;/?:@&=]|(?:%[a-fA-F\d]{2}))(?:(?:(?:[a-zA-Z\d$\-_.+!*'(),;/?:@&=]|(?:%[a-fA-F\d]{2}))*)(?:%09(?:(?:(?:[a-zA-Z\d$\-_.+!*'(),]|(?:%[a-fA-F\d]{2}))|[;:@&=])*)(?:%09(?:(?:[a-zA-Z\d$\-_.+!*'(),;/?:@&=]|(?:%[a-fA-F\d]{2}))*))?)?)?)?)|(?:wais://(?:(?:(?:(?:(?:[a-zA-Z\d](?:(?:[a-zA-Z\d]|-)*[a-zA-Z\d])?)\.)*(?:[a-zA-Z](?:(?:[a-zA-Z\d]|-)*[a-zA-Z\d])?))|(?:(?:\d+)(?:\.(?:\d+)){3}))(?::(?:\d+))?)/(?:(?:[a-zA-Z\d$\-_.+!*'(),]|(?:%[a-fA-F\d]{2}))*)(?:(?:/(?:(?:[a-zA-Z\d$\-_.+!*'(),]|(?:%[a-fA-F\d]{2}))*)/(?:(?:[a-zA-Z\d$\-_.+!*'(),]|(?:%[a-fA-F\d]{2}))*))|\?(?:(?:(?:[a-zA-Z\d$\-_.+!*'(),]|(?:%[a-fA-F\d]{2}))|[;:@&=])*))?)|(?:mailto:(?:(?:[a-zA-Z\d$\-_.+!*'(),;/?:@&=]|(?:%[a-fA-F\d]{2}))+))|(?:file://(?:(?:(?:(?:(?:[a-zA-Z\d](?:(?:[a-zA-Z\d]|-)*[a-zA-Z\d])?)\.)*(?:[a-zA-Z](?:(?:[a-zA-Z\d]|-)*[a-zA-Z\d])?))|(?:(?:\d+)(?:\.(?:\d+)){3}))|localhost)?/(?:(?:(?:(?:[a-zA-Z\d$\-_.+!*'(),]|(?:%[a-fA-F\d]{2}))|[?:@&=])*)(?:/(?:(?:(?:[a-zA-Z\d$\-_.+!*'(),]|(?:%[a-fA-F\d]{2}))|[?:@&=])*))*))|(?:prospero://(?:(?:(?:(?:(?:[a-zA-Z\d](?:(?:[a-zA-Z\d]|-)*[a-zA-Z\d])?)\.)*(?:[a-zA-Z](?:(?:[a-zA-Z\d]|-)*[a-zA-Z\d])?))|(?:(?:\d+)(?:\.(?:\d+)){3}))(?::(?:\d+))?)/(?:(?:(?:(?:[a-zA-Z\d$\-_.+!*'(),]|(?:%[a-fA-F\d]{2}))|[?:@&=])*)(?:/(?:(?:(?:[a-zA-Z\d$\-_.+!*'(),]|(?:%[a-fA-F\d]{2}))|[?:@&=])*))*)(?:(?:;(?:(?:(?:[a-zA-Z\d$\-_.+!*'(),]|(?:%[a-fA-F\d]{2}))|[?:@&])*)=(?:(?:(?:[a-zA-Z\d$\-_.+!*'(),]|(?:%[a-fA-F\d]{2}))|[?:@&])*)))*)|(?:ldap://(?:(?:(?:(?:(?:(?:[a-zA-Z\d](?:(?:[a-zA-Z\d]|-)*[a-zA-Z\d])?)\.)*(?:[a-zA-Z](?:(?:[a-zA-Z\d]|-)*[a-zA-Z\d])?))|(?:(?:\d+)(?:\.(?:\d+)){3}))(?::(?:\d+))?))?/(?:(?:(?:(?:(?:(?:(?:[a-zA-Z\d]|%(?:3\d|[46][a-fA-F\d]|[57][Aa\d]))|(?:%20))+|(?:OID|oid)\.(?:(?:\d+)(?:\.(?:\d+))*))(?:(?:%0[Aa])?(?:%20)*)=(?:(?:%0[Aa])?(?:%20)*))?(?:(?:[a-zA-Z\d$\-_.+!*'(),]|(?:%[a-fA-F\d]{2}))*))(?:(?:(?:%0[Aa])?(?:%20)*)\+(?:(?:%0[Aa])?(?:%20)*)(?:(?:(?:(?:(?:[a-zA-Z\d]|%(?:3\d|[46][a-fA-F\d]|[57][Aa\d]))|(?:%20))+|(?:OID|oid)\.(?:(?:\d+)(?:\.(?:\d+))*))(?:(?:%0[Aa])?(?:%20)*)=(?:(?:%0[Aa])?(?:%20)*))?(?:(?:[a-zA-Z\d$\-_.+!*'(),]|(?:%[a-fA-F\d]{2}))*)))*)(?:(?:(?:(?:%0[Aa])?(?:%20)*)(?:[;,])(?:(?:%0[Aa])?(?:%20)*))(?:(?:(?:(?:(?:(?:[a-zA-Z\d]|%(?:3\d|[46][a-fA-F\d]|[57][Aa\d]))|(?:%20))+|(?:OID|oid)\.(?:(?:\d+)(?:\.(?:\d+))*))(?:(?:%0[Aa])?(?:%20)*)=(?:(?:%0[Aa])?(?:%20)*))?(?:(?:[a-zA-Z\d$\-_.+!*'(),]|(?:%[a-fA-F\d]{2}))*))(?:(?:(?:%0[Aa])?(?:%20)*)\+(?:(?:%0[Aa])?(?:%20)*)(?:(?:(?:(?:(?:[a-zA-Z\d]|%(?:3\d|[46][a-fA-F\d]|[57][Aa\d]))|(?:%20))+|(?:OID|oid)\.(?:(?:\d+)(?:\.(?:\d+))*))(?:(?:%0[Aa])?(?:%20)*)=(?:(?:%0[Aa])?(?:%20)*))?(?:(?:[a-zA-Z\d$\-_.+!*'(),]|(?:%[a-fA-F\d]{2}))*)))*))*(?:(?:(?:%0[Aa])?(?:%20)*)(?:[;,])(?:(?:%0[Aa])?(?:%20)*))?)(?:\?(?:(?:(?:(?:[a-zA-Z\d$\-_.+!*'(),]|(?:%[a-fA-F\d]{2}))+)(?:,(?:(?:[a-zA-Z\d$\-_.+!*'(),]|(?:%[a-fA-F\d]{2}))+))*)?)(?:\?(?:base|one|sub)(?:\?(?:((?:[a-zA-Z\d$\-_.+!*'(),;/?:@&=]|(?:%[a-fA-F\d]{2}))+)))?)?)?)|(?:(?:z39\.50[rs])://(?:(?:(?:(?:(?:[a-zA-Z\d](?:(?:[a-zA-Z\d]|-)*[a-zA-Z\d])?)\.)*(?:[a-zA-Z](?:(?:[a-zA-Z\d]|-)*[a-zA-Z\d])?))|(?:(?:\d+)(?:\.(?:\d+)){3}))(?::(?:\d+))?)(?:/(?:(?:(?:[a-zA-Z\d$\-_.+!*'(),]|(?:%[a-fA-F\d]{2}))+)(?:\+(?:(?:[a-zA-Z\d$\-_.+!*'(),]|(?:%[a-fA-F\d]{2}))+))*(?:\?(?:(?:[a-zA-Z\d$\-_.+!*'(),]|(?:%[a-fA-F\d]{2}))+))?)?(?:;esn=(?:(?:[a-zA-Z\d$\-_.+!*'(),]|(?:%[a-fA-F\d]{2}))+))?(?:;rs=(?:(?:[a-zA-Z\d$\-_.+!*'(),]|(?:%[a-fA-F\d]{2}))+)(?:\+(?:(?:[a-zA-Z\d$\-_.+!*'(),]|(?:%[a-fA-F\d]{2}))+))*)?))|(?:cid:(?:(?:(?:[a-zA-Z\d$\-_.+!*'(),]|(?:%[a-fA-F\d]{2}))|[;?:@&=])*))|(?:mid:(?:(?:(?:[a-zA-Z\d$\-_.+!*'(),]|(?:%[a-fA-F\d]{2}))|[;?:@&=])*)(?:/(?:(?:(?:[a-zA-Z\d$\-_.+!*'(),]|(?:%[a-fA-F\d]{2}))|[;?:@&=])*))?)|(?:vemmi://(?:(?:(?:(?:(?:[a-zA-Z\d](?:(?:[a-zA-Z\d]|-)*[a-zA-Z\d])?)\.)*(?:[a-zA-Z](?:(?:[a-zA-Z\d]|-)*[a-zA-Z\d])?))|(?:(?:\d+)(?:\.(?:\d+)){3}))(?::(?:\d+))?)(?:/(?:(?:(?:[a-zA-Z\d$\-_.+!*'(),]|(?:%[a-fA-F\d]{2}))|[/?:@&=])*)(?:(?:;(?:(?:(?:[a-zA-Z\d$\-_.+!*'(),]|(?:%[a-fA-F\d]{2}))|[/?:@&])*)=(?:(?:(?:[a-zA-Z\d$\-_.+!*'(),]|(?:%[a-fA-F\d]{2}))|[/?:@&])*))*))?)|(?:imap://(?:(?:(?:(?:(?:(?:(?:[a-zA-Z\d$\-_.+!*'(),]|(?:%[a-fA-F\d]{2}))|[&=~])+)(?:(?:;[Aa][Uu][Tt][Hh]=(?:\*|(?:(?:(?:[a-zA-Z\d$\-_.+!*'(),]|(?:%[a-fA-F\d]{2}))|[&=~])+))))?)|(?:(?:;[Aa][Uu][Tt][Hh]=(?:\*|(?:(?:(?:[a-zA-Z\d$\-_.+!*'(),]|(?:%[a-fA-F\d]{2}))|[&=~])+)))(?:(?:(?:(?:[a-zA-Z\d$\-_.+!*'(),]|(?:%[a-fA-F\d]{2}))|[&=~])+))?))@)?(?:(?:(?:(?:(?:[a-zA-Z\d](?:(?:[a-zA-Z\d]|-)*[a-zA-Z\d])?)\.)*(?:[a-zA-Z](?:(?:[a-zA-Z\d]|-)*[a-zA-Z\d])?))|(?:(?:\d+)(?:\.(?:\d+)){3}))(?::(?:\d+))?))/(?:(?:(?:(?:(?:(?:[a-zA-Z\d$\-_.+!*'(),]|(?:%[a-fA-F\d]{2}))|[&=~:@/])+)?;[Tt][Yy][Pp][Ee]=(?:[Ll](?:[Ii][Ss][Tt]|[Ss][Uu][Bb])))|(?:(?:(?:(?:[a-zA-Z\d$\-_.+!*'(),]|(?:%[a-fA-F\d]{2}))|[&=~:@/])+)(?:\?(?:(?:(?:[a-zA-Z\d$\-_.+!*'(),]|(?:%[a-fA-F\d]{2}))|[&=~:@/])+))?(?:(?:;[Uu][Ii][Dd][Vv][Aa][Ll][Ii][Dd][Ii][Tt][Yy]=(?:[1-9]\d*)))?)|(?:(?:(?:(?:[a-zA-Z\d$\-_.+!*'(),]|(?:%[a-fA-F\d]{2}))|[&=~:@/])+)(?:(?:;[Uu][Ii][Dd][Vv][Aa][Ll][Ii][Dd][Ii][Tt][Yy]=(?:[1-9]\d*)))?(?:/;[Uu][Ii][Dd]=(?:[1-9]\d*))(?:(?:/;[Ss][Ee][Cc][Tt][Ii][Oo][Nn]=(?:(?:(?:[a-zA-Z\d$\-_.+!*'(),]|(?:%[a-fA-F\d]{2}))|[&=~:@/])+)))?)))?)|(?:nfs:(?:(?://(?:(?:(?:(?:(?:[a-zA-Z\d](?:(?:[a-zA-Z\d]|-)*[a-zA-Z\d])?)\.)*(?:[a-zA-Z](?:(?:[a-zA-Z\d]|-)*[a-zA-Z\d])?))|(?:(?:\d+)(?:\.(?:\d+)){3}))(?::(?:\d+))?)(?:(?:/(?:(?:(?:(?:(?:[a-zA-Z\d\$\-_.!~*'(),])|(?:%[a-fA-F\d]{2})|[:@&=+])*)(?:/(?:(?:(?:[a-zA-Z\d\$\-_.!~*'(),])|(?:%[a-fA-F\d]{2})|[:@&=+])*))*)?)))?)|(?:/(?:(?:(?:(?:(?:[a-zA-Z\d\$\-_.!~*'(),])|(?:%[a-fA-F\d]{2})|[:@&=+])*)(?:/(?:(?:(?:[a-zA-Z\d\$\-_.!~*'(),])|(?:%[a-fA-F\d]{2})|[:@&=+])*))*)?))|(?:(?:(?:(?:(?:[a-zA-Z\d\$\-_.!~*'(),])|(?:%[a-fA-F\d]{2})|[:@&=+])*)(?:/(?:(?:(?:[a-zA-Z\d\$\-_.!~*'(),])|(?:%[a-fA-F\d]{2})|[:@&=+])*))*)?)))",
                RegexOptions.IgnoreCase);

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Determines whether the identifier is valid for the specified version.
        /// </summary>
        /// <param name="version">The version.</param>
        /// <param name="identifier">The identifier.</param>
        /// <returns>
        ///   <c>true</c> if the identifier is valid for the specified version; otherwise, <c>false</c>.
        /// </returns>
        /// <remarks>
        /// A valid identifier is made out of the characters capital A-Z and 0-9, and is 3 or 4 bytes long depending on the <see cref="Version"/>.
        /// <para />
        /// Use <see cref="GetIdentifierFieldLength"/> to get the required field length of the <paramref name="identifier"/>.
        /// </remarks>
        public static bool IsValidIdentifier(Id3v2Version version, string identifier)
        {
            if (identifier == null)
                throw new ArgumentNullException("identifier");

            return (identifier.Length == GetIdentifierFieldLength(version)) && identifier.All(c => IsValidIdentifierByte((short)c));
        }

        /// <summary>
        /// Determines whether the language code is a valid language code according to the ISO-639-2 list.
        /// </summary>
        /// <param name="languageCode">The language code.</param>
        /// <returns>
        /// <c>true</c> if the language code is valid; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsValidLanguageCode(string languageCode)
        {
            if (languageCode == null)
                throw new ArgumentNullException("languageCode");

            return (languageCode.Length == 3) && LanguageCodes.ContainsKey(languageCode);
        }

        /// <summary>
        /// Determines whether the country code is a valid country code according to the ISO-3166-1 alpha-2 list.
        /// </summary>
        /// <param name="countryCode">The country code.</param>
        /// <returns>
        /// <c>true</c> if the country code is valid; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsValidCountryCode(string countryCode)
        {
            if (countryCode == null)
                throw new ArgumentNullException("countryCode");

            return (countryCode.Length == 2) && CountryCodes.ContainsKey(countryCode);
        }

        /// <summary>
        /// Determines whether the currency code is a valid currency code according to the ISO-4217 list.
        /// </summary>
        /// <param name="currencyCode">The currency code.</param>
        /// <returns>
        /// <c>true</c> if the currency code is valid; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsValidCurrencyCode(string currencyCode)
        {
            if (currencyCode == null)
                throw new ArgumentNullException("currencyCode");

            return (currencyCode.Length == 3) && CurrencyCodes.ContainsKey(currencyCode);
        }

        /// <summary>
        /// Determines whether the URL is a valid URL according to RFC 1738.
        /// </summary>
        /// <param name="url">The uniform resource locator (URL).</param>
        /// <returns>
        /// <c>true</c> if the URL is a valid URL according to RFC 1738; otherwise, <c>false</c>.
        /// </returns>
        /// Credits: http://internet.ls-la.net/folklore/url-regexpr.html
        public static bool IsValidUrl(string url)
        {
            if (url == null)
                throw new ArgumentNullException("url");

            return UrlRegEx.IsMatch(url);
        }

        /// <summary>
        /// Determines whether the string is a valid <see cref="Id3v2FrameEncodingType.Default"/> string.
        /// </summary>
        /// <param name="textString">The string to check.</param>
        /// <param name="newLineAllowed">if set to <c>true</c>, new lines are allowed in the string.</param>
        /// <returns>
        /// <c>true</c> if the string is a valid <see cref="Id3v2FrameEncodingType.Default"/> string; otherwise, <c>false</c>.
        /// </returns>
        /// <remarks>
        /// an <see cref="Id3v2FrameEncodingType.Default"/> string is represented as characters in the range 0x20 - 0xFF.
        /// In <see cref="Id3v2FrameEncodingType.Default"/> a new line is represented, when allowed, with 0x0A only.
        /// </remarks>
        public static bool IsValidDefaultTextString(string textString, bool newLineAllowed)
        {
            if (textString == null)
                throw new ArgumentNullException("textString");

            return IsValidTextString(textString, Id3v2FrameEncodingType.Default, newLineAllowed);
        }

        /// <summary>
        /// Determines whether the string is a valid string in the specified frame encoding type and if it contains new line characters.
        /// </summary>
        /// <param name="textString">The string to check.</param>
        /// <param name="frameEncodingType">Type of the frame encoding.</param>
        /// <param name="newLineAllowed">if set to <c>true</c>, new lines are allowed in the string.</param>
        /// <returns>
        /// <c>true</c> if the string is a valid string in the specified frame encoding type; otherwise, <c>false</c>.
        /// </returns>
        /// <remarks>
        /// An <see cref="Id3v2FrameEncodingType.Default"/> string is represented as characters in the range 0x20 - 0xFF.
        /// In <see cref="Id3v2FrameEncodingType.Default"/> and <see cref="Id3v2FrameEncodingType.UTF8"/> 
        /// a new line is represented, when allowed, with 0x0A only.
        /// In <see cref="Id3v2FrameEncodingType.UTF16LittleEndian"/> a new line is represented, when allowed, with 0x0A 0x00.
        /// In <see cref="Id3v2FrameEncodingType.UTF16BigEndian"/> and <see cref="Id3v2FrameEncodingType.UTF16BigEndianWithoutBom"/> 
        /// a new line is represented, when allowed, with 0x00 0x0A.
        /// </remarks>
        public static bool IsValidTextString(string textString, Id3v2FrameEncodingType frameEncodingType, bool newLineAllowed)
        {
            if (textString == null)
                throw new ArgumentNullException("textString");

            switch (frameEncodingType)
            {
                case Id3v2FrameEncodingType.Default:
                    return textString.All(c => ((c >= (char)0x20) && (c <= (char)0xFF)) || (newLineAllowed && (c == '\n')));

                case Id3v2FrameEncodingType.UTF16LittleEndian:
                case Id3v2FrameEncodingType.UTF16BigEndian:
                case Id3v2FrameEncodingType.UTF16BigEndianWithoutBom:
                case Id3v2FrameEncodingType.UTF7:
                case Id3v2FrameEncodingType.UTF8:
                    return textString.All(c => (newLineAllowed && (c == '\n')) || (c != '\0'));

                default:
                    return true;
            }
        }

        /// <summary>
        /// Determines whether the given time stamp is a valid <see cref="Id3v2TimeStampFormat"/> time stamp format.
        /// </summary>
        /// <param name="timeStampFormat">The time stamp format.</param>
        /// <returns>
        /// <c>true</c> if the time stamp format is a valid time stamp format; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsValidTimeStampFormat(Id3v2TimeStampFormat timeStampFormat)
        {
            return Enum.TryParse(timeStampFormat.ToString(), true, out timeStampFormat);
        }

        /// <summary>
        /// Determines whether the <paramref name="identifierByte"/> is a valid identifier byte.
        /// </summary>
        /// <param name="identifierByte">The identifier byte.</param>
        /// <returns>
        /// <c>true</c> if the identifier byte is valid; otherwise, <c>false</c>.
        /// </returns>
        /// <remarks>
        /// A valid identifier byte is a byte between A-Z or 0-9.
        /// </remarks>
        public static bool IsValidIdentifierByte(short identifierByte)
        {
            return ((identifierByte >= 'A') && (identifierByte <= 'Z')) || ((identifierByte >= '0') && (identifierByte <= '9'));
        }

        /// <summary>
        /// Determines whether the <paramref name="version"/> is a valid version.
        /// </summary>
        /// <param name="version">The version.</param>
        /// <returns>
        /// <c>true</c> if the version is valid; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsValidVersion(Id3v2Version version)
        {
            return Enum.TryParse(version.ToString(), true, out version);
        }

        /// <summary>
        /// Gets the length of the <see cref="Identifier" />, in bytes.
        /// </summary>
        /// <param name="version">The version.</param>
        /// <returns>The length of the identifier field.</returns>
        /// <remarks>
        /// The length of the name field is 3 bytes for <see cref="Id3v2Version.Id3v220" />
        /// and 4 bytes for <see cref="Id3v2Version.Id3v230" /> and later.
        /// </remarks>
        public static int GetIdentifierFieldLength(Id3v2Version version)
        {
            return (version < Id3v2Version.Id3v230) ? 3 : 4;
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets the length of the data size field.
        /// </summary>
        /// <param name="version">The version.</param>
        /// <returns>
        /// The length for the data size field.
        /// </returns>
        private static int GetDataSizeFieldLength(Id3v2Version version)
        {
            return (version < Id3v2Version.Id3v230) ? 3 : 4;
        }

        /// <summary>
        /// Gets the size of the frame header for the <see cref="Id3v2Version"/>.
        /// </summary>
        /// <param name="version">The <see cref="Id3v2Version"/>.</param>
        /// <returns>
        /// The size of the frame header; in bytes.
        /// </returns>
        private static int GetHeaderSize(Id3v2Version version)
        {
            return (version < Id3v2Version.Id3v230) ? 6 : 10;
        }

        /// <summary>
        /// Gets the proper size of the frame as best as possible.
        /// </summary>
        /// <param name="version">The version.</param>
        /// <param name="frameSize">Size of the frame.</param>
        /// <param name="maximumFrameSize">Maximum size of the frame.</param>
        /// <returns>
        /// The proper size of the frame; as best as possible. There are programs writing incorrect frame size's in a ID3v2Tag.
        /// This function tries to find the best frame size as possible; based on some theory and test files.
        /// </returns>
        private static int GetFrameSize(Id3v2Version version, long frameSize, long maximumFrameSize)
        {
            //  The frame ID is followed by a size descriptor containing the size of the data in the final frame, after encryption, compression and unsynchronization.
            // The size is excluding the frame header ('total frame size' - 10 bytes) and stored as a 32 bit synchsafe integer.
            if (version >= Id3v2Version.Id3v240)
            {
                // Check if all the bytes are valid unsynched bytes (the last bit {most left bit of the first byte in Windows Calculator} should be 0)
                byte[] frameSizeBytes = BitConverter.GetBytes(frameSize);
                if (frameSizeBytes.All(b => (b & 0x80) == 0))
                    return (int)Id3v2Tag.GetUnsynchedValue(frameSizeBytes, 0, 4);

                // Some badly written ID3v2 programs write the frame size as-is; i.e. synched. They don't bother to read the specs properly -_-
                // frame sizes like '0xAAFB0000' probably only need to be reversed...
                if (frameSize >= 0x1000000)
                {
                    long size = StreamBuffer.SwitchEndianness(frameSize, 4);
                    return (int)((size > frameSize) ? frameSize : Math.Min(size, maximumFrameSize));
                }
            }
            return (int)Math.Min(frameSize, maximumFrameSize);
        }
    }
}
