using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Unity.Entities.SourceGen.Common;

namespace Unity.Entities.SourceGen.SystemGenerator.Common
{
    public readonly struct SingleArchetypeQueryFieldDescription : IEquatable<SingleArchetypeQueryFieldDescription>, IQueryFieldDescription
    {
        readonly Archetype _archetype;
        readonly IReadOnlyCollection<Query> _changeFilterTypes;
        readonly string _queryStorageFieldName;

        public string GetFieldDeclaration(string generatedQueryFieldName, bool forcePublic = false)
            => $"{(forcePublic ? "public" : "")} global::Unity.Entities.EntityQuery {generatedQueryFieldName};";

        public SingleArchetypeQueryFieldDescription(
            Archetype archetype,
            IReadOnlyCollection<Query> changeFilterTypes = null,
            string queryStorageFieldName = null)
        {
            _archetype = archetype;
            _changeFilterTypes = changeFilterTypes ?? Array.Empty<Query>();
            _queryStorageFieldName = queryStorageFieldName;
        }

        public bool Equals(SingleArchetypeQueryFieldDescription other)
        {
            return _archetype.Equals(other._archetype)
                   && _changeFilterTypes.SequenceEqual(other._changeFilterTypes)
                   && _queryStorageFieldName == other._queryStorageFieldName;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            if (obj.GetType() != GetType())
                return false;
            return Equals((SingleArchetypeQueryFieldDescription)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 19;
                hash = hash * 31 + _archetype.GetHashCode();

                foreach (var changeFilterType in _changeFilterTypes)
                    hash = hash * 31 + changeFilterType.GetHashCode();

                return hash;
            }
        }
        public static bool operator ==(SingleArchetypeQueryFieldDescription left, SingleArchetypeQueryFieldDescription right) => Equals(left, right);
        public static bool operator !=(SingleArchetypeQueryFieldDescription left, SingleArchetypeQueryFieldDescription right) => !Equals(left, right);

        /// <summary>
        /// Use EntityQueryBuilder to create the query
        /// Support creating queries with aspects
        /// </summary>
        /// <param name="systemStateName"></param>
        /// <param name="generatedQueryFieldName"></param>
        /// <returns></returns>
        string EntityQueryWithEntityQueryBuilder(string generatedQueryFieldName)
        {
            var code = new System.Text.StringBuilder();
            var codeAspect = new System.Text.StringBuilder();
            code.AppendLine($@"{generatedQueryFieldName} = new global::Unity.Entities.EntityQueryBuilder(global::Unity.Collections.Allocator.Temp)");
            foreach (var comp in _archetype.All.Concat(_changeFilterTypes))
                if (comp.TypeSymbol.IsAspect())
                    codeAspect.AppendLine($".WithAspect<{comp.TypeSymbol.ToFullName()}>()");
                else
                    code.AppendLine($@".WithAll{(comp.IsReadOnly ? "" : "RW")}<{comp.TypeSymbol.ToFullName()}>()");
            foreach (var comp in _archetype.Any)
                code.AppendLine($@".WithAny<{comp.TypeSymbol.ToFullName()}>()");
            foreach (var comp in _archetype.None)
                code.AppendLine($@".WithNone<{comp.TypeSymbol.ToFullName()}>()");
            foreach (var comp in _archetype.Disabled)
                code.AppendLine($@".WithDisabled<{comp.TypeSymbol.ToFullName()}>()");
            foreach (var comp in _archetype.Absent)
                code.AppendLine($@".WithAbsent<{comp.TypeSymbol.ToFullName()}>()");

            // Append all ".WithAspect" calls. They must be done after all "WithAll", "WithAny" and "WithNone" calls to avoid component aliasing
            code.Append(codeAspect);

            if(_archetype.Options != EntityQueryOptions.Default)
                code.Append($".WithOptions({_archetype.Options.GetFlags().Select(flag => $"global::Unity.Entities.EntityQueryOptions.{flag.ToString()}").SeparateByBinaryOr()})");

            code.AppendLine($".Build(ref state);");
            return code.ToString();
        }

        public string EntityQueryFieldAssignment(string generatedQueryFieldName)
        {
            // if there are any aspects in our query, use the EntityQueryBuilder to create the query
            if (_archetype.All.Concat(_changeFilterTypes).Any(x => x.TypeSymbol.IsAspect()))
                return EntityQueryWithEntityQueryBuilder(generatedQueryFieldName);

            var entityQuerySetup =
                $@"{generatedQueryFieldName} = state.GetEntityQuery
                    (
                        new global::Unity.Entities.EntityQueryDesc
                        {{
                            All = {DistinctQueryTypesFor()},
                            Any = new global::Unity.Entities.ComponentType[] {{
                                {_archetype.Any.Select(q => q.ToString()).Distinct().SeparateByCommaAndNewLine()}
                            }},
                            None = new global::Unity.Entities.ComponentType[] {{
                                {_archetype.None.Select(q => q.ToString()).Distinct().SeparateByCommaAndNewLine()}
                            }},
                            Disabled = new global::Unity.Entities.ComponentType[] {{
                                {_archetype.Disabled.Select(q => q.ToString()).Distinct().SeparateByCommaAndNewLine()}
                            }},
                            Absent = new global::Unity.Entities.ComponentType[] {{
                                {_archetype.Absent.Select(q => q.ToString()).Distinct().SeparateByCommaAndNewLine()}
                            }},
                            Options =
                                {_archetype.Options.GetFlags().Select(flag => $"global::Unity.Entities.EntityQueryOptions.{flag.ToString()}").SeparateByBinaryOr()}
                        }}
                    );";

            if (_queryStorageFieldName != null)
                entityQuerySetup = $"{_queryStorageFieldName} = " + entityQuerySetup;

            if (_changeFilterTypes.Any())
            {
                entityQuerySetup +=
                    $@"{generatedQueryFieldName}.SetChangedVersionFilter(new ComponentType[{_changeFilterTypes.Count}]
				    {{
                        {_changeFilterTypes.Select(q => q.ToString()).SeparateByComma()}
                    }});";
            }

            return entityQuerySetup;
        }

