using Microsoft.CodeAnalysis;
using System;

namespace EvilOctane.SourceGenerators.Symbol
{
    public static class AccessibilityExtensions
    {
        public static string ToQualifier(this Accessibility self)
        {
            return self switch
            {
                Accessibility.NotApplicable => string.Empty,
                Accessibility.Private => "private",
                Accessibility.Protected => "protected",
                Accessibility.Internal => "internal",
                Accessibility.Public => "public",
                _ => throw new NotImplementedException(),
            };
        }
    }
}
