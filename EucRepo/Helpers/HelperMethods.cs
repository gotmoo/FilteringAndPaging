using System.Diagnostics.CodeAnalysis;
using System.Security.Principal;

namespace EucRepo.Helpers;

public static class HelperMethods
{
    [SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
    public static string ResolveSid(string item)
    {
        var account = new NTAccount(item.Split(new[] { "\\" }, StringSplitOptions.None)[0], item.Split(new[] { "\\" }, StringSplitOptions.None)[1]);
        SecurityIdentifier sid = (SecurityIdentifier)account.Translate(typeof(SecurityIdentifier));
        return sid.Value;
    }
}
