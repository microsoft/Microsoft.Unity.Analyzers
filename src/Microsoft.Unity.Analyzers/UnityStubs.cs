﻿/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

// Stubs to represent Unity API and messages

using System;
using System.Collections.Generic;
using Microsoft.Unity.Analyzers;
using Microsoft.Unity.Analyzers.Resources;

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
		void OnApplicationPause(bool pause) { }
		void OnApplicationQuit() { }
		IEnumeratorOrVoid OnBecameInvisible() { return null; }
		IEnumeratorOrVoid OnBecameVisible() { return null; }
		IEnumeratorOrVoid OnCollisionEnter(Collision collision) { return null; }
		IEnumeratorOrVoid OnCollisionExit(Collision collision) { return null; }
		IEnumeratorOrVoid OnCollisionStay(Collision collision) { return null; }
		void OnDisable() { }
		void OnDrawGizmos() { }
		void OnDrawGizmosSelected() { }
		void OnEnable() { }
		void OnLevelWasLoaded(int level) { }
		IEnumeratorOrVoid OnMouseDown() { return null; }
		IEnumeratorOrVoid OnMouseDrag() { return null; }
		IEnumeratorOrVoid OnMouseEnter() { return null; }
		IEnumeratorOrVoid OnMouseExit() { return null; }
		IEnumeratorOrVoid OnMouseOver() { return null; }
		IEnumeratorOrVoid OnMouseUp() { return null; }
		IEnumeratorOrVoid OnParticleCollision(GameObject other) { return null; }
		void OnPostRender() { }
		void OnPreCull() { }
		void OnPreRender() { }
		void OnRenderImage(RenderTexture source, RenderTexture destination) { }
		IEnumeratorOrVoid OnTriggerEnter(Collider other) { return null; }
		IEnumeratorOrVoid OnTriggerExit(Collider other) { return null; }
		IEnumeratorOrVoid OnTriggerStay(Collider other) { return null; }
		void Reset() { }
		IEnumeratorOrVoid Start() { return null; }
		void Update() { }
		void OnConnectedToServer() { }
		void OnControllerColliderHit(ControllerColliderHit hit) { }
		void OnDisconnectedFromServer(NetworkDisconnection info) { }
		void OnDisconnectedFromMasterServer(NetworkDisconnection info) { }
		void OnFailedToConnect(NetworkConnectionError error) { }
		void OnFailedToConnectToMasterServer(NetworkConnectionError error) { }
		void OnGUI() { }
		void OnJointBreak(float breakForce) { }
		void OnMasterServerEvent(MasterServerEvent msEvent) { }
		void OnNetworkInstantiate(NetworkMessageInfo info) { }
		void OnPlayerConnected(NetworkPlayer player) { }
		void OnPlayerDisconnected(NetworkPlayer player) { }
		void OnSerializeNetworkView(BitStream stream, NetworkMessageInfo info) { }
		void OnServerInitialized() { }
		void OnWillRenderObject() { }
		IEnumeratorOrVoid OnApplicationFocus(bool focus) { return null; }
		void OnRenderObject() { }
		void OnDestroy() { }
		IEnumeratorOrVoid OnMouseUpAsButton() { return null; }
		void OnAudioFilterRead(float[] data, int channels) { }
		void OnAnimatorIK(int layerIndex) { }
		void OnAnimatorMove() { }
		void OnValidate() { }
		IEnumeratorOrVoid OnCollisionEnter2D(Collision2D collision) { return null; }
		IEnumeratorOrVoid OnCollisionExit2D(Collision2D collision) { return null; }
		IEnumeratorOrVoid OnCollisionStay2D(Collision2D collision) { return null; }
		IEnumeratorOrVoid OnTriggerEnter2D(Collider2D collision) { return null; }
		IEnumeratorOrVoid OnTriggerExit2D(Collider2D collision) { return null; }
		IEnumeratorOrVoid OnTriggerStay2D(Collider2D collision) { return null; }
		IEnumeratorOrVoid OnJointBreak2D(Joint2D joint) { return null; }
		void OnBeforeTransformParentChanged() { }
		void OnTransformParentChanged() { }
		void OnTransformChildrenChanged() { }
		void OnRectTransformDimensionsChange() { } // Not a typo on our side, the 'd' is missing...
		void OnRectTransformRemoved() { }
		void OnCanvasGroupChanged() { }
		IEnumeratorOrVoid OnParticleTrigger() { return null; }
		void OnParticleSystemStopped() { }
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

namespace UnityEditor
{
	using UnityEngine;
	using UnityEngine.UIElements;

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
}
