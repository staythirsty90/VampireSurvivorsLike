using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Scripting;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

[UpdateInGroup(typeof(InitializationSystemGroup), OrderFirst = true)]
public partial class Init1 : ComponentSystemGroup {
    [Preserve]
    public Init1() {
    }

    protected override void OnUpdate() {
        base.OnUpdate();
    }
}

[UpdateInGroup(typeof(InitializationSystemGroup))]
[UpdateAfter(typeof(Init1))]
public partial class Init2 : ComponentSystemGroup {
    [Preserve]
    public Init2() {
    }

    protected override void OnUpdate() {
        base.OnUpdate();
    }
}

[UpdateInGroup(typeof(InitializationSystemGroup))]
[UpdateAfter(typeof(Init2))]
public partial class Init3 : ComponentSystemGroup {
    [Preserve]
    public Init3() {
    }

    protected override void OnUpdate() {
        base.OnUpdate();
    }
}

[UpdateInGroup(typeof(InitializationSystemGroup))]
[UpdateAfter(typeof(Init3))]
public partial class Init4 : ComponentSystemGroup {
    [Preserve]
    public Init4() {
    }

    protected override void OnUpdate() {
        base.OnUpdate();
    }
}

[UpdateInGroup(typeof(InitializationSystemGroup))]
[UpdateAfter(typeof(Init4))]
public partial class Init5 : ComponentSystemGroup {
    [Preserve]
    public Init5() {
    }

    protected override void OnUpdate() {
        base.OnUpdate();
    }
}

[UpdateInGroup(typeof(InitializationSystemGroup))]
[UpdateAfter(typeof(Init5))]
public partial class Init6 : ComponentSystemGroup {
    [Preserve]
    public Init6() {
    }

    protected override void OnUpdate() {
        base.OnUpdate();
    }
}