using System;
using System.Collections.Immutable;
using System.Text;

namespace EvilOctane.SourceGenerators.Type
{
    public sealed class TypeName : IEquatable<TypeName>, IComparable<TypeName>
    {
        public static readonly TypeName None = new();

        /// <summary>
        /// <see langword="class"/>, <see langword="interface"/>, <see langword="struct"/> or <see langword="record"/> containing this type.
        /// </summary>
        public readonly TypeName? ContainingType = null;

        /// <summary>
        /// As the index increases, we get further away from the global namespace.
        /// </summary>
        public readonly ImmutableArray<string> CommaSeparatedNamespace = ImmutableArray<string>.Empty;

        /// <summary>
        /// Generic type arguments if this is a generic symbol.
        /// </summary>
        public readonly ImmutableArray<TypeName> GenericTypeArguments = ImmutableArray<TypeName>.Empty;

        /// <summary>
        /// Class, struct, etc. name
        /// </summary>
        public readonly string Name = string.Empty;

        public bool IsContainedWithinAnotherType => ContainingType is not null;
        public bool IsInNamespace => !CommaSeparatedNamespace.IsEmpty;
        public bool IsGenericType => !GenericTypeArguments.IsEmpty;

        public TypeName()
        {
        }

        public TypeName(string name)
        {
            Name = name;
        }

        public TypeName(string @namespace, string name)
        {
            CommaSeparatedNamespace = @namespace.Split('.').ToImmutableArray();
            Name = name;
        }

        public TypeName(ImmutableArray<string> commaSeparatedNamespace, string name) : this(commaSeparatedNamespace, name, ImmutableArray<TypeName>.Empty)
        {
            CommaSeparatedNamespace = commaSeparatedNamespace;
            Name = name;
        }

        public TypeName(ImmutableArray<string> commaSeparatedNamespace, string name, ImmutableArray<TypeName> genericTypeArguments)
        {
            CommaSeparatedNamespace = commaSeparatedNamespace;
            Name = name;
            GenericTypeArguments = genericTypeArguments;
        }

        public TypeName(TypeName containingType, string name)
        {
            ContainingType = containingType;
            CommaSeparatedNamespace = containingType.CommaSeparatedNamespace;
            Name = name;
        }

        public TypeName(TypeName containingType, string name, ImmutableArray<TypeName> genericTypeArguments)
        {
            ContainingType = containingType;
            CommaSeparatedNamespace = containingType.CommaSeparatedNamespace;
            Name = name;
            GenericTypeArguments = genericTypeArguments;
        }

        public static ReadOnlySpan<char> TrimNamespace(ReadOnlySpan<char> @namespace, ReadOnlySpan<char> relativeTo)
        {
            if (@namespace.StartsWith(relativeTo))
            {
                ReadOnlySpan<char> namespaceTrimmed = @namespace.Slice(relativeTo.Length);

                if (!namespaceTrimmed.IsEmpty)
                {
                    if (namespaceTrimmed.StartsWith(".".AsSpan()))
                    {
                        namespaceTrimmed = namespaceTrimmed.Slice(1);
                    }
                    else if (namespaceTrimmed.StartsWith("::".AsSpan()))
                    {
                        namespaceTrimmed = namespaceTrimmed.Slice(2);
                    }
                }

                return namespaceTrimmed;
            }
            else
            {
                return @namespace;
            }
        }

        public bool Equals(TypeName? other)
        {
            if (other is null)
            {
                return false;
            }
            else if (ReferenceEquals(this, other))
            {
                return true;
            }

            return
                ContainingType == other.ContainingType &&
                CommaSeparatedNamespace.AsSpan().SequenceEqual(other.CommaSeparatedNamespace.AsSpan()) &&
                Name == other.Name &&
                GenericTypeArguments.AsSpan().SequenceEqual(other.GenericTypeArguments.AsSpan());
        }

