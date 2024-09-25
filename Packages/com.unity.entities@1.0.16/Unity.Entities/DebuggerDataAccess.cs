using System;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Entities
{
    /// <summary>
    /// Access to entity data used by the managed debugger via [DebuggerTypeProxy] for Entity and friends.
    /// </summary>
    /// <remarks>
    /// We approach safety here completely different than normally, one goal is that you can with the debugger
    /// see the current state right now from any job, thread or main thread. Essentially if you have a watch
    /// window for a specific variable you can see the state no matter where in code execution we are or what
    /// other jobs are running right now. Normally the safety system does it's best to prevent any of such
    /// access to prevent race conditions, but in this codepath we don't care about race conditions we just
    /// want to know what the state is right now. Thus we completely circumvent the safety system to get to
    /// the data, instead we do very basic sanity checks that the pointers and data we are looking at are still
    /// valid pointers. (Since we don't use the safety system, we can't safely make the assumption that all the
    /// pointers here haven't in fact already been deallocated).
    /// </remarks>
    unsafe struct DebuggerDataAccess
    {
        EntityComponentStore* ComponentStore;
        ManagedComponentStore ManagedStore;

        internal DebuggerDataAccess(World world)
        {
            if (world !=null && world.IsCreated)
            {
                ComponentStore = world.EntityManager.GetUncheckedEntityDataAccess()->EntityComponentStore;
                ManagedStore = world.EntityManager.GetUncheckedEntityDataAccess()->ManagedComponentStore;
            }
            else
            {
                ComponentStore = null;
                ManagedStore = null;
            }
        }
        internal DebuggerDataAccess(EntityComponentStore* store)
        {
            ComponentStore = null;
            ManagedStore = null;
            if (store == null)
                return;
            var instance = ManagedEntityDataAccess.Debugger_GetInstance(store->m_DebugOnlyManagedAccess);
            if (instance.ManagedComponentStore == null)
                return;
            if (instance.World.EntityManager.Debugger_GetEntityDataAccess() != store)
                return;

            ComponentStore = store;
            ManagedStore = instance.ManagedComponentStore;
        }

        public bool IsCreated => ComponentStore != null && ManagedStore != null;

        public string GetDebugNameWithWorld (Entity entity)
        {
            if (!entity.Equals(default) && IsCreated)
                return $"'{GetName(entity)}' Entity({entity.Index}:{entity.Version}) {GetWorld()}";
            return entity.ToString();
        }

        public string GetDebugNameWithoutWorld (Entity entity)
        {
            if (!entity.Equals(default) && IsCreated)
                return $"'{GetName(entity)}' Entity({entity.Index}:{entity.Version})";
            return entity.ToString();
        }

        string GetName(Entity entity)
        {
#if !DOTS_DISABLE_DEBUG_NAMES
            return EntityComponentStore.Debugger_GetName(ComponentStore, entity);
#else
            return "";
#endif
        }

        object GetComponentObject(Entity entity, ComponentType componentType)
        {
            int* ptr = (int*)EntityComponentStore.Debugger_GetComponentDataWithTypeRO(ComponentStore, entity, componentType.TypeIndex);
            if (ptr == null)
                return null;
            return ManagedStore.Debugger_GetManagedComponent(*ptr);
        }

        public object GetSharedComponentDataBoxed(int shareComponentIndex, TypeIndex typeIndex)
        {
            if (Entities.EntityComponentStore.IsUnmanagedSharedComponentIndex(shareComponentIndex))
                return ComponentStore->GetSharedComponentDataObject_Unmanaged(shareComponentIndex, typeIndex);
            else
                return ManagedStore.GetSharedComponentDataBoxed(shareComponentIndex, typeIndex);
        }


        internal bool Exists(Entity entity)
        {
            return EntityComponentStore.Debugger_Exists(ComponentStore, entity);
        }


        static bool SanityCheckArchetype(Archetype* archetype)
        {
            // From the debugger we can't safely assume that any pointers are still valid, so just check that some of values are reasonable.
            return archetype != null && archetype->TypesCount >= 1 && archetype->TypesCount < 4096 && archetype->Offsets[0] == 0;
        }

        internal object[] GetComponents(Entity entity)
        {
            if (!EntityComponentStore.Debugger_Exists(ComponentStore, entity))
                return null;

            var archetype = ComponentStore->GetArchetype(entity);
            if (!SanityCheckArchetype(archetype))
                return null;

            // NOTE: First component is the entity itself
            var objects = new object[archetype->TypesCount-1];
            for (int i = 1; i < archetype->TypesCount; i++)
            {
                var type = ComponentType.FromTypeIndex(archetype->Types[i].TypeIndex);
                var obj = GetComponentBoxedUnchecked(entity, type);

                if (obj != null && TypeManager.IsEnableable(type.TypeIndex))
                    obj = new Component_E(obj, ComponentStore->IsComponentEnabled(entity, type.TypeIndex));

                objects[i-1] = obj;
            }

            return objects;
        }

        internal World GetWorld()
        {
            if (ComponentStore == null)
                return null;

            var instance = ManagedEntityDataAccess.GetInstance(ComponentStore->m_DebugOnlyManagedAccess);
            return instance.World;
        }

        internal EntityArchetype GetArchetype(Entity entity)
        {
            if (!EntityComponentStore.Debugger_Exists(ComponentStore, entity))
                return default;

            var archetype = ComponentStore->GetArchetype(entity);
            if (!SanityCheckArchetype(archetype))
                return default;
            return new EntityArchetype {Archetype = archetype};
        }

        internal ArchetypeChunk GetChunk(Entity entity)
        {
            if (!EntityComponentStore.Debugger_Exists(ComponentStore, entity))
                return default;

            var chunk = ComponentStore->GetChunk(entity);
            return new ArchetypeChunk(chunk, ComponentStore);
        }

        internal object GetComponentBoxedUnchecked(Entity entity, ComponentType type)
        {
            var typeIndex = type.TypeIndex;
            ref readonly var typeInfo = ref TypeManager.GetTypeInfo(typeIndex);
           // object obj = null;
            if (typeInfo.Category == TypeManager.TypeCategory.ComponentData)
            {
                if (TypeManager.IsManagedComponent(typeIndex))
                {
                    return GetComponentObject(entity, type);
                }
                else
                {
                    var src = EntityComponentStore.Debugger_GetComponentDataWithTypeRO(ComponentStore, entity, typeIndex);
                    return TypeManager.ConstructComponentFromBuffer(typeIndex, src);
                }
            }
            else if (typeInfo.Category == TypeManager.TypeCategory.ISharedComponentData)
            {
                var sharedComponentIndex = ComponentStore->Debugger_GetSharedComponentDataIndex(entity, typeIndex);
                if (sharedComponentIndex == -1)
                    return null;
                return GetSharedComponentDataBoxed(sharedComponentIndex, typeIndex);
            }
            else if (typeInfo.Category == TypeManager.TypeCategory.UnityEngineObject)
            {
                return GetComponentObject(entity, type);
            }
            else if (typeInfo.Category == TypeManager.TypeCategory.BufferData)
            {
                var src = EntityComponentStore.Debugger_GetComponentDataWithTypeRO(ComponentStore, entity, typeIndex);
                var header = (BufferHeader*) src;
                if (header == null || header->Length < 0)
                    return null;

                int length = header->Length;

    #if !NET_DOTS
                System.Array array = Array.CreateInstance(TypeManager.GetType(typeIndex), length);
    #else
                            // no Array.CreateInstance in Tiny BCL
                            // This unfortunately means that the debugger display for this will be object[], because we can't
                            // create an array of the right type.  But better than nothing, since the values are still viewable.
                            var array = new object[length];
    #endif

                var elementSize = TypeManager.GetTypeInfo(typeIndex).ElementSize;
                byte* basePtr = BufferHeader.GetElementPointer(header);

    #if !UNITY_DOTSRUNTIME
                var dstPtr = UnsafeUtility.PinGCArrayAndGetDataAddress(array, out var handle);
                UnsafeUtility.MemCpy(dstPtr, basePtr, elementSize * length);
                UnsafeUtility.ReleaseGCObject(handle);
    #else
                            // DOTS Runtime doesn't have PinGCArrayAndGetDataAddress, because that's in Unity's Mono impl only
                            for (int i = 0; i < length; i++)
                            {
                                var item = TypeManager.ConstructComponentFromBuffer(type.TypeIndex, basePtr + elementSize * i);
                                #if !NET_DOTS
                                array.SetValue(item, i);
                                #else
                                array[i] = item;
                                #endif
                            }
    #endif
                return array;
            }
            else
            {
                return null;
            }
        }
    }
}
