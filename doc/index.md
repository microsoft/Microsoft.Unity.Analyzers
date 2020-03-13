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
[UNT0010](UNT0010.md) | MonoBehaviour instance creation | Type Safety
[UNT0011](UNT0011.md) | ScriptableObject instance creation | Type Safety
[UNT0012](UNT0012.md) | Unused coroutine return value | Correctness
[UNT0013](UNT0013.md) | Invalid or redundant SerializeField attribute | Correctness
[UNT0014](UNT0014.md) | GetComponent called with non-Component or non-Interface type | Type Safety

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
[USP0009](USP0009.md) | IDE0051 | Don't flag methods with the ContextMenu attribute or referenced by a field with the ContextMenuItem attribute as unused.
[USP0010](USP0010.md) | IDE0051 | Don't flag fields with the ContextMenuItem attribute as unused
[USP0011](USP0011.md) | IDE0044 | Don't make fields with the ContextMenuItem attribute read-only