        public int CompareTo(TypeName? other)
        {
            if (other is null)
            {
                return 1;
            }
            else if (ReferenceEquals(this, other))
            {
                return 0;
            }

            int cmpIsContained = IsContainedWithinAnotherType.CompareTo(other.IsContainedWithinAnotherType);

            if (cmpIsContained != 0)
            {
                return cmpIsContained;
            }

            if (IsContainedWithinAnotherType && other.IsContainedWithinAnotherType)
            {
                int cmpContainingType = ContainingType!.CompareTo(other.ContainingType!);

                if (cmpContainingType != 0)
                {
                    return cmpContainingType;
                }
            }

            int cmpNamespaces = CommaSeparatedNamespace.AsSpan().SequenceCompareTo(other.CommaSeparatedNamespace.AsSpan());

            if (cmpNamespaces != 0)
            {
                return cmpNamespaces;
            }

            int cmpNames = Name.CompareTo(other.Name);

            if (cmpNames != 0)
            {
                return cmpNames;
            }

            //
            return GenericTypeArguments.AsSpan().SequenceCompareTo(other.GenericTypeArguments.AsSpan());
        }

        public override bool Equals(object? obj)
        {
            return obj is TypeName other && Equals(other);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override string ToString()
        {
            StringBuilder codeBuilder = new();
            ExpandWithGenericTypeArguments(codeBuilder);
            return codeBuilder.ToString();
        }

        public TypeName WithGenericTypeArguments(params TypeName[] arguments)
        {
            ImmutableArray<TypeName> genericTypeArguments = arguments.ToImmutableArray();

            return IsContainedWithinAnotherType ?
                new TypeName(ContainingType!, Name, genericTypeArguments) :
                new TypeName(CommaSeparatedNamespace, Name, genericTypeArguments);
        }

        public string GetNamespace()
        {
            return IsInNamespace ? string.Join(".", CommaSeparatedNamespace) : string.Empty;
        }

        public string GetFullNameAsIdentifier(string separator = "_", bool includeNamespaces = true)
        {
            // Unity.Collections.NativeArray<int>
            // Unity_Collections_NativeArray_int

            StringBuilder sb = new();
            ExpandWithGenericTypeArguments(sb, includeNamespaces);

            string expanded = sb.ToString();
            _ = sb.Clear();

            foreach (char c in expanded)
            {
                switch (c)
                {
                    case '.':
                    case '<':
                        _ = sb.Append(separator);
                        break;

                    case '>':
                        break;

                    default:
                        _ = sb.Append(c);
                        break;
                }
            }

            return sb.ToString();
        }

        public void ExpandNoGenericTypeArguments(StringBuilder stringBuilder, bool includeNamespaces = true)
        {
            if (IsContainedWithinAnotherType)
            {
                if (ContainingType!.IsGenericType)
                {
                    throw new InvalidOperationException("Type is contained in generic class.");
                }

                ContainingType.ExpandNoGenericTypeArguments(stringBuilder, includeNamespaces);
                _ = stringBuilder.Append('.');
            }
            else if (includeNamespaces)
            {
                // Only top level

                foreach (string @namespace in CommaSeparatedNamespace)
                {
                    _ = stringBuilder.AppendFormat("{0}.", @namespace);
                }
            }

            _ = stringBuilder.Append(Name);
        }

        public void ExpandWithGenericTypeArguments(StringBuilder stringBuilder, bool includeNamespaces = true)
        {
            ExpandNoGenericTypeArguments(stringBuilder, includeNamespaces);

            if (IsGenericType)
            {
                _ = stringBuilder.Append('<');

                for (int index = 0; index != GenericTypeArguments.Length; ++index)
                {
                    if (index != 0)
                    {
                        _ = stringBuilder.Append(", ");
                    }

                    TypeName typeArgument = GenericTypeArguments[index];
                    typeArgument.ExpandWithGenericTypeArguments(stringBuilder, includeNamespaces);
                }

                _ = stringBuilder.Append('>');
            }
        }

        public static bool operator ==(TypeName? rhs, TypeName? lhs)
        {
            return rhs is null ? lhs is null : rhs.Equals(lhs);
        }

        public static bool operator !=(TypeName? rhs, TypeName? lhs)
        {
            return !(rhs == lhs);
        }
    }
}
