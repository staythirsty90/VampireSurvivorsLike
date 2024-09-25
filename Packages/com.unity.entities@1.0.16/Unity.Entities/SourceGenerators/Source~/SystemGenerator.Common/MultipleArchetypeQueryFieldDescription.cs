using System;
using System.Collections.Generic;
using System.Linq;

namespace Unity.Entities.SourceGen.SystemGenerator.Common
{
    public readonly struct MultipleArchetypeQueryFieldDescription : IEquatable<MultipleArchetypeQueryFieldDescription>, IQueryFieldDescription
    {
        readonly IReadOnlyCollection<Archetype> _archetypes;
        readonly string _entityQueryBuilderBodyBeforeBuild;

        public MultipleArchetypeQueryFieldDescription(IReadOnlyCollection<Archetype> archetypes, string entityQueryBuilderBodyBeforeBuild)
        {
            _archetypes = archetypes;
            _entityQueryBuilderBodyBeforeBuild = entityQueryBuilderBodyBeforeBuild;
        }

        public string EntityQueryFieldAssignment(string generatedQueryFieldName)
        {
            var buildInvocation = $"entityQueryBuilder{_entityQueryBuilderBodyBeforeBuild}.Build(ref state);";
            return $"{generatedQueryFieldName} = {buildInvocation}{Environment.NewLine}entityQueryBuilder.Reset();";
        }

        public string GetFieldDeclaration(string generatedQueryFieldName, bool forcePublic = false)
            => $"{(forcePublic ? "public" : "")} Unity.Entities.EntityQuery {generatedQueryFieldName};";

        public bool Equals(MultipleArchetypeQueryFieldDescription other)
            => _archetypes.SequenceEqual(other._archetypes);

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;

            return obj.GetType() == GetType() && Equals((MultipleArchetypeQueryFieldDescription)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 19;

                foreach (var archetype in _archetypes)
                    hash = hash * 31 + archetype.GetHashCode();

                return hash;
            }
        }

        public static bool operator ==(MultipleArchetypeQueryFieldDescription left, MultipleArchetypeQueryFieldDescription right) => Equals(left, right);
        public static bool operator !=(MultipleArchetypeQueryFieldDescription left, MultipleArchetypeQueryFieldDescription right) => !Equals(left, right);
    }
}
