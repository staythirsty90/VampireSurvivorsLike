using System.Reflection;
using Unity.Collections;

public static class InitializerHelper {
    public static void AddToList<T>(ref FieldInfo[] fields, ref NativeList<T> list) where T : unmanaged {
        list = new NativeList<T>(Allocator.Persistent);
        for(int i = 0; i < fields.Length; i++) {
            var field = fields[i];

            if(field.FieldType == typeof(T)) {
                T thing = (T)field.GetValue(null);
                list.Add(thing);
            }
        }
    }

    public static void AddToList<T>(ref MethodInfo[] methods, ref NativeList<T> list) where T : unmanaged {
        list = new NativeList<T>(Allocator.Persistent);
        for(int i = 0; i < methods.Length; i++) {
            var method = methods[i];

            if(method.ReturnType == typeof(T) && method.GetParameters().Length == 0) {
                T thing = (T)method.Invoke(null, null);
                list.Add(thing);
            }
        }
    }
}
