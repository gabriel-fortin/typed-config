using System.Text;

namespace org.g14.FeatureFlags.Generation;

public static class Utils
{
    public static string ToSafeIdentifier(this string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "_";

        var sb = new StringBuilder(name.Length + 2);

        // the first char must be a letter or an underscore
        sb.Append(char.IsLetter(name[0]) ? name[0] : '_');

        // the following chars: only letters, digits, underscores
        for (int i = 1; i < name.Length; i++)
        {
            char c = name[i];
            sb.Append(char.IsLetterOrDigit(c) ? c : '_');
        }

        string safeName = sb.ToString();

        // prefix C# keywords etc. with '@'
        bool isKeyword = Microsoft.CodeAnalysis.CSharp.SyntaxFacts.GetKeywordKind(safeName) !=
            Microsoft.CodeAnalysis.CSharp.SyntaxKind.None;
        bool isContextualKeyword = Microsoft.CodeAnalysis.CSharp.SyntaxFacts.GetContextualKeywordKind(safeName) !=
            Microsoft.CodeAnalysis.CSharp.SyntaxKind.None;
        if (isKeyword || isContextualKeyword)
        {
            safeName = "@" + safeName;
        }

        return safeName;
    }
}