/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

// Stubs to represent Unity API and messages

using System;
using System.Collections.Generic;
using Microsoft.Unity.Analyzers;

#pragma warning disable 

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
	
	class Bounds
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
		void Reset() { }
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

	public struct Vector2 { }
	public struct Vector3 { }
	public struct Vector4 { }

	class Input
	{
		public static bool GetKey(string name) { return false; }
		public static bool GetKeyUp(string name) { return false; }
		public static bool GetKeyDown(string name) { return false; }
	}

	internal class KeyTextAttribute : Attribute {
		public string? Text { get; set; }

		public KeyTextAttribute(string? text)
		{
			Text = text;
		}
	}

	public enum KeyCode
	{
		[KeyTextAttribute(null)] None = 0,
		[KeyTextAttribute("backspace")] Backspace = 8,
		[KeyTextAttribute("delete")] Delete = 0x7F,
		[KeyTextAttribute("tab")] Tab = 9,
		[KeyTextAttribute("clear")] Clear = 12,
		[KeyTextAttribute("return")] Return = 13,
		[KeyTextAttribute("pause")] Pause = 19,
		[KeyTextAttribute("escape")] Escape = 27,
		[KeyTextAttribute("space")] Space = 0x20,
		[KeyTextAttribute("[0]")] Keypad0 = 0x100,
		[KeyTextAttribute("[1]")] Keypad1 = 257,
		[KeyTextAttribute("[2]")] Keypad2 = 258,
		[KeyTextAttribute("[3]")] Keypad3 = 259,
		[KeyTextAttribute("[4]")] Keypad4 = 260,
		[KeyTextAttribute("[5]")] Keypad5 = 261,
		[KeyTextAttribute("[6]")] Keypad6 = 262,
		[KeyTextAttribute("[7]")] Keypad7 = 263,
		[KeyTextAttribute("[8]")] Keypad8 = 264,
		[KeyTextAttribute("[9]")] Keypad9 = 265,
		[KeyTextAttribute("[.]")] KeypadPeriod = 266,
		[KeyTextAttribute("[/]")] KeypadDivide = 267,
		[KeyTextAttribute("[*]")] KeypadMultiply = 268,
		[KeyTextAttribute("[-]")] KeypadMinus = 269,
		[KeyTextAttribute("[+]")] KeypadPlus = 270,
		[KeyTextAttribute("enter")] KeypadEnter = 271,
		[KeyTextAttribute("equals")] KeypadEquals = 272,
		[KeyTextAttribute("up")] UpArrow = 273,
		[KeyTextAttribute("down")] DownArrow = 274,
		[KeyTextAttribute("right")] RightArrow = 275,
		[KeyTextAttribute("left")] LeftArrow = 276,
		[KeyTextAttribute("insert")] Insert = 277,
		[KeyTextAttribute("home")] Home = 278,
		[KeyTextAttribute("end")] End = 279,
		[KeyTextAttribute("page up")] PageUp = 280,
		[KeyTextAttribute("page down")] PageDown = 281,
		[KeyTextAttribute("f1")] F1 = 282,
		[KeyTextAttribute("f2")] F2 = 283,
		[KeyTextAttribute("f3")] F3 = 284,
		[KeyTextAttribute("f4")] F4 = 285,
		[KeyTextAttribute("f5")] F5 = 286,
		[KeyTextAttribute("f6")] F6 = 287,
		[KeyTextAttribute("f7")] F7 = 288,
		[KeyTextAttribute("f8")] F8 = 289,
		[KeyTextAttribute("f9")] F9 = 290,
		[KeyTextAttribute("f10")] F10 = 291,
		[KeyTextAttribute("f11")] F11 = 292,
		[KeyTextAttribute("f12")] F12 = 293,
		[KeyTextAttribute("f13")] F13 = 294,
		[KeyTextAttribute("f14")] F14 = 295,
		[KeyTextAttribute("f15")] F15 = 296,
		[KeyTextAttribute("0")] Alpha0 = 48,
		[KeyTextAttribute("1")] Alpha1 = 49,
		[KeyTextAttribute("2")] Alpha2 = 50,
		[KeyTextAttribute("3")] Alpha3 = 51,
		[KeyTextAttribute("4")] Alpha4 = 52,
		[KeyTextAttribute("5")] Alpha5 = 53,
		[KeyTextAttribute("6")] Alpha6 = 54,
		[KeyTextAttribute("7")] Alpha7 = 55,
		[KeyTextAttribute("8")] Alpha8 = 56,
		[KeyTextAttribute("9")] Alpha9 = 57,
		[KeyTextAttribute("!")] Exclaim = 33,
		[KeyTextAttribute("\"")] DoubleQuote = 34,
		[KeyTextAttribute("#")] Hash = 35,
		[KeyTextAttribute("$")] Dollar = 36,
		[KeyTextAttribute("%")] Percent = 37,
		[KeyTextAttribute("&")] Ampersand = 38,
		[KeyTextAttribute("'")] Quote = 39,
		[KeyTextAttribute("(")] LeftParen = 40,
		[KeyTextAttribute(")")] RightParen = 41,
		[KeyTextAttribute("*")] Asterisk = 42,
		[KeyTextAttribute("+")] Plus = 43,
		[KeyTextAttribute(",")] Comma = 44,
		[KeyTextAttribute("-")] Minus = 45,
		[KeyTextAttribute(".")] Period = 46,
		[KeyTextAttribute("/")] Slash = 47,
		[KeyTextAttribute(":")] Colon = 58,
		[KeyTextAttribute(";")] Semicolon = 59,
		[KeyTextAttribute("<")] Less = 60,
		[KeyTextAttribute("=")] Equals = 61,
		[KeyTextAttribute(">")] Greater = 62,
		[KeyTextAttribute("?")] Question = 0x3F,
		[KeyTextAttribute("@")] At = 0x40,
		[KeyTextAttribute("[")] LeftBracket = 91,
		[KeyTextAttribute("\\")] Backslash = 92,
		[KeyTextAttribute("]")] RightBracket = 93,
		[KeyTextAttribute("^")] Caret = 94,
		[KeyTextAttribute("_")] Underscore = 95,
		[KeyTextAttribute("`")] BackQuote = 96,
		[KeyTextAttribute("a")] A = 97,
		[KeyTextAttribute("b")] B = 98,
		[KeyTextAttribute("c")] C = 99,
		[KeyTextAttribute("d")] D = 100,
		[KeyTextAttribute("e")] E = 101,
		[KeyTextAttribute("f")] F = 102,
		[KeyTextAttribute("g")] G = 103,
		[KeyTextAttribute("h")] H = 104,
		[KeyTextAttribute("i")] I = 105,
		[KeyTextAttribute("j")] J = 106,
		[KeyTextAttribute("k")] K = 107,
		[KeyTextAttribute("l")] L = 108,
		[KeyTextAttribute("m")] M = 109,
		[KeyTextAttribute("n")] N = 110,
		[KeyTextAttribute("o")] O = 111,
		[KeyTextAttribute("p")] P = 112,
		[KeyTextAttribute("q")] Q = 113,
		[KeyTextAttribute("r")] R = 114,
		[KeyTextAttribute("s")] S = 115,
		[KeyTextAttribute("t")] T = 116,
		[KeyTextAttribute("u")] U = 117,
		[KeyTextAttribute("v")] V = 118,
		[KeyTextAttribute("w")] W = 119,
		[KeyTextAttribute("x")] X = 120,
		[KeyTextAttribute("y")] Y = 121,
		[KeyTextAttribute("z")] Z = 122,
		[KeyTextAttribute("{")] LeftCurlyBracket = 123,
		[KeyTextAttribute("|")] Pipe = 124,
		[KeyTextAttribute("}")] RightCurlyBracket = 125,
		[KeyTextAttribute("~")] Tilde = 126,
		[KeyTextAttribute("numlock")] Numlock = 300,
		[KeyTextAttribute("caps lock")] CapsLock = 301,
		[KeyTextAttribute("scroll lock")] ScrollLock = 302,
		[KeyTextAttribute("right shift")] RightShift = 303,
		[KeyTextAttribute("left shift")] LeftShift = 304,
		[KeyTextAttribute("right ctrl")] RightControl = 305,
		[KeyTextAttribute("left ctrl")] LeftControl = 306,
		[KeyTextAttribute("right alt")] RightAlt = 307,
		[KeyTextAttribute("left alt")] LeftAlt = 308,
		[KeyTextAttribute("left cmd")] LeftCommand = 310,
		//[KeyTextAttribute(null)] LeftApple = 310,
		[KeyTextAttribute(null)] LeftWindows = 311,
		[KeyTextAttribute("right cmd")] RightCommand = 309,
		//[KeyTextAttribute(null)] RightApple = 309,
		[KeyTextAttribute(null)] RightWindows = 312,
		[KeyTextAttribute("alt gr")] AltGr = 313,
		[KeyTextAttribute("help")] Help = 315,
		[KeyTextAttribute("print screen")] Print = 316,
		[KeyTextAttribute("sys req")] SysReq = 317,
		[KeyTextAttribute("break")] Break = 318,
		[KeyTextAttribute("menu")] Menu = 319,
		[KeyTextAttribute("mouse 0")] Mouse0 = 323,
		[KeyTextAttribute("mouse 1")] Mouse1 = 324,
		[KeyTextAttribute("mouse 2")] Mouse2 = 325,
		[KeyTextAttribute("mouse 3")] Mouse3 = 326,
		[KeyTextAttribute("mouse 4")] Mouse4 = 327,
		[KeyTextAttribute("mouse 5")] Mouse5 = 328,
		[KeyTextAttribute("mouse 6")] Mouse6 = 329,
		[KeyTextAttribute("joystick button 0")] JoystickButton0 = 330,
		[KeyTextAttribute("joystick button 1")] JoystickButton1 = 331,
		[KeyTextAttribute("joystick button 2")] JoystickButton2 = 332,
		[KeyTextAttribute("joystick button 3")] JoystickButton3 = 333,
		[KeyTextAttribute("joystick button 4")] JoystickButton4 = 334,
		[KeyTextAttribute("joystick button 5")] JoystickButton5 = 335,
		[KeyTextAttribute("joystick button 6")] JoystickButton6 = 336,
		[KeyTextAttribute("joystick button 7")] JoystickButton7 = 337,
		[KeyTextAttribute("joystick button 8")] JoystickButton8 = 338,
		[KeyTextAttribute("joystick button 9")] JoystickButton9 = 339,
		[KeyTextAttribute("joystick button 10")] JoystickButton10 = 340,
		[KeyTextAttribute("joystick button 11")] JoystickButton11 = 341,
		[KeyTextAttribute("joystick button 12")] JoystickButton12 = 342,
		[KeyTextAttribute("joystick button 13")] JoystickButton13 = 343,
		[KeyTextAttribute("joystick button 14")] JoystickButton14 = 344,
		[KeyTextAttribute("joystick button 15")] JoystickButton15 = 345,
		[KeyTextAttribute("joystick button 16")] JoystickButton16 = 346,
		[KeyTextAttribute("joystick button 17")] JoystickButton17 = 347,
		[KeyTextAttribute("joystick button 18")] JoystickButton18 = 348,
		[KeyTextAttribute("joystick button 19")] JoystickButton19 = 349,
		[KeyTextAttribute("joystick 1 button 0")] Joystick1Button0 = 350,
		[KeyTextAttribute("joystick 1 button 1")] Joystick1Button1 = 351,
		[KeyTextAttribute("joystick 1 button 2")] Joystick1Button2 = 352,
		[KeyTextAttribute("joystick 1 button 3")] Joystick1Button3 = 353,
		[KeyTextAttribute("joystick 1 button 4")] Joystick1Button4 = 354,
		[KeyTextAttribute("joystick 1 button 5")] Joystick1Button5 = 355,
		[KeyTextAttribute("joystick 1 button 6")] Joystick1Button6 = 356,
		[KeyTextAttribute("joystick 1 button 7")] Joystick1Button7 = 357,
		[KeyTextAttribute("joystick 1 button 8")] Joystick1Button8 = 358,
		[KeyTextAttribute("joystick 1 button 9")] Joystick1Button9 = 359,
		[KeyTextAttribute("joystick 1 button 10")] Joystick1Button10 = 360,
		[KeyTextAttribute("joystick 1 button 11")] Joystick1Button11 = 361,
		[KeyTextAttribute("joystick 1 button 12")] Joystick1Button12 = 362,
		[KeyTextAttribute("joystick 1 button 13")] Joystick1Button13 = 363,
		[KeyTextAttribute("joystick 1 button 14")] Joystick1Button14 = 364,
		[KeyTextAttribute("joystick 1 button 15")] Joystick1Button15 = 365,
		[KeyTextAttribute("joystick 1 button 16")] Joystick1Button16 = 366,
		[KeyTextAttribute("joystick 1 button 17")] Joystick1Button17 = 367,
		[KeyTextAttribute("joystick 1 button 18")] Joystick1Button18 = 368,
		[KeyTextAttribute("joystick 1 button 19")] Joystick1Button19 = 369,
		[KeyTextAttribute("joystick 2 button 0")] Joystick2Button0 = 370,
		[KeyTextAttribute("joystick 2 button 1")] Joystick2Button1 = 371,
		[KeyTextAttribute("joystick 2 button 2")] Joystick2Button2 = 372,
		[KeyTextAttribute("joystick 2 button 3")] Joystick2Button3 = 373,
		[KeyTextAttribute("joystick 2 button 4")] Joystick2Button4 = 374,
		[KeyTextAttribute("joystick 2 button 5")] Joystick2Button5 = 375,
		[KeyTextAttribute("joystick 2 button 6")] Joystick2Button6 = 376,
		[KeyTextAttribute("joystick 2 button 7")] Joystick2Button7 = 377,
		[KeyTextAttribute("joystick 2 button 8")] Joystick2Button8 = 378,
		[KeyTextAttribute("joystick 2 button 9")] Joystick2Button9 = 379,
		[KeyTextAttribute("joystick 2 button 10")] Joystick2Button10 = 380,
		[KeyTextAttribute("joystick 2 button 11")] Joystick2Button11 = 381,
		[KeyTextAttribute("joystick 2 button 12")] Joystick2Button12 = 382,
		[KeyTextAttribute("joystick 2 button 13")] Joystick2Button13 = 383,
		[KeyTextAttribute("joystick 2 button 14")] Joystick2Button14 = 384,
		[KeyTextAttribute("joystick 2 button 15")] Joystick2Button15 = 385,
		[KeyTextAttribute("joystick 2 button 16")] Joystick2Button16 = 386,
		[KeyTextAttribute("joystick 2 button 17")] Joystick2Button17 = 387,
		[KeyTextAttribute("joystick 2 button 18")] Joystick2Button18 = 388,
		[KeyTextAttribute("joystick 2 button 19")] Joystick2Button19 = 389,
		[KeyTextAttribute("joystick 3 button 0")] Joystick3Button0 = 390,
		[KeyTextAttribute("joystick 3 button 1")] Joystick3Button1 = 391,
		[KeyTextAttribute("joystick 3 button 2")] Joystick3Button2 = 392,
		[KeyTextAttribute("joystick 3 button 3")] Joystick3Button3 = 393,
		[KeyTextAttribute("joystick 3 button 4")] Joystick3Button4 = 394,
		[KeyTextAttribute("joystick 3 button 5")] Joystick3Button5 = 395,
		[KeyTextAttribute("joystick 3 button 6")] Joystick3Button6 = 396,
		[KeyTextAttribute("joystick 3 button 7")] Joystick3Button7 = 397,
		[KeyTextAttribute("joystick 3 button 8")] Joystick3Button8 = 398,
		[KeyTextAttribute("joystick 3 button 9")] Joystick3Button9 = 399,
		[KeyTextAttribute("joystick 3 button 10")] Joystick3Button10 = 400,
		[KeyTextAttribute("joystick 3 button 11")] Joystick3Button11 = 401,
		[KeyTextAttribute("joystick 3 button 12")] Joystick3Button12 = 402,
		[KeyTextAttribute("joystick 3 button 13")] Joystick3Button13 = 403,
		[KeyTextAttribute("joystick 3 button 14")] Joystick3Button14 = 404,
		[KeyTextAttribute("joystick 3 button 15")] Joystick3Button15 = 405,
		[KeyTextAttribute("joystick 3 button 16")] Joystick3Button16 = 406,
		[KeyTextAttribute("joystick 3 button 17")] Joystick3Button17 = 407,
		[KeyTextAttribute("joystick 3 button 18")] Joystick3Button18 = 408,
		[KeyTextAttribute("joystick 3 button 19")] Joystick3Button19 = 409,
		[KeyTextAttribute("joystick 4 button 0")] Joystick4Button0 = 410,
		[KeyTextAttribute("joystick 4 button 1")] Joystick4Button1 = 411,
		[KeyTextAttribute("joystick 4 button 2")] Joystick4Button2 = 412,
		[KeyTextAttribute("joystick 4 button 3")] Joystick4Button3 = 413,
		[KeyTextAttribute("joystick 4 button 4")] Joystick4Button4 = 414,
		[KeyTextAttribute("joystick 4 button 5")] Joystick4Button5 = 415,
		[KeyTextAttribute("joystick 4 button 6")] Joystick4Button6 = 416,
		[KeyTextAttribute("joystick 4 button 7")] Joystick4Button7 = 417,
		[KeyTextAttribute("joystick 4 button 8")] Joystick4Button8 = 418,
		[KeyTextAttribute("joystick 4 button 9")] Joystick4Button9 = 419,
		[KeyTextAttribute("joystick 4 button 10")] Joystick4Button10 = 420,
		[KeyTextAttribute("joystick 4 button 11")] Joystick4Button11 = 421,
		[KeyTextAttribute("joystick 4 button 12")] Joystick4Button12 = 422,
		[KeyTextAttribute("joystick 4 button 13")] Joystick4Button13 = 423,
		[KeyTextAttribute("joystick 4 button 14")] Joystick4Button14 = 424,
		[KeyTextAttribute("joystick 4 button 15")] Joystick4Button15 = 425,
		[KeyTextAttribute("joystick 4 button 16")] Joystick4Button16 = 426,
		[KeyTextAttribute("joystick 4 button 17")] Joystick4Button17 = 427,
		[KeyTextAttribute("joystick 4 button 18")] Joystick4Button18 = 428,
		[KeyTextAttribute("joystick 4 button 19")] Joystick4Button19 = 429,
		[KeyTextAttribute("joystick 5 button 0")] Joystick5Button0 = 430,
		[KeyTextAttribute("joystick 5 button 1")] Joystick5Button1 = 431,
		[KeyTextAttribute("joystick 5 button 2")] Joystick5Button2 = 432,
		[KeyTextAttribute("joystick 5 button 3")] Joystick5Button3 = 433,
		[KeyTextAttribute("joystick 5 button 4")] Joystick5Button4 = 434,
		[KeyTextAttribute("joystick 5 button 5")] Joystick5Button5 = 435,
		[KeyTextAttribute("joystick 5 button 6")] Joystick5Button6 = 436,
		[KeyTextAttribute("joystick 5 button 7")] Joystick5Button7 = 437,
		[KeyTextAttribute("joystick 5 button 8")] Joystick5Button8 = 438,
		[KeyTextAttribute("joystick 5 button 9")] Joystick5Button9 = 439,
		[KeyTextAttribute("joystick 5 button 10")] Joystick5Button10 = 440,
		[KeyTextAttribute("joystick 5 button 11")] Joystick5Button11 = 441,
		[KeyTextAttribute("joystick 5 button 12")] Joystick5Button12 = 442,
		[KeyTextAttribute("joystick 5 button 13")] Joystick5Button13 = 443,
		[KeyTextAttribute("joystick 5 button 14")] Joystick5Button14 = 444,
		[KeyTextAttribute("joystick 5 button 15")] Joystick5Button15 = 445,
		[KeyTextAttribute("joystick 5 button 16")] Joystick5Button16 = 446,
		[KeyTextAttribute("joystick 5 button 17")] Joystick5Button17 = 447,
		[KeyTextAttribute("joystick 5 button 18")] Joystick5Button18 = 448,
		[KeyTextAttribute("joystick 5 button 19")] Joystick5Button19 = 449,
		[KeyTextAttribute("joystick 6 button 0")] Joystick6Button0 = 450,
		[KeyTextAttribute("joystick 6 button 1")] Joystick6Button1 = 451,
		[KeyTextAttribute("joystick 6 button 2")] Joystick6Button2 = 452,
		[KeyTextAttribute("joystick 6 button 3")] Joystick6Button3 = 453,
		[KeyTextAttribute("joystick 6 button 4")] Joystick6Button4 = 454,
		[KeyTextAttribute("joystick 6 button 5")] Joystick6Button5 = 455,
		[KeyTextAttribute("joystick 6 button 6")] Joystick6Button6 = 456,
		[KeyTextAttribute("joystick 6 button 7")] Joystick6Button7 = 457,
		[KeyTextAttribute("joystick 6 button 8")] Joystick6Button8 = 458,
		[KeyTextAttribute("joystick 6 button 9")] Joystick6Button9 = 459,
		[KeyTextAttribute("joystick 6 button 10")] Joystick6Button10 = 460,
		[KeyTextAttribute("joystick 6 button 11")] Joystick6Button11 = 461,
		[KeyTextAttribute("joystick 6 button 12")] Joystick6Button12 = 462,
		[KeyTextAttribute("joystick 6 button 13")] Joystick6Button13 = 463,
		[KeyTextAttribute("joystick 6 button 14")] Joystick6Button14 = 464,
		[KeyTextAttribute("joystick 6 button 15")] Joystick6Button15 = 465,
		[KeyTextAttribute("joystick 6 button 16")] Joystick6Button16 = 466,
		[KeyTextAttribute("joystick 6 button 17")] Joystick6Button17 = 467,
		[KeyTextAttribute("joystick 6 button 18")] Joystick6Button18 = 468,
		[KeyTextAttribute("joystick 6 button 19")] Joystick6Button19 = 469,
		[KeyTextAttribute("joystick 7 button 0")] Joystick7Button0 = 470,
		[KeyTextAttribute("joystick 7 button 1")] Joystick7Button1 = 471,
		[KeyTextAttribute("joystick 7 button 2")] Joystick7Button2 = 472,
		[KeyTextAttribute("joystick 7 button 3")] Joystick7Button3 = 473,
		[KeyTextAttribute("joystick 7 button 4")] Joystick7Button4 = 474,
		[KeyTextAttribute("joystick 7 button 5")] Joystick7Button5 = 475,
		[KeyTextAttribute("joystick 7 button 6")] Joystick7Button6 = 476,
		[KeyTextAttribute("joystick 7 button 7")] Joystick7Button7 = 477,
		[KeyTextAttribute("joystick 7 button 8")] Joystick7Button8 = 478,
		[KeyTextAttribute("joystick 7 button 9")] Joystick7Button9 = 479,
		[KeyTextAttribute("joystick 7 button 10")] Joystick7Button10 = 480,
		[KeyTextAttribute("joystick 7 button 11")] Joystick7Button11 = 481,
		[KeyTextAttribute("joystick 7 button 12")] Joystick7Button12 = 482,
		[KeyTextAttribute("joystick 7 button 13")] Joystick7Button13 = 483,
		[KeyTextAttribute("joystick 7 button 14")] Joystick7Button14 = 484,
		[KeyTextAttribute("joystick 7 button 15")] Joystick7Button15 = 485,
		[KeyTextAttribute("joystick 7 button 16")] Joystick7Button16 = 486,
		[KeyTextAttribute("joystick 7 button 17")] Joystick7Button17 = 487,
		[KeyTextAttribute("joystick 7 button 18")] Joystick7Button18 = 488,
		[KeyTextAttribute("joystick 7 button 19")] Joystick7Button19 = 489,
		[KeyTextAttribute("joystick 8 button 0")] Joystick8Button0 = 490,
		[KeyTextAttribute("joystick 8 button 1")] Joystick8Button1 = 491,
		[KeyTextAttribute("joystick 8 button 2")] Joystick8Button2 = 492,
		[KeyTextAttribute("joystick 8 button 3")] Joystick8Button3 = 493,
		[KeyTextAttribute("joystick 8 button 4")] Joystick8Button4 = 494,
		[KeyTextAttribute("joystick 8 button 5")] Joystick8Button5 = 495,
		[KeyTextAttribute("joystick 8 button 6")] Joystick8Button6 = 496,
		[KeyTextAttribute("joystick 8 button 7")] Joystick8Button7 = 497,
		[KeyTextAttribute("joystick 8 button 8")] Joystick8Button8 = 498,
		[KeyTextAttribute("joystick 8 button 9")] Joystick8Button9 = 499,
		[KeyTextAttribute("joystick 8 button 10")] Joystick8Button10 = 500,
		[KeyTextAttribute("joystick 8 button 11")] Joystick8Button11 = 501,
		[KeyTextAttribute("joystick 8 button 12")] Joystick8Button12 = 502,
		[KeyTextAttribute("joystick 8 button 13")] Joystick8Button13 = 503,
		[KeyTextAttribute("joystick 8 button 14")] Joystick8Button14 = 504,
		[KeyTextAttribute("joystick 8 button 15")] Joystick8Button15 = 505,
		[KeyTextAttribute("joystick 8 button 16")] Joystick8Button16 = 506,
		[KeyTextAttribute("joystick 8 button 17")] Joystick8Button17 = 507,
		[KeyTextAttribute("joystick 8 button 18")] Joystick8Button18 = 508,
		[KeyTextAttribute("joystick 8 button 19")] Joystick8Button19 = 509
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

	class ScriptedImporter
	{
		static string[] GatherDependenciesFromSourceFile(string assetPath) { return null; }
		void OnValidate() { }
		void Reset() { }
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
		bool HasFrameBounds() { return false; }
		Bounds OnGetFrameBounds() { return default; }
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

#pragma warning enable
