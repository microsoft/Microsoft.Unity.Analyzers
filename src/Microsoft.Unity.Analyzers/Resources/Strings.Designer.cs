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
        ///   Looks up a localized string similar to MonoBehaviours can only be added using AddComponent(). Indeed MonoBehaviour is a component, and needs to be attached to a GameObject..
        /// </summary>
        internal static string CreateMonoBehaviourInstanceDiagnosticDescription {
            get {
                return ResourceManager.GetString("CreateMonoBehaviourInstanceDiagnosticDescription", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to MonoBehavior &apos;{0}&apos; cannot be instantiated directly.
        /// </summary>
        internal static string CreateMonoBehaviourInstanceDiagnosticMessageFormat {
            get {
                return ResourceManager.GetString("CreateMonoBehaviourInstanceDiagnosticMessageFormat", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Direct MonoBehavior instance creation is not allowed.
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
        ///   Looks up a localized string similar to ScriptableObject can only be added using CreateInstance(). Indeed ScriptableObject needs to be created by the Unity engine to handle special message methods..
        /// </summary>
        internal static string CreateScriptableObjectInstanceDiagnosticDescription {
            get {
                return ResourceManager.GetString("CreateScriptableObjectInstanceDiagnosticDescription", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to ScriptableObject &apos;{0}&apos; cannot be instantiated directly.
        /// </summary>
        internal static string CreateScriptableObjectInstanceDiagnosticMessageFormat {
            get {
                return ResourceManager.GetString("CreateScriptableObjectInstanceDiagnosticMessageFormat", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Direct ScriptableObject instance creation is not allowed.
        /// </summary>
        internal static string CreateScriptableObjectInstanceDiagnosticTitle {
            get {
                return ResourceManager.GetString("CreateScriptableObjectInstanceDiagnosticTitle", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Delete empty Unity message.
        /// </summary>
        internal static string EmptyUnityMessageCodeFixTitle {
            get {
                return ResourceManager.GetString("EmptyUnityMessageCodeFixTitle", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Unity messages are called by the runtime even if they are empty, do not declare them to avoid uncesseray processing by the Unity runtime..
        /// </summary>
        internal static string EmptyUnityMessageDiagnosticDescription {
            get {
                return ResourceManager.GetString("EmptyUnityMessageDiagnosticDescription", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Unity message &apos;{0}&apos; is declared but is empty.
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
        ///   Looks up a localized string similar to FixedUpdate message is frame-rate independent, and should use Time.fixedDeltaTime instead of Time.deltaTime..
        /// </summary>
        internal static string FixedUpdateWithoutDeltaTimeDiagnosticDescription {
            get {
                return ResourceManager.GetString("FixedUpdateWithoutDeltaTimeDiagnosticDescription", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Usage of Time.deltaTime is suspicious in FixedUpdate message.
        /// </summary>
        internal static string FixedUpdateWithoutDeltaTimeDiagnosticMessageFormat {
            get {
                return ResourceManager.GetString("FixedUpdateWithoutDeltaTimeDiagnosticMessageFormat", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Suspicious Time.deltaTime usage.
        /// </summary>
        internal static string FixedUpdateWithoutDeltaTimeDiagnosticTitle {
            get {
                return ResourceManager.GetString("FixedUpdateWithoutDeltaTimeDiagnosticTitle", resourceCulture);
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
        ///   Looks up a localized string similar to When applying the InitializeOnLoad attribute to a class, you need to provide a static constructor. InitializeOnLoad attribute ensures that it will be called as the editor launches..
        /// </summary>
        internal static string InitializeOnLoadStaticCtorDiagnosticDescription {
            get {
                return ResourceManager.GetString("InitializeOnLoadStaticCtorDiagnosticDescription", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Class &apos;{0}&apos; tagged with InitializeOnLoad attribute is missing a static constructor.
        /// </summary>
        internal static string InitializeOnLoadStaticCtorDiagnosticMessageFormat {
            get {
                return ResourceManager.GetString("InitializeOnLoadStaticCtorDiagnosticMessageFormat", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Missing static constructor with InitializeOnLoad attribute.
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
        ///   Looks up a localized string similar to An incorrect method signature was detected for this Unity message.
        /// </summary>
        internal static string MessageSignatureDiagnosticDescription {
            get {
                return ResourceManager.GetString("MessageSignatureDiagnosticDescription", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Unity message &apos;{0}&apos; has incorrect signature.
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
        ///   Looks up a localized string similar to Method &apos;{0}&apos; has a preferred generic overload..
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
        ///   Looks up a localized string similar to Use CompareTag method.
        /// </summary>
        internal static string TagComparisonCodeFixTitle {
            get {
                return ResourceManager.GetString("TagComparisonCodeFixTitle", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Tag comparison using string equality is slower than the built-in CompareTag method..
        /// </summary>
        internal static string TagComparisonDiagnosticDescription {
            get {
                return ResourceManager.GetString("TagComparisonDiagnosticDescription", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Tag comparison using == is inefficient.
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
        ///   Looks up a localized string similar to Unity overrides the null comparison operator for Unity objects which is incompatible with null coalescing..
        /// </summary>
        internal static string UnityObjectNullCoalescingDiagnosticDescription {
            get {
                return ResourceManager.GetString("UnityObjectNullCoalescingDiagnosticDescription", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Usage of null coalescing on Unity objects is incorrect.
        /// </summary>
        internal static string UnityObjectNullCoalescingDiagnosticMessageFormat {
            get {
                return ResourceManager.GetString("UnityObjectNullCoalescingDiagnosticMessageFormat", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Do not use null coalescing on Unity objects.
        /// </summary>
        internal static string UnityObjectNullCoalescingDiagnosticTitle {
            get {
                return ResourceManager.GetString("UnityObjectNullCoalescingDiagnosticTitle", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Unity objects should not use null coalescing.
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
        ///   Looks up a localized string similar to Unity overrides the null comparison operator for Unity objects which is incompatible with null propagation..
        /// </summary>
        internal static string UnityObjectNullPropagationDiagnosticDescription {
            get {
                return ResourceManager.GetString("UnityObjectNullPropagationDiagnosticDescription", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Usage of null propagation on Unity objects is incorrect.
        /// </summary>
        internal static string UnityObjectNullPropagationDiagnosticMessageFormat {
            get {
                return ResourceManager.GetString("UnityObjectNullPropagationDiagnosticMessageFormat", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Unity objects should not use null propagation.
        /// </summary>
        internal static string UnityObjectNullPropagationDiagnosticTitle {
            get {
                return ResourceManager.GetString("UnityObjectNullPropagationDiagnosticTitle", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Unity objects should not use null propagation.
        /// </summary>
        internal static string UnityObjectNullPropagationSuppressorJustification {
            get {
                return ResourceManager.GetString("UnityObjectNullPropagationSuppressorJustification", resourceCulture);
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
        ///   Looks up a localized string similar to Update message is frame-rate dependent, and should use Time.deltaTime instead of Time.fixedDeltaTime..
        /// </summary>
        internal static string UpdateWithoutFixedDeltaTimeDiagnosticDescription {
            get {
                return ResourceManager.GetString("UpdateWithoutFixedDeltaTimeDiagnosticDescription", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Usage of Time.fixedDeltaTime is suspicious in Update message.
        /// </summary>
        internal static string UpdateWithoutFixedDeltaTimeDiagnosticMessageFormat {
            get {
                return ResourceManager.GetString("UpdateWithoutFixedDeltaTimeDiagnosticMessageFormat", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Suspicious Time.fixedDeltaTime usage.
        /// </summary>
        internal static string UpdateWithoutFixedDeltaTimeDiagnosticTitle {
            get {
                return ResourceManager.GetString("UpdateWithoutFixedDeltaTimeDiagnosticTitle", resourceCulture);
            }
        }
    }
}
