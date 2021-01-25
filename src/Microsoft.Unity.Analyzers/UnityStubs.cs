/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

// Stubs to represent Unity API and messages

using System;
using System.Collections.Generic;
using Microsoft.Unity.Analyzers;

namespace UnityEngine
{
	class Object
	{
	}

	class Collision
	{
	}

	class Collision2D
	{
	}

	class Component
	{
		public string tag { get; }

		public bool CompareTag(string tag) { return false; }

		public Component GetComponent(Type type) { return null; }
		public Component GetComponent<T>() { return null; }
		public Component GetComponentInChildren(Type type) { return null; }
		public Component GetComponentInChildren<T>() { return null; }
		public Component GetComponentInParent(Type type) { return null; }
		public Component GetComponentInParent<T>() { return null; }
		public Component[] GetComponents(Type type) { return null; }
		public Component[] GetComponents<T>() { return null; }
		public Component[] GetComponentsInChildren(Type type, bool includeInactive = false) { return null; }
		public Component[] GetComponentsInChildren<T>(bool includeInactive = false) { return null; }
		public Component[] GetComponentsInParent(Type type, bool includeInactive = false) { return null; }
		public Component[] GetComponentsInParent<T>(bool includeInactive = false) { return null; }
	}

	class ControllerColliderHit
	{
	}

	class NetworkDisconnection
	{
	}

	class NetworkConnectionError
	{
	}

	class NetworkMessageInfo
	{
	}

	class GameObject
	{
	}

	class NetworkPlayer
	{
	}

	class RenderTexture
	{
	}

	class MasterServerEvent
	{
	}

	class BitStream
	{
	}

	class Collider
	{
	}

	class Collider2D
	{
	}

	class Joint2D
	{
	}

	class Shader
	{
	}

	class Material
	{
	}

	class Renderer
	{
	}

	class AnimationClip
	{
	}

	class AudioClip
	{
	}

	class Cubemap
	{
	}

	class Sprite
	{
	}

	class Transform
	{
		void position() { }
		void rotation() { }
		void SetPositionAndRotation() { }
	}
	class ScriptableObject
	{
		void Awake() { }
		void OnDestroy() { }
		void OnDisable() { }
		void OnEnable() { }
		void OnValidate() { }
	}

	class IEnumeratorOrVoid
	{
	}

	class MonoBehaviour
	{
		void Awake() { }
		void FixedUpdate() { }
		void LateUpdate() { }
		IEnumeratorOrVoid OnApplicationPause(bool pause) { return null; }
		IEnumeratorOrVoid OnApplicationQuit() { return null; }
		IEnumeratorOrVoid OnBecameInvisible() { return null; }
		IEnumeratorOrVoid OnBecameVisible() { return null; }
		IEnumeratorOrVoid OnCollisionEnter(Collision collision) { return null; }
		IEnumeratorOrVoid OnCollisionExit(Collision collision) { return null; }
		IEnumeratorOrVoid OnCollisionStay(Collision collision) { return null; }
		void OnDisable() { }
		void OnDrawGizmos() { }
		IEnumeratorOrVoid OnDrawGizmosSelected() { return null; }
		void OnEnable() { }
		IEnumeratorOrVoid OnLevelWasLoaded(int level) { return null; }
		IEnumeratorOrVoid OnMouseDown() { return null; }
		IEnumeratorOrVoid OnMouseDrag() { return null; }
		IEnumeratorOrVoid OnMouseEnter() { return null; }
		IEnumeratorOrVoid OnMouseExit() { return null; }
		IEnumeratorOrVoid OnMouseOver() { return null; }
		IEnumeratorOrVoid OnMouseUp() { return null; }
		IEnumeratorOrVoid OnParticleCollision(GameObject other) { return null; }
		IEnumeratorOrVoid OnPostRender() { return null; }
		IEnumeratorOrVoid OnPreCull() { return null; }
		IEnumeratorOrVoid OnPreRender() { return null; }
		IEnumeratorOrVoid OnRenderImage(RenderTexture source, RenderTexture destination) { return null; }
		IEnumeratorOrVoid OnTriggerEnter(Collider other) { return null; }
		IEnumeratorOrVoid OnTriggerExit(Collider other) { return null; }
		IEnumeratorOrVoid OnTriggerStay(Collider other) { return null; }
		IEnumeratorOrVoid Reset() { return null; }
		IEnumeratorOrVoid Start() { return null; }
		void Update() { }
		IEnumeratorOrVoid OnConnectedToServer() { return null; }
		IEnumeratorOrVoid OnControllerColliderHit(ControllerColliderHit hit) { return null; }
		void OnDisconnectedFromServer(NetworkDisconnection info) { }
		void OnDisconnectedFromMasterServer(NetworkDisconnection info) { }
		void OnFailedToConnect(NetworkConnectionError error) { }
		void OnFailedToConnectToMasterServer(NetworkConnectionError error) { }
		void OnGUI() { }
		IEnumeratorOrVoid OnJointBreak(float breakForce) { return null; }
		void OnMasterServerEvent(MasterServerEvent msEvent) { }
		void OnNetworkInstantiate(NetworkMessageInfo info) { }
		void OnPlayerConnected(NetworkPlayer player) { }
		void OnPlayerDisconnected(NetworkPlayer player) { }
		void OnSerializeNetworkView(BitStream stream, NetworkMessageInfo info) { }
		IEnumeratorOrVoid OnServerInitialized() { return null; }
		IEnumeratorOrVoid OnWillRenderObject() { return null; }
		IEnumeratorOrVoid OnApplicationFocus(bool focus) { return null; }
		void OnRenderObject() { }
		void OnDestroy() { }
		IEnumeratorOrVoid OnMouseUpAsButton() { return null; }
		IEnumeratorOrVoid OnAudioFilterRead(float[] data, int channels) { return null; }
		IEnumeratorOrVoid OnAnimatorIK(int layerIndex) { return null; }
		IEnumeratorOrVoid OnAnimatorMove() { return null; }
		void OnValidate() { }
		IEnumeratorOrVoid OnCollisionEnter2D(Collision2D collision) { return null; }
		IEnumeratorOrVoid OnCollisionExit2D(Collision2D collision) { return null; }
		IEnumeratorOrVoid OnCollisionStay2D(Collision2D collision) { return null; }
		IEnumeratorOrVoid OnTriggerEnter2D(Collider2D collision) { return null; }
		IEnumeratorOrVoid OnTriggerExit2D(Collider2D collision) { return null; }
		IEnumeratorOrVoid OnTriggerStay2D(Collider2D collision) { return null; }
		IEnumeratorOrVoid OnJointBreak2D(Joint2D joint) { return null; }
		IEnumeratorOrVoid OnBeforeTransformParentChanged() { return null; }
		IEnumeratorOrVoid OnTransformParentChanged() { return null; }
		IEnumeratorOrVoid OnTransformChildrenChanged() { return null; }
		IEnumeratorOrVoid OnRectTransformDimensionsChange() { return null; } // Not a typo on our side, the 'd' is missing...
		IEnumeratorOrVoid OnRectTransformRemoved() { return null; }
		IEnumeratorOrVoid OnCanvasGroupChanged() { return null; }
		IEnumeratorOrVoid OnParticleTrigger() { return null; }
		IEnumeratorOrVoid OnParticleSystemStopped() { return null; }
		IEnumeratorOrVoid OnParticleUpdateJobScheduled() { return null; }
	}

