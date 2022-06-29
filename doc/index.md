# Diagnostic Analyzers

ID | Title | Category
---- | --- | --- |
[UNT0001](UNT0001.md) | Empty Unity message | Performance
[UNT0002](UNT0002.md) | Inefficient tag comparison | Performance
[UNT0003](UNT0003.md) | Usage of non generic GetComponent | Type Safety
[UNT0004](UNT0004.md) | Time.fixedDeltaTime used with Update | Correctness
[UNT0005](UNT0005.md) | Time.deltaTime used with FixedUpdate `[retired]` | Correctness
[UNT0006](UNT0006.md) | Incorrect message signature | Type Safety
[UNT0007](UNT0007.md) | Null coalescing on Unity objects | Correctness
[UNT0008](UNT0008.md) | Null propagation on Unity objects | Correctness
[UNT0009](UNT0009.md) | Missing static constructor with InitializeOnLoad | Correctness
[UNT0010](UNT0010.md) | Component instance creation | Type Safety
[UNT0011](UNT0011.md) | ScriptableObject instance creation | Type Safety
[UNT0012](UNT0012.md) | Unused coroutine return value | Correctness
[UNT0013](UNT0013.md) | Invalid or redundant SerializeField attribute | Correctness
[UNT0014](UNT0014.md) | GetComponent called with non-Component or non-Interface type | Type Safety
[UNT0015](UNT0015.md) | Incorrect method signature with InitializeOnLoadMethod, RuntimeInitializeOnLoadMethod or DidReloadScripts attribute | Type Safety
[UNT0016](UNT0016.md) | Unsafe way to get the method name | Type Safety
[UNT0017](UNT0017.md) | SetPixels invocation is slow | Performance
[UNT0018](UNT0018.md) | System.Reflection features in performance critical messages | Performance
[UNT0019](UNT0019.md) | Unnecessary indirection call for GameObject.gameObject | Performance
[UNT0020](UNT0020.md) | MenuItem attribute used on non-static method | Correctness
[UNT0021](UNT0021.md) | Unity message should be protected `[opt-in]` | Correctness
[UNT0022](UNT0022.md) | Inefficient method to set position and rotation | Performance
[UNT0023](UNT0023.md) | Coalescing assignment on Unity objects | Correctness
[UNT0024](UNT0024.md) | Give priority to scalar calculations over vector calculations | Performance
[UNT0025](UNT0025.md) | Input.GetKey overloads with KeyCode argument | Correctness
[UNT0026](UNT0026.md) | GetComponent always allocates | Performance
[UNT0027](UNT0027.md) | Do not call PropertyDrawer.OnGUI() | Correctness
[UNT0028](UNT0028.md) | Use non-allocating physics APIs | Performance
[UNT0029](UNT0029.md) | Pattern matching with null on Unity objects | Correctness

# Diagnostic Suppressors

ID | Suppressed ID | Justification
---- | --- | --- |
[USP0001](USP0001.md) | IDE0029 | Unity objects should not use null coalescing
[USP0002](USP0002.md) | IDE0031 | Unity objects should not use null propagation
[USP0003](USP0003.md) | IDE0051 | The Unity runtime invokes Unity messages
[USP0004](USP0004.md) | IDE0044 | Don't set fields with SerializeField or SerializeReference attributes to read-only
[USP0005](USP0005.md) | IDE0060 | The Unity runtime invokes Unity messages
[USP0006](USP0006.md) | IDE0051 | Don't flag private fields with SerializeField or SerializeReference attributes as unused
[USP0007](USP0007.md) | CS0649 | Don't flag fields with SerializeField or SerializeReference attributes as never assigned
[USP0008](USP0008.md) | IDE0051 | Don't flag private methods used with Invoke/InvokeRepeating or StartCoroutine/StopCoroutine as unused
[USP0009](USP0009.md) | IDE0051 | Don't flag methods with MenuItem/ContextMenu attribute or referenced by a field with the ContextMenuItem attribute as unused
[USP0010](USP0010.md) | IDE0051 | Don't flag fields with the ContextMenuItem attribute as unused
[USP0011](USP0011.md) | IDE0044 | Don't make fields with the ContextMenuItem attribute read-only
[USP0012](USP0012.md) | IDE0051 | Don't flag private methods with InitializeOnLoadMethod, RuntimeInitializeOnLoadMethod or DidReloadScripts attribute as unused
[USP0013](USP0013.md) | CA1823 | Don't flag private fields with SerializeField or SerializeReference attributes as unused
[USP0014](USP0014.md) | CA1822 | The Unity runtime invokes Unity messages
[USP0015](USP0015.md) | CA1801 | The Unity runtime invokes Unity messages
[USP0016](USP0016.md) | CS8618 | Initialization detection with nullable reference types
[USP0017](USP0017.md) | IDE0074 | Unity objects should not use coalescing assignment
[USP0018](USP0018.md) | IDE0016 | Unity objects should not be used with throw expressions
[USP0019](USP0012.md) | IDE0051 | Don't flag private methods with PreserveAttribute or UsedImplicitlyAttribute as unused
