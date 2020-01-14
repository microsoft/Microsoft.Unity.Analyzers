# Diagnostic Analyzers

ID | Title | Category
---- | --- | --- |
[UNT0001](UNT0001.md) | Empty Unity message | Performance
[UNT0002](UNT0002.md) | Inefficient tag comparison | Performance
[UNT0003](UNT0003.md) | Usage of non generic GetComponent | Type Safety
[UNT0004](UNT0004.md) | Time.fixedDeltaTime used with Update | Correctness
[UNT0005](UNT0005.md) | Time.deltaTime used with FixedUpdate | Correctness
[UNT0006](UNT0006.md) | Incorrect message signature | Type Safety
[UNT0007](UNT0007.md) | Null coalescing on Unity objects | Correctness
[UNT0008](UNT0008.md) | Null propagation on Unity objects | Correctness
[UNT0009](UNT0009.md) | Missing static constructor with InitializeOnLoad | Correctness
[UNT0010](UNT0010.md) | MonoBehaviour instance creation | Type Safety
[UNT0011](UNT0011.md) | ScriptableObject instance creation | Type Safety

# Diagnostic Suppressors

ID | Suppressed ID | Justification
---- | --- | --- |
USP0001 | IDE0029 | Unity objects should not use null coalescing
USP0002 | IDE0031 | Unity objects should not use null propagation
USP0003 | IDE0051 | The Unity runtime invokes Unity messages
USP0004 | IDE0044 | Don't set fields with a SerializeField attribute to read-only
USP0005 | IDE0060 | The Unity runtime invokes Unity messages
USP0006 | IDE0051 | Don't flag private fields with a SerializeField attribute as unused
USP0007 | CS0649 | Don't flag fields with a SerializeField attribute as never assigned
USP0008 | IDE0051 | Don't flag private methods used with Invoke/InvokeRepeating or StartCoroutine/StopCoroutine as unused