	class Animator
	{
	}

	class AnimatorStateInfo
	{
	}

	class StateMachineBehaviour : ScriptableObject
	{
		public virtual void OnStateEnter(Animator animator, AnimatorStateInfo animatorStateInfo, int layerIndex) { }
		public virtual void OnStateExit(Animator animator, AnimatorStateInfo animatorStateInfo, int layerIndex) { }
		public virtual void OnStateIK(Animator animator, AnimatorStateInfo animatorStateInfo, int layerIndex) { }
		public virtual void OnStateMove(Animator animator, AnimatorStateInfo animatorStateInfo, int layerIndex) { }
		public virtual void OnStateUpdate(Animator animator, AnimatorStateInfo animatorStateInfo, int layerIndex) { }
		public virtual void OnStateMachineEnter(Animator animator, int stateMachinePathHash) { }
		public virtual void OnStateMachineExit(Animator animator, int stateMachinePathHash) { }
	}

	class HideInInspector : System.Attribute
	{
	}

	class SerializeField : System.Attribute
	{
	}

	class SerializeReference : System.Attribute
	{
	}

	class ContextMenu : System.Attribute
	{
	}

	class ContextMenuItemAttribute : System.Attribute
	{
	}

	class RuntimeInitializeOnLoadMethodAttribute : System.Attribute
	{
	}

	struct Color
	{
	}

	enum CubemapFace
	{
	}

	class Texture2D
	{
		[SetPixelsMethodUsage] void SetPixels(int x, int y, int blockWidth, int blockHeight, Color[] colors, int miplevel) { }
		[SetPixelsMethodUsage] void SetPixels(int x, int y, int blockWidth, int blockHeight, Color[] colors) { }
		[SetPixelsMethodUsage] void SetPixels(Color[] colors, int miplevel) { }
		[SetPixelsMethodUsage] void SetPixels(Color[] colors) { }
	}

	class Texture3D
	{
		[SetPixelsMethodUsage] void SetPixels(Color[] colors, int miplevel) { }
		[SetPixelsMethodUsage] void SetPixels(Color[] colors) { }
	}

	class Texture2DArray
	{
		[SetPixelsMethodUsage] void SetPixels(Color[] colors, int arrayElement) { }
		[SetPixelsMethodUsage] void SetPixels(Color[] colors, int arrayElement, int miplevel) { }
	}

