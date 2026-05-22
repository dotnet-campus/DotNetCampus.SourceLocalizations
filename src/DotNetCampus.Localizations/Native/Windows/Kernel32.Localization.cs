using System.Runtime.InteropServices;

namespace dotnetCampus.Localizations.Native.Windows;

internal class Kernel32
{
    /// <summary>
    /// <para>
    /// Maximum length of a locale name. The maximum number of characters allowed for this string is 85, including a terminating null character.
    /// Your application must use the constant for the maximum locale name length, instead of hard-coding the value "85".
    /// </para>
    /// <para>
    /// From: <see href="https://learn.microsoft.com/en-us/windows/win32/intl/locale-name-constants"/>
    /// </para>
    /// </summary>
    public const int LOCALE_NAME_MAX_LENGTH = 85;

    /// <summary>
    /// <para>
    /// Retrieves the user default locale name.
    /// Note
    /// The application should call this function in preference to GetUserDefaultLCID if designed to run only on Windows Vista and later.
    /// </para>
    /// <para>
    /// From: <see href="https://learn.microsoft.com/en-us/windows/win32/api/winnls/nf-winnls-getuserdefaultlocalename"/>
    /// </para>
    /// </summary>
    /// <param name="lpLocaleName">
    /// Pointer to a buffer in which this function retrieves the locale name.
    /// </param>
    /// <param name="cchLocaleName">
    /// Size, in characters, of the buffer indicated by <paramref name="lpLocaleName"/>.
    /// The maximum possible length of a locale name, including a terminating null character, is <see cref="LOCALE_NAME_MAX_LENGTH"/>.
    /// This is the recommended size to supply in this parameter.
    /// </param>
    /// <returns>
    /// Returns the size of the buffer containing the locale name, including the terminating null character, if successful.
    /// Note
    /// On single-user systems, the return value is the same as that returned by GetSystemDefaultLocaleName.
    /// The function returns 0 if it does not succeed.
    /// To get extended error information, the application can call GetLastError,
    /// which can return one of the following error codes:
    /// ERROR_INSUFFICIENT_BUFFER. A supplied buffer size was not large enough, or it was incorrectly set to NULL.
    /// </returns>
    /// <remarks>
    /// This function can retrieve data from custom locales.
    /// Data is not guaranteed to be the same from computer to computer or between runs of an application.
    /// If your application must persist or transmit data, see Using Persistent Locale Data.
    /// </remarks>
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, EntryPoint = "GetUserDefaultLocaleName", ExactSpelling = true, SetLastError = true)]
    public static extern int GetUserDefaultLocaleName([In] nint lpLocaleName, [In] int cchLocaleName);
}
