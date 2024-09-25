using Unity.Entities;

public static class ForEachDelegates {
    //[Unity.Entities.CodeGeneratedJobForEach.EntitiesForEachCompatible]
    public delegate void CustomForEachDelegate<T0, T1, T2, T3, T4, T5, T6, T7, T8>
        (T0 t0, ref T1 t1, ref T2 t2, ref T3 t3, ref T4 t4, ref T5 t5,
         ref T6 t6, ref T7 t7, ref T8 t8);

    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4, T5, T6, T7, T8>
        (this TDescription description, CustomForEachDelegate<T0, T1, T2, T3, T4, T5, T6, T7, T8> codeToRun)
        where TDescription : struct, Unity.Entities.CodeGeneratedJobForEach.ISupportForEachWithUniversalDelegate =>
        LambdaForEachDescriptionConstructionMethods.ThrowCodeGenException<TDescription>();

    public delegate void CustomForEachDelegate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>
        (T0 t0, T1 t1, ref T2 t2, ref T3 t3, ref T4 t4, ref T5 t5,
         ref T6 t6, ref T7 t7, ref T8 t8, ref T9 t9);

    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>
        (this TDescription description, CustomForEachDelegate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> codeToRun)
        where TDescription : struct, Unity.Entities.CodeGeneratedJobForEach.ISupportForEachWithUniversalDelegate =>
        LambdaForEachDescriptionConstructionMethods.ThrowCodeGenException<TDescription>();

    public delegate void CustomForEachDelegate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>
        (T0 t0, T1 t1, T2 t2, ref T3 t3, ref T4 t4, ref T5 t5,
         ref T6 t6, ref T7 t7, ref T8 t8, ref T9 t9, ref T10 t10);

    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>
        (this TDescription description, CustomForEachDelegate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> codeToRun)
        where TDescription : struct, Unity.Entities.CodeGeneratedJobForEach.ISupportForEachWithUniversalDelegate =>
        LambdaForEachDescriptionConstructionMethods.ThrowCodeGenException<TDescription>();

    public delegate void CustomForEachDelegate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>
        (T0 t0, T1 t1, T2 t2, ref T3 t3, ref T4 t4, ref T5 t5,
         ref T6 t6, ref T7 t7, ref T8 t8, ref T9 t9, ref T10 t10, ref T11 t11);

    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>
        (this TDescription description, CustomForEachDelegate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> codeToRun)
        where TDescription : struct, Unity.Entities.CodeGeneratedJobForEach.ISupportForEachWithUniversalDelegate =>
        LambdaForEachDescriptionConstructionMethods.ThrowCodeGenException<TDescription>();

    public delegate void CustomForEachDelegate2<T0, T1, T2, T3, T4, T5, T6, T7, T8>
        (ref T0 t0, ref T1 t1, ref T2 t2, ref T3 t3, ref T4 t4, ref T5 t5,
         ref T6 t6, ref T7 t7, in T8 t8);

    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4, T5, T6, T7, T8>
        (this TDescription description, CustomForEachDelegate2<T0, T1, T2, T3, T4, T5, T6, T7, T8> codeToRun)
        where TDescription : struct, Unity.Entities.CodeGeneratedJobForEach.ISupportForEachWithUniversalDelegate =>
        LambdaForEachDescriptionConstructionMethods.ThrowCodeGenException<TDescription>();

    public delegate void CustomForEachDelegateRef<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>
        (T0 t0, T1 t1, T2 t2, ref T3 t3, ref T4 t4, ref T5 t5, ref T6 t6, ref T7 t7, ref T8 t8, ref T9 t9);

    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>
        (this TDescription description, CustomForEachDelegateRef<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> codeToRun)
        where TDescription : struct, Unity.Entities.CodeGeneratedJobForEach.ISupportForEachWithUniversalDelegate =>
        LambdaForEachDescriptionConstructionMethods.ThrowCodeGenException<TDescription>();



    public delegate void CustomForEachDelegate11<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>
        (T0 t0, ref T1 t1, ref T2 t2, ref T3 t3, ref T4 t4, ref T5 t5, ref T6 t6, ref T7 t7, ref T8 t8, ref T9 t9, ref T10 t10);

    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>
        (this TDescription description, CustomForEachDelegate11<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> codeToRun)
        where TDescription : struct, Unity.Entities.CodeGeneratedJobForEach.ISupportForEachWithUniversalDelegate =>
        LambdaForEachDescriptionConstructionMethods.ThrowCodeGenException<TDescription>();


    public delegate void CustomForEachDelegate9<T0, T1, T2, T3, T4, T5, T6, T7, T8>
        (T0 t0, ref T1 t1, ref T2 t2, ref T3 t3, ref T4 t4, ref T5 t5, ref T6 t6, in T7 t7, in T8 t8 );

    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4, T5, T6, T7, T8>
        (this TDescription description, CustomForEachDelegate9<T0, T1, T2, T3, T4, T5, T6, T7, T8> codeToRun)
        where TDescription : struct, Unity.Entities.CodeGeneratedJobForEach.ISupportForEachWithUniversalDelegate =>
        LambdaForEachDescriptionConstructionMethods.ThrowCodeGenException<TDescription>();

    public delegate void CustomForEachDelegate10<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>
        (T0 t0, ref T1 t1, ref T2 t2, ref T3 t3, ref T4 t4, ref T5 t5, ref T6 t6, in T7 t7, in T8 t8, in T9 t9);

    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>
        (this TDescription description, CustomForEachDelegate10<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> codeToRun)
        where TDescription : struct, Unity.Entities.CodeGeneratedJobForEach.ISupportForEachWithUniversalDelegate =>
        LambdaForEachDescriptionConstructionMethods.ThrowCodeGenException<TDescription>();
}