namespace ProgressiveBotSystem.Web.Core;

using Services;
using Shared;

internal class Utils
{
    public static IEnumerable<string> StringObjectIDValidation(string value)
    {
        if (!string.IsNullOrEmpty(value) && (value.Length != 24 || !IsHex(value)))
        {
            yield return "Invalid MongoID";
        }
    }

    public static IEnumerable<string> StringLengthValidation(string value)
    {
        if (!string.IsNullOrEmpty(value) && value.Length >= 19)
        {
            yield return "Invalid, Name too long";
        }
    }

    public static bool IsHex(IEnumerable<char> chars)
    {
        bool isHex;
        foreach (var c in chars)
        {
            isHex = c >= '0' && c <= '9' || c >= 'a' && c <= 'f' || c >= 'A' && c <= 'F';

            if (!isHex)
            {
                Console.WriteLine(isHex);
                return false;
            }
        }
        return true;
    }

    public static bool IsHexAndValidLength(string value)
    {
        if (value.Length == 24 && IsHex(value))
        {
            return true;
        }
        return false;
    }

    public static bool IsStringAndValidLength(string value)
    {
        if (value.Length <= 19)
        {
            return true;
        }
        return false;
    }

    // Still a MainLayout concern: the bottom-bar "unsaved changes" flag, which
    // has no per-field caller key and isn't tracked in PendingChanges.
    public static void UpdateViewBool(bool holder, bool actual)
    {
        if (holder != actual)
        {
            MainLayout.EnableUnsavedChangesButton();
        }
    }

    private static void UpdatePendingState(bool changed, string caller)
    {
        var state = PresetStateService.Instance;

        if (changed)
        {
            state.PendingChanges.Add(caller);
        }
        else
        {
            state.PendingChanges.Remove(caller);
        }

        state.RaisePendingChangesUpdated();
    }

    public static void UpdateView(bool holder, bool originalConfigValue, string caller)
    {
        UpdatePendingState(holder != originalConfigValue, caller);
    }

    public static void UpdateView(int holder, int originalConfigValue, string caller)
    {
        UpdatePendingState(holder != originalConfigValue, caller);
    }

    public static void UpdateView(double holder, double originalConfigValue, string caller)
    {
        UpdatePendingState(Math.Abs(holder - originalConfigValue) > 0.0001d, caller);
    }

    public static void UpdateView(string holder, string originalConfigValue, string caller)
    {
        UpdatePendingState(holder != originalConfigValue, caller);
    }

    public static void UpdateView(
        List<string> holder,
        List<string> originalConfigValue,
        string caller
    )
    {
        UpdatePendingState(!holder.SequenceEqual(originalConfigValue), caller);
    }

    public static void UpdateView(List<int> holder, List<int> originalConfigValue, string caller)
    {
        UpdatePendingState(!holder.SequenceEqual(originalConfigValue), caller);
    }

    public static void UpdateView(string caller)
    {
        var state = PresetStateService.Instance;

        if (state.PendingChanges.Contains(caller))
        {
            state.PendingChanges.Remove(caller);
        }
        else
        {
            state.PendingChanges.Add(caller);
        }

        state.RaisePendingChangesUpdated();
    }
}
