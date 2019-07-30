using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;

namespace Microsoft.Unity.Analyzers
{
	class ScriptInfo
	{
		private static readonly Type[] Types =
		{
			typeof(UnityEngine.Networking.NetworkBehaviour),
			typeof(UnityEngine.StateMachineBehaviour),
			typeof(UnityEngine.EventSystems.UIBehaviour),
			typeof(UnityEditor.ScriptableWizard),
			typeof(UnityEditor.EditorWindow),
			typeof(UnityEditor.Editor),
			typeof(UnityEngine.ScriptableObject),
			typeof(UnityEngine.MonoBehaviour),
		};

		private readonly ITypeSymbol _symbol;
		private readonly Type _metadata;

		public bool HasMessages => _metadata != null;

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

				if (returnType != null && !TypeMatch(message.ReturnType, returnType))
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

		private static bool TypeMatch(Type type, ITypeSymbol symbol)
		{
			if (type == typeof(UnityEngine.IEnumeratorOrVoid))
				return symbol.SpecialType == SpecialType.System_Void
					|| symbol.SpecialType == SpecialType.System_Collections_IEnumerator;

			switch (symbol.SpecialType)
			{
				case SpecialType.System_Void:
					return type == typeof(void);
				case SpecialType.System_Boolean:
					return type == typeof(bool);
				case SpecialType.System_Int32:
					return type == typeof(int);
				case SpecialType.System_Single:
					return type == typeof(float);
			}

			if (type.IsArray)
			{
				var array = symbol as IArrayTypeSymbol;
				return array != null
					&& TypeMatch(type.GetElementType(), array.ElementType);
			}

			var named = symbol as INamedTypeSymbol;
			if (named == null)
				return false;

			if (type.IsConstructedGenericType)
			{
				var args = type.GetTypeInfo().GenericTypeArguments;
				if (args.Length != named.TypeArguments.Length)
					return false;

				for (var i = 0; i < args.Length; i++)
					if (!TypeMatch(args[i], named.TypeArguments[i]))
						return false;

				return TypeMatch(type.GetGenericTypeDefinition(), named.ConstructedFrom);
			}

			//return named.Name == type.TypeName()
			return named.Name == type.Name
				&& named.ContainingNamespace.ToDisplayString() == type.Namespace;
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

			if (!TypeMatch(method.ReturnType, symbol.ReturnType))
				return false;

			var parameters = method.GetParameters();
			if (parameters.Length != symbol.Parameters.Length)
				return false;

			for (var i = 0; i < parameters.Length; i++)
			{
				if (!TypeMatch(parameters[i].ParameterType, symbol.Parameters[i].Type))
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
					if (TypeMatch(Types[i], baseType))
						return Types[i];
				}
			}

			return null;
		}

		private static IEnumerable<MethodInfo> GetMessages(Type type) => type.GetTypeInfo().DeclaredMethods;
	}
}
