using System.Text.Encodings.Web;

namespace OvraelShell.Utils.Settings;

/// <summary>
/// Escapes only what JSON requires (quote, backslash, control characters). The built-in
/// encoders also escape private use characters - the Nerd Font icons would become \uXXXX.
/// </summary>
public sealed class MinimalJsonEncoder : JavaScriptEncoder
{
    public static MinimalJsonEncoder Instance { get; } = new();

    public override int MaxOutputCharactersPerInputCharacter =>
        UnsafeRelaxedJsonEscaping.MaxOutputCharactersPerInputCharacter;

    public override bool WillEncode(int unicodeScalar) =>
        unicodeScalar < 0x20 || unicodeScalar is '"' or '\\';

    public override unsafe int FindFirstCharacterToEncode(char* text, int textLength)
    {
        for (var i = 0; i < textLength; i++)
        {
            if (WillEncode(text[i]))
                return i;
        }

        return -1;
    }

    public override unsafe bool TryEncodeUnicodeScalar(
        int unicodeScalar,
        char* buffer,
        int bufferLength,
        out int numberOfCharactersWritten
    ) =>
        UnsafeRelaxedJsonEscaping.TryEncodeUnicodeScalar(
            unicodeScalar,
            buffer,
            bufferLength,
            out numberOfCharactersWritten
        );
}