	class CubemapArray
	{
		[SetPixelsMethodUsage] void SetPixels(Color[] colors, CubemapFace face, int arrayElement, int miplevel) { }
		[SetPixelsMethodUsage] void SetPixels(Color[] colors, CubemapFace face, int arrayElement) { }
	}
}

namespace UnityEngine.EventSystems
{
	abstract class UIBehaviour : MonoBehaviour
	{
		protected virtual void Awake() { }
		protected virtual void OnEnable() { }
		protected virtual void Start() { }
		protected virtual void OnDisable() { }
		protected virtual void OnDestroy() { }
		public virtual bool IsActive() { return false; }
		protected virtual void OnValidate() { }
		protected virtual void Reset() { }
		protected virtual void OnRectTransformDimensionsChange() { }
		protected virtual void OnBeforeTransformParentChanged() { }
		protected virtual void OnTransformParentChanged() { }
		protected virtual void OnDidApplyAnimationProperties() { }
		protected virtual void OnCanvasGroupChanged() { }
		protected virtual void OnCanvasHierarchyChanged() { }
	}
}

namespace UnityEngine.Networking
{
	class NetworkReader
	{
	}

	class NetworkWriter
	{
	}

	class NetworkConnection
	{
	}

	class NetworkBehaviour : MonoBehaviour
	{
		public virtual bool OnCheckObserver(NetworkConnection connection)
		{
			return false;
		}

		public virtual void OnDeserialize(NetworkReader reader, bool initialState) { }
		public virtual void OnNetworkDestroy() { }

		public virtual bool OnRebuildObservers(HashSet<NetworkConnection> observers, bool initialize)
		{
			return false;
		}

		public virtual bool OnSerialize(NetworkWriter writer, bool initialState)
		{
			return false;
		}

		public virtual void OnSetLocalVisibility(bool vis) { }
		public virtual void OnStartAuthority() { }
		public virtual void OnStartClient() { }
		public virtual void OnStartLocalPlayer() { }
		public virtual void OnStartServer() { }
		public virtual void OnStopAuthority() { }
	}
}

namespace UnityEngine.UIElements
{
	class VisualElement
	{
	}
}

namespace UnityEditor.AssetImporters
{
	class MaterialDescription
	{
	}
}

namespace UnityEditor
{
	using UnityEngine;
	using UnityEngine.UIElements;
	using AssetImporters;

	class Editor : ScriptableObject
	{
		public virtual VisualElement CreateInspectorGUI() { return null; }
		protected virtual bool ShouldHideOpenButton() { return false; }
		void OnSceneGUI() { }
	}

	class EditorWindow : ScriptableObject
	{
		void OnFocus() { }
		void OnDestroy() { }
		void OnGUI() { }
		void OnHierarchyChange() { }
		void OnInspectorUpdate() { }
		void OnLostFocus() { }
		void OnProjectChange() { }
		void OnSelectionChange() { }
		void Update() { }
		void CreateGUI() { }
	}

	class ScriptableWizard : EditorWindow
	{
		void OnWizardUpdate() { }
		void OnWizardCreate() { }
		void OnWizardOtherButton() { }
	}

	class InitializeOnLoadAttribute : System.Attribute
	{
	}

	class InitializeOnLoadMethodAttribute : System.Attribute
	{
	}

	class MenuItem : System.Attribute
	{
	}

	class EditorCurveBinding
	{
	}

	class AssetPostprocessor
	{
		Material OnAssignMaterialModel(Material material, Renderer renderer) { return null; }
		void OnPostprocessAnimation(GameObject root, AnimationClip clip) { }
		void OnPostprocessAssetbundleNameChanged(string assetPath, string previousAssetBundleName, string newAssetBundleName) { }
		void OnPostprocessAudio(AudioClip clip) { }
		void OnPostprocessCubemap(Cubemap texture) { }
		void OnPostprocessGameObjectWithAnimatedUserProperties(GameObject gameObject, EditorCurveBinding[] bindings) { }
		void OnPostprocessGameObjectWithUserProperties(GameObject gameObject, string[] propNames, System.Object[] values) { }
		void OnPostprocessMaterial(Material material) { }
		void OnPostprocessMeshHierarchy(GameObject root) { }
		void OnPostprocessModel(GameObject gameObject) { }
		void OnPostprocessSpeedTree(GameObject gameobject) { }
		void OnPostProcessSprites(Texture2D texture, Sprite[] sprites) { }
		void OnPostprocessTexture(Texture2D texture) { }

		void OnPreprocessAnimation() { }
		void OnPreprocessAsset() { }
		void OnPreprocessAudio() { }
		void OnPreprocessMaterialDescription(MaterialDescription description, Material material, AnimationClip[] materialAnimation) { }
		void OnPreprocessModel() { }
		void OnPreprocessSpeedTree() { }
		void OnPreprocessTexture() { }

		// undocumented static methods
		static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths) { }
		static bool OnPreGeneratingCSProjectFiles() { return false; }
		static string OnGeneratedSlnSolution(string path, string content) { return null; }
		static string OnGeneratedCSProject(string path, string content) { return null; }
	}

}
