using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;

namespace Microsoft.Unity.Analyzers
{
	class ScriptInfo
	{
		private static readonly Type[] Types = { typeof(UnityEngine.Networking.NetworkBehaviour), typeof(UnityEngine.StateMachineBehaviour), typeof(UnityEngine.EventSystems.UIBehaviour), typeof(UnityEditor.ScriptableWizard), typeof(UnityEditor.EditorWindow), typeof(UnityEditor.Editor), typeof(UnityEngine.ScriptableObject), typeof(UnityEngine.MonoBehaviour), };

		private readonly ITypeSymbol _symbol;
		private readonly Type _metadata;

		public bool HasMessages => _metadata != null;
		public Type Metadata => _metadata;

		public ScriptInfo(ITypeSymbol symbol)
		{
			_symbol = symbol;
			_metadata = GetMatchingMetadata(symbol);
		}

		public static MethodInfo[] Messages { get; } = Types.SelectMany(GetMessages).ToArray();

		public IEnumerable<MethodInfo> GetMessages()
		{
			if (_metadata == null)
				yield break;

			for (var type = _metadata; type != null && type != typeof(object); type = type.GetTypeInfo().BaseType)
			{
				foreach (var message in GetMessages(type))
					yield return message;
			}
		}

		public IEnumerable<MethodInfo> GetNotImplementedMessages(Accessibility? accessibility = null, ITypeSymbol returnType = null)
		{
			foreach (var message in GetMessages())
			{
				if (IsImplemented(message))
					continue;

				if (accessibility.HasValue && !AccessibilityMatch(message, accessibility.Value))
					continue;

				if (returnType != null && !returnType.Matches(message.ReturnType))
					continue;

				yield return message;
			}
		}

		private static bool AccessibilityMatch(MethodInfo message, Accessibility accessibility)
		{
			// If the message is declared as public or protected we need to honor it, other messages can be anything
			if (message.IsPublic && message.IsVirtual)
				return accessibility == Accessibility.Public;

			if (message.IsFamily && message.IsVirtual)
				return accessibility == Accessibility.Protected;

			return true;
		}

		private bool IsImplemented(MethodInfo method)
		{
			foreach (var member in _symbol.GetMembers())
			{
				var methodSymbol = member as IMethodSymbol;
				if (methodSymbol == null)
					continue;

				if (MethodMatch(method, methodSymbol))
					return true;
			}

			return false;
		}

		private static bool MethodMatch(MethodInfo method, IMethodSymbol symbol)
		{
			if (method.Name != symbol.Name)
				return false;

			if (!symbol.ReturnType.Matches(method.ReturnType))
				return false;

			var parameters = method.GetParameters();
			if (parameters.Length != symbol.Parameters.Length)
				return false;

			for (var i = 0; i < parameters.Length; i++)
			{
				if (!symbol.Parameters[i].Type.Matches(parameters[i].ParameterType))
					return false;
			}

			return true;
		}

		public bool IsMessage(IMethodSymbol method)
		{
			foreach (var message in GetMessages())
			{
				if (MethodMatch(message, method))
					return true;
			}

			return false;
		}

		private static Type GetMatchingMetadata(ITypeSymbol symbol)
		{
			for (; symbol != null; symbol = symbol.BaseType)
			{
				if (symbol.BaseType == null)
					return null;

				var baseType = symbol.BaseType;

				for (var i = 0; i < Types.Length; i++)
				{
					if (baseType.Matches(Types[i]))
						return Types[i];
				}
			}

			return null;
		}

		private static IEnumerable<MethodInfo> GetMessages(Type type) => type.GetTypeInfo().DeclaredMethods;
	}
}