        string DistinctQueryTypesFor()
        {
            var readOnlyTypeNames = new HashSet<string>();
            var readWriteTypeNames = new HashSet<string>();

            int componentCount = 0;

            void AddQueryType(ITypeSymbol queryType, bool isReadOnly)
            {
                if (queryType == null)
                    return;

                var queryTypeFullName = queryType.ToFullName();
                ++componentCount;
                if (!isReadOnly)
                {
                    readOnlyTypeNames.Remove(queryTypeFullName);
                    readWriteTypeNames.Add(queryTypeFullName);
                }
                else
                {
                    if (!readWriteTypeNames.Contains(queryTypeFullName) &&
                        !readOnlyTypeNames.Contains(queryTypeFullName))
                    {
                        readOnlyTypeNames.Add(queryTypeFullName);
                    }
                }
            }

            foreach (var allComponentType in _archetype.All)
                AddQueryType(allComponentType.TypeSymbol, allComponentType.IsReadOnly);

            foreach (var changeFilterType in _changeFilterTypes)
                AddQueryType(changeFilterType.TypeSymbol, changeFilterType.IsReadOnly);

            if (componentCount == 0)
                return "new global::Unity.Entities.ComponentType[]{}";

            var eComponents = readOnlyTypeNames
                .Select(type => $@"global::Unity.Entities.ComponentType.ReadOnly<{type}>()")
                .Concat(readWriteTypeNames.Select(type => $@"global::Unity.Entities.ComponentType.ReadWrite<{type}>()"));
            return $"new global::Unity.Entities.ComponentType[] {{{eComponents.Distinct().SeparateByCommaAndNewLine()}}}";
        }
    }
}
