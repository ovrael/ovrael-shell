namespace OvraelShell.Models.Settings;

public static class CommonValidators
{
    public static bool ValidateBool(bool? nullableBool)
    {
        if (nullableBool == true)
            return true;
        return false;
    }
}
