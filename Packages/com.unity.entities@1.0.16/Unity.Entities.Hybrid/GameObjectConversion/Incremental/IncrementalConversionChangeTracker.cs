using System;
using System.Collections.Generic;
using System.Text;
using Unity.Collections;
using UnityEditor;
using UnityEngine;

namespace Unity.Entities
{
    /// <summary>
    /// Represents a fine-grained description of changes that happened since the last conversion.
    /// </summary>
    internal struct IncrementalConversionBatch : IDisposable
    {
        /// <summary>
        /// Instance IDs of all GameObjects that were deleted.
        /// Note that this can overlap with any of the other collections.
        /// </summary>
        public NativeArray<int> DeletedInstanceIds;

        /// <summary>
        /// Instance IDs of all GameObjects that were changed.
        /// /// Note that this might include IDs of destroyed GameObjects.
        /// </summary>
        public NativeArray<int> ChangedInstanceIds;

        /// <summary>
        /// Instance IDs of all GameObjects that should have the entire hierarchy below them reconverted.
        /// Note that this might include IDs of destroyed GameObjects.
        /// </summary>
        public NativeArray<int> ReconvertHierarchyInstanceIds;

        /// <summary>
        /// Maps instance IDs of GameObjects to the instance ID of their last recorded parent if the parenting changed.
        /// Note that this might included instance IDs of destroyed GameObjects on either side.
        /// </summary>
        public NativeParallelHashMap<int, int> ParentChangeInstanceIds;

        /// <summary>
        /// Contains the instance IDs of all assets that were changed since the last conversion.
        /// </summary>
        public NativeArray<int> ChangedAssets;

        /// <summary>
        /// Contains the GUIDs of all assets that were deleted since the last conversion.
        /// </summary>
        public NativeArray<int> DeletedAssets;

        /// <summary>
        /// Contains a list of all components that were changed since the last conversion. Note that the components
        /// might have been destroyed in the mean time.
        /// </summary>
        public List<Component> ChangedComponents;

        public void Dispose()
        {
            DeletedInstanceIds.Dispose();
            ChangedInstanceIds.Dispose();
            ReconvertHierarchyInstanceIds.Dispose();
            ParentChangeInstanceIds.Dispose();
            ChangedAssets.Dispose();
            DeletedAssets.Dispose();
        }
#if UNITY_EDITOR
        internal string FormatSummary()
        {
            var sb = new StringBuilder();
            FormatSummary(sb);
            return sb.ToString();
        }

        internal void FormatSummary(StringBuilder sb)
        {
            sb.AppendLine(nameof(IncrementalConversionBatch));
            PrintOut(sb, nameof(DeletedInstanceIds), DeletedInstanceIds);
            PrintOut(sb, nameof(ChangedInstanceIds), ChangedInstanceIds);
            PrintOut(sb, nameof(ReconvertHierarchyInstanceIds), ReconvertHierarchyInstanceIds);
            PrintOut(sb, nameof(ChangedAssets), ChangedAssets);
            PrintOut(sb, nameof(DeletedAssets), DeletedAssets);

            if (ChangedComponents.Count > 0)
            {
                sb.Append(nameof(ChangedComponents));
                sb.Append(": ");
                sb.Append(ChangedComponents.Count);
                sb.AppendLine();
                foreach (var c in ChangedComponents)
                {
                    sb.Append('\t');
                    sb.Append(c.ToString());
                    sb.AppendLine();
                }
                sb.AppendLine();
            }

            if (!ParentChangeInstanceIds.IsEmpty)
            {
                sb.Append(nameof(ParentChangeInstanceIds));
                sb.Append(": ");
                sb.Append(ParentChangeInstanceIds.Count());
                sb.AppendLine();
                foreach (var kvp in ParentChangeInstanceIds)
                {
                    sb.Append('\t');
                    sb.Append(kvp.Key);
                    sb.Append(" (");
                    {
                        var obj = EditorUtility.InstanceIDToObject(kvp.Key);
                        if (obj == null)
                            sb.Append("null");
                        else
                            sb.Append(obj.name);
                    }
                    sb.Append(") reparented to ");
                    sb.Append(kvp.Value);
                    sb.Append(" (");
                    {
                        var obj = EditorUtility.InstanceIDToObject(kvp.Value);
                        if (obj == null)
                            sb.Append("null");
                        else
                            sb.Append(obj.name);
                    }
                    sb.AppendLine(")");
                }
            }
        }

        static void PrintOut(StringBuilder sb, string name, NativeArray<int> instanceIds)
        {
            if (instanceIds.Length == 0)
                return;
            sb.Append(name);
            sb.Append(": ");
            sb.Append(instanceIds.Length);
            sb.AppendLine();
            for (int i = 0; i < instanceIds.Length; i++)
            {
                sb.Append('\t');
                sb.Append(instanceIds[i]);
                sb.Append(" - ");
                var obj = EditorUtility.InstanceIDToObject(instanceIds[i]);
                if (obj == null)
                    sb.AppendLine("(null)");
                else
                    sb.AppendLine(obj.name);
            }

            sb.AppendLine();
        }

        internal void EnsureFullyInitialized()
        {
            if (!DeletedInstanceIds.IsCreated)
                DeletedInstanceIds = new NativeArray<int>(0, Allocator.TempJob);
            if (!ChangedInstanceIds.IsCreated)
                ChangedInstanceIds = new NativeArray<int>(0, Allocator.TempJob);
            if (!ReconvertHierarchyInstanceIds.IsCreated)
                ReconvertHierarchyInstanceIds = new NativeArray<int>(0, Allocator.TempJob);
            if (!ParentChangeInstanceIds.IsCreated)
                ParentChangeInstanceIds = new NativeParallelHashMap<int, int>(0, Allocator.TempJob);
            if (!ChangedAssets.IsCreated)
                ChangedAssets = new NativeArray<int>(0, Allocator.TempJob);
            if (!DeletedAssets.IsCreated)
                DeletedAssets = new NativeArray<int>(0, Allocator.TempJob);
            if (ChangedComponents == null)
                ChangedComponents = new List<Component>();
        }
#endif
    }
}
