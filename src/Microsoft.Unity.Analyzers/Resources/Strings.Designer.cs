﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Microsoft.Unity.Analyzers.Resources {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "16.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Strings {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Strings() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("Microsoft.Unity.Analyzers.Resources.Strings", typeof(Strings).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Correctness.
        /// </summary>
        internal static string CategoryCorrectness {
            get {
                return ResourceManager.GetString("CategoryCorrectness", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Performance.
        /// </summary>
        internal static string CategoryPerformance {
            get {
                return ResourceManager.GetString("CategoryPerformance", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Type Safety.
        /// </summary>
        internal static string CategoryTypeSafety {
            get {
                return ResourceManager.GetString("CategoryTypeSafety", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Use gameObject.AddComponent().
        /// </summary>
        internal static string CreateMonoBehaviourInstanceCodeFixTitle {
            get {
                return ResourceManager.GetString("CreateMonoBehaviourInstanceCodeFixTitle", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Use AddComponent() to create MonoBehaviours. A MonoBehaviour component needs to be attached to a GameObject..
        /// </summary>
        internal static string CreateMonoBehaviourInstanceDiagnosticDescription {
            get {
                return ResourceManager.GetString("CreateMonoBehaviourInstanceDiagnosticDescription", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to MonoBehaviour &quot;{0}&quot; should not be instantiated directly..
        /// </summary>
        internal static string CreateMonoBehaviourInstanceDiagnosticMessageFormat {
            get {
                return ResourceManager.GetString("CreateMonoBehaviourInstanceDiagnosticMessageFormat", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to MonoBehaviour instance creation.
        /// </summary>
        internal static string CreateMonoBehaviourInstanceDiagnosticTitle {
            get {
                return ResourceManager.GetString("CreateMonoBehaviourInstanceDiagnosticTitle", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Use ScriptableObject.CreateInstance().
        /// </summary>
        internal static string CreateScriptableObjectInstanceCodeFixTitle {
            get {
                return ResourceManager.GetString("CreateScriptableObjectInstanceCodeFixTitle", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Use CreateInstance() to create a ScriptableObject. To handle Unity message methods, the Unity engine needs to create the ScriptableObject..
        /// </summary>
        internal static string CreateScriptableObjectInstanceDiagnosticDescription {
            get {
                return ResourceManager.GetString("CreateScriptableObjectInstanceDiagnosticDescription", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to ScriptableObject &quot;{0}&quot; should not be instantiated directly..
        /// </summary>
        internal static string CreateScriptableObjectInstanceDiagnosticMessageFormat {
            get {
                return ResourceManager.GetString("CreateScriptableObjectInstanceDiagnosticMessageFormat", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to ScriptableObject instance creation.
        /// </summary>
        internal static string CreateScriptableObjectInstanceDiagnosticTitle {
            get {
                return ResourceManager.GetString("CreateScriptableObjectInstanceDiagnosticTitle", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Remove empty Unity message.
        /// </summary>
        internal static string EmptyUnityMessageCodeFixTitle {
            get {
                return ResourceManager.GetString("EmptyUnityMessageCodeFixTitle", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Unity messages are called by the runtime even if they&apos;re empty. Remove them to avoid unnecessary processing..
        /// </summary>
        internal static string EmptyUnityMessageDiagnosticDescription {
            get {
                return ResourceManager.GetString("EmptyUnityMessageDiagnosticDescription", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The Unity message &quot;{0}&quot; is empty..
        /// </summary>
        internal static string EmptyUnityMessageDiagnosticMessageFormat {
            get {
                return ResourceManager.GetString("EmptyUnityMessageDiagnosticMessageFormat", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Empty Unity message.
        /// </summary>
        internal static string EmptyUnityMessageDiagnosticTitle {
            get {
                return ResourceManager.GetString("EmptyUnityMessageDiagnosticTitle", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Use Time.fixedDeltaTime.
        /// </summary>
        internal static string FixedUpdateWithoutDeltaTimeCodeFixTitle {
            get {
                return ResourceManager.GetString("FixedUpdateWithoutDeltaTimeCodeFixTitle", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to FixedUpdate is independent of the frame rate. Use Time.fixedDeltaTime instead of Time.deltaTime..
        /// </summary>
        internal static string FixedUpdateWithoutDeltaTimeDiagnosticDescription {
            get {
                return ResourceManager.GetString("FixedUpdateWithoutDeltaTimeDiagnosticDescription", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to FixedUpdate should use Time.fixedDeltaTime..
        /// </summary>
        internal static string FixedUpdateWithoutDeltaTimeDiagnosticMessageFormat {
            get {
                return ResourceManager.GetString("FixedUpdateWithoutDeltaTimeDiagnosticMessageFormat", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Time.deltaTime used with FixedUpdate.
        /// </summary>
        internal static string FixedUpdateWithoutDeltaTimeDiagnosticTitle {
            get {
                return ResourceManager.GetString("FixedUpdateWithoutDeltaTimeDiagnosticTitle", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to GetComponent only supports arguments that are a Unity Component or an interface type..
        /// </summary>
        internal static string GetComponentIncorrectTypeDiagnosticDescription {
            get {
                return ResourceManager.GetString("GetComponentIncorrectTypeDiagnosticDescription", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to {0} is not a Unity Component.
        /// </summary>
        internal static string GetComponentIncorrectTypeDiagnosticMessageFormat {
            get {
                return ResourceManager.GetString("GetComponentIncorrectTypeDiagnosticMessageFormat", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Invalid type for call to GetComponent.
        /// </summary>
        internal static string GetComponentIncorrectTypeDiagnosticTitle {
            get {
                return ResourceManager.GetString("GetComponentIncorrectTypeDiagnosticTitle", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Remove SerializeField attribute.
        /// </summary>
        internal static string ImproperSerializeFieldCodeFixTitle {
            get {
                return ResourceManager.GetString("ImproperSerializeFieldCodeFixTitle", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to SerializeField attribute does not work on properties, and are unnecessary for public fields.
        /// </summary>
        internal static string ImproperSerializeFieldDiagnosticDescription {
            get {
                return ResourceManager.GetString("ImproperSerializeFieldDiagnosticDescription", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to SerializeField attribute is invalid or redundant for property or field: {0}.
        /// </summary>
        internal static string ImproperSerializeFieldDiagnosticMessageFormat {
            get {
                return ResourceManager.GetString("ImproperSerializeFieldDiagnosticMessageFormat", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Remove invalid SerializeField attribute.
        /// </summary>
        internal static string ImproperSerializeFieldDiagnosticTitle {
            get {
                return ResourceManager.GetString("ImproperSerializeFieldDiagnosticTitle", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Don&apos;t flag private methods decorated with InitializeOnLoadMethodAttribute as unused..
        /// </summary>
        internal static string InitializeOnLoadMethodSuppressorJustification {
            get {
                return ResourceManager.GetString("InitializeOnLoadMethodSuppressorJustification", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Add static constructor.
        /// </summary>
        internal static string InitializeOnLoadStaticCtorCodeFixTitle {
            get {
                return ResourceManager.GetString("InitializeOnLoadStaticCtorCodeFixTitle", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Provide a static constructor when applying the InitializeOnLoad attribute to a class. This will call it when the editor launches..
        /// </summary>
        internal static string InitializeOnLoadStaticCtorDiagnosticDescription {
            get {
                return ResourceManager.GetString("InitializeOnLoadStaticCtorDiagnosticDescription", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The class &quot;{0}&quot; tagged with the InitializeOnLoad attribute is missing a static conductor..
        /// </summary>
        internal static string InitializeOnLoadStaticCtorDiagnosticMessageFormat {
            get {
                return ResourceManager.GetString("InitializeOnLoadStaticCtorDiagnosticMessageFormat", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Missing static constructor with InitializeOnLoad.
        /// </summary>
        internal static string InitializeOnLoadStaticCtorDiagnosticTitle {
            get {
                return ResourceManager.GetString("InitializeOnLoadStaticCtorDiagnosticTitle", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Fix message signature.
        /// </summary>
        internal static string MessageSignatureCodeFixTitle {
            get {
                return ResourceManager.GetString("MessageSignatureCodeFixTitle", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to This Unity message uses an incorrect method signature..
        /// </summary>
        internal static string MessageSignatureDiagnosticDescription {
            get {
                return ResourceManager.GetString("MessageSignatureDiagnosticDescription", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The Unity message &quot;{0}&quot; has an incorrect signature..
        /// </summary>
        internal static string MessageSignatureDiagnosticMessageFormat {
            get {
                return ResourceManager.GetString("MessageSignatureDiagnosticMessageFormat", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Incorrect message signature.
        /// </summary>
        internal static string MessageSignatureDiagnosticTitle {
            get {
                return ResourceManager.GetString("MessageSignatureDiagnosticTitle", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Use nameof operator instead of a string literal.
        /// </summary>
        internal static string MethodInvocationCodeFixTitle {
            get {
                return ResourceManager.GetString("MethodInvocationCodeFixTitle", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to TODO: Add description..
        /// </summary>
        internal static string MethodInvocationDiagnosticDescription {
            get {
                return ResourceManager.GetString("MethodInvocationDiagnosticDescription", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to TODO: Add Message format.
        /// </summary>
        internal static string MethodInvocationDiagnosticMessageFormat {
            get {
                return ResourceManager.GetString("MethodInvocationDiagnosticMessageFormat", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to TODO: Add title.
        /// </summary>
        internal static string MethodInvocationDiagnosticTitle {
            get {
                return ResourceManager.GetString("MethodInvocationDiagnosticTitle", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Don&apos;t flag fields with a SerializeField or SerializeReference attribute as never assigned..
        /// </summary>
        internal static string NeverAssignedSerializeFieldSuppressorJustification {
            get {
                return ResourceManager.GetString("NeverAssignedSerializeFieldSuppressorJustification", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Use the generic form of GetComponent.
        /// </summary>
        internal static string NonGenericGetComponentCodeFixTitle {
            get {
                return ResourceManager.GetString("NonGenericGetComponentCodeFixTitle", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Usage of the generic form of GetComponent is preferred for type safety..
        /// </summary>
        internal static string NonGenericGetComponentDiagnosticDescription {
            get {
                return ResourceManager.GetString("NonGenericGetComponentDiagnosticDescription", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Method &quot;{0}&quot; has a preferred generic overload..
        /// </summary>
        internal static string NonGenericGetComponentDiagnosticMessageFormat {
            get {
                return ResourceManager.GetString("NonGenericGetComponentDiagnosticMessageFormat", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Usage of non generic GetComponent.
        /// </summary>
        internal static string NonGenericGetComponentDiagnosticTitle {
            get {
                return ResourceManager.GetString("NonGenericGetComponentDiagnosticTitle", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Don&apos;t set fields with a ContextMenuItem attribute as readonly..
        /// </summary>
        internal static string ReadonlyContextMenuItemJustification {
            get {
                return ResourceManager.GetString("ReadonlyContextMenuItemJustification", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Don&apos;t set fields with a SerializeField or SerializeReference attributes to read-only..
        /// </summary>
        internal static string ReadonlySerializeFieldSuppressorJustification {
            get {
                return ResourceManager.GetString("ReadonlySerializeFieldSuppressorJustification", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Use CompareTag method.
        /// </summary>
        internal static string TagComparisonCodeFixTitle {
            get {
                return ResourceManager.GetString("TagComparisonCodeFixTitle", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Comparing tags using == is slower than using the built-in CompareTag method..
        /// </summary>
        internal static string TagComparisonDiagnosticDescription {
            get {
                return ResourceManager.GetString("TagComparisonDiagnosticDescription", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Comparing tags using == is inefficient..
        /// </summary>
        internal static string TagComparisonDiagnosticMessageFormat {
            get {
                return ResourceManager.GetString("TagComparisonDiagnosticMessageFormat", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Inefficient tag comparison.
        /// </summary>
        internal static string TagComparisonDiagnosticTitle {
            get {
                return ResourceManager.GetString("TagComparisonDiagnosticTitle", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Use null comparison.
        /// </summary>
        internal static string UnityObjectNullCoalescingCodeFixTitle {
            get {
                return ResourceManager.GetString("UnityObjectNullCoalescingCodeFixTitle", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Unity overrides the null comparison operator for Unity objects, which is incompatible with null coalescing..
        /// </summary>
        internal static string UnityObjectNullCoalescingDiagnosticDescription {
            get {
                return ResourceManager.GetString("UnityObjectNullCoalescingDiagnosticDescription", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Unity objects should not use null coalescing..
        /// </summary>
        internal static string UnityObjectNullCoalescingDiagnosticMessageFormat {
            get {
                return ResourceManager.GetString("UnityObjectNullCoalescingDiagnosticMessageFormat", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Null coalescing on Unity objects.
        /// </summary>
        internal static string UnityObjectNullCoalescingDiagnosticTitle {
            get {
                return ResourceManager.GetString("UnityObjectNullCoalescingDiagnosticTitle", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Unity objects should not use null coalescing..
        /// </summary>
        internal static string UnityObjectNullCoalescingSuppressorJustification {
            get {
                return ResourceManager.GetString("UnityObjectNullCoalescingSuppressorJustification", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Use null comparison.
        /// </summary>
        internal static string UnityObjectNullPropagationCodeFixTitle {
            get {
                return ResourceManager.GetString("UnityObjectNullPropagationCodeFixTitle", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Unity overrides the null comparison operator for Unity objects, which is incompatible with null propagation..
        /// </summary>
        internal static string UnityObjectNullPropagationDiagnosticDescription {
            get {
                return ResourceManager.GetString("UnityObjectNullPropagationDiagnosticDescription", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Unity objects should not use null propagation..
        /// </summary>
        internal static string UnityObjectNullPropagationDiagnosticMessageFormat {
            get {
                return ResourceManager.GetString("UnityObjectNullPropagationDiagnosticMessageFormat", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Null propagation on Unity objects.
        /// </summary>
        internal static string UnityObjectNullPropagationDiagnosticTitle {
            get {
                return ResourceManager.GetString("UnityObjectNullPropagationDiagnosticTitle", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Unity objects should not use null propagation..
        /// </summary>
        internal static string UnityObjectNullPropagationSuppressorJustification {
            get {
                return ResourceManager.GetString("UnityObjectNullPropagationSuppressorJustification", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Don&apos;t flag fields with a ContextMenuItem attribute as unused..
        /// </summary>
        internal static string UnusedContextMenuItemJustification {
            get {
                return ResourceManager.GetString("UnusedContextMenuItemJustification", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Use StartCoroutine().
        /// </summary>
        internal static string UnusedCoroutineReturnValueCodeFixTitle {
            get {
                return ResourceManager.GetString("UnusedCoroutineReturnValueCodeFixTitle", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Use StartCoroutine() to start a coroutine. Calling the coroutine method directly will result in the coroutine never being executed..
        /// </summary>
        internal static string UnusedCoroutineReturnValueDiagnosticDescription {
            get {
                return ResourceManager.GetString("UnusedCoroutineReturnValueDiagnosticDescription", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Coroutine {0} should be called using StartCoroutine()..
        /// </summary>
        internal static string UnusedCoroutineReturnValueDiagnosticMessageFormat {
            get {
                return ResourceManager.GetString("UnusedCoroutineReturnValueDiagnosticMessageFormat", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Unused Unity Coroutine.
        /// </summary>
        internal static string UnusedCoroutineReturnValueDiagnosticTitle {
            get {
                return ResourceManager.GetString("UnusedCoroutineReturnValueDiagnosticTitle", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The Unity runtime invokes Unity messages..
        /// </summary>
        internal static string UnusedMessageSuppressorJustification {
            get {
                return ResourceManager.GetString("UnusedMessageSuppressorJustification", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Don&apos;t flag methods with the ContextMenu attribute or referenced by a field with the ContextMenuItem attribute as unused..
        /// </summary>
        internal static string UnusedMethodContextMenuSuppressorJustification {
            get {
                return ResourceManager.GetString("UnusedMethodContextMenuSuppressorJustification", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Don&apos;t flag private methods used with Invoke/InvokeRepeating or StartCoroutine/StopCoroutine as unused..
        /// </summary>
        internal static string UnusedMethodSuppressorJustification {
            get {
                return ResourceManager.GetString("UnusedMethodSuppressorJustification", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Don&apos;t flag private fields with a SerializeField or SerializeReference attribute as unused..
        /// </summary>
        internal static string UnusedSerializeFieldSuppressorJustification {
            get {
                return ResourceManager.GetString("UnusedSerializeFieldSuppressorJustification", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Use Time.deltaTime.
        /// </summary>
        internal static string UpdateWithoutFixedDeltaTimeCodeFixTitle {
            get {
                return ResourceManager.GetString("UpdateWithoutFixedDeltaTimeCodeFixTitle", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Update is dependent on the frame rate. Use Time.deltaTime instead of Time.fixedDeltaTime..
        /// </summary>
        internal static string UpdateWithoutFixedDeltaTimeDiagnosticDescription {
            get {
                return ResourceManager.GetString("UpdateWithoutFixedDeltaTimeDiagnosticDescription", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Update should use Time.deltaTime..
        /// </summary>
        internal static string UpdateWithoutFixedDeltaTimeDiagnosticMessageFormat {
            get {
                return ResourceManager.GetString("UpdateWithoutFixedDeltaTimeDiagnosticMessageFormat", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Time.fixedDeltaTime used with Update.
        /// </summary>
        internal static string UpdateWithoutFixedDeltaTimeDiagnosticTitle {
            get {
                return ResourceManager.GetString("UpdateWithoutFixedDeltaTimeDiagnosticTitle", resourceCulture);
            }
        }
    }
}
