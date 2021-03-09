/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.Unity.Analyzers.Tests
{
	/// <summary>
	///     Structure that stores information about a <see cref="Diagnostic" /> appearing in a source.
	/// </summary>
	public readonly struct DiagnosticResult
	{
		private static readonly object[] EmptyArguments = new object[0];

		private readonly ImmutableArray<DiagnosticLocation> _spans;
		private readonly bool _suppressMessage;
		private readonly string _message;

		public DiagnosticResult(string id, DiagnosticSeverity severity)
			: this()
		{
			Id = id;
			Severity = severity;
		}

		public DiagnosticResult(DiagnosticDescriptor descriptor)
			: this()
		{
			Id = descriptor.Id;
			Severity = descriptor.DefaultSeverity;
			MessageFormat = descriptor.MessageFormat;
		}

		private DiagnosticResult(
			ImmutableArray<DiagnosticLocation> spans,
			bool suppressMessage,
			string message,
			DiagnosticSeverity severity,
			string id,
			LocalizableString messageFormat,
			object[] messageArguments,
			string suppressedId)
			
		{
			_spans = spans;
			_suppressMessage = suppressMessage;
			_message = message;
			Severity = severity;
			Id = id;
			MessageFormat = messageFormat;
			MessageArguments = messageArguments;
			SuppressedId = suppressedId;
		}

		public ImmutableArray<DiagnosticLocation> Spans => _spans.IsDefault ? ImmutableArray<DiagnosticLocation>.Empty : _spans;

		public DiagnosticSeverity Severity { get; }

		public string Id { get; }
		public string SuppressedId { get; }

		public string Message
		{
			get
			{
				if (_suppressMessage) return null;

				if (_message != null) return _message;

				if (MessageFormat != null) return string.Format(MessageFormat.ToString(), MessageArguments ?? EmptyArguments);

				return null;
			}
		}

		public LocalizableString MessageFormat { get; }

		public object[] MessageArguments { get; }

		public bool HasLocation => !Spans.IsEmpty;

		public static DiagnosticResult CompilerError(string identifier)
			=> new(identifier, DiagnosticSeverity.Error);

		public static DiagnosticResult CompilerWarning(string identifier)
			=> new(identifier, DiagnosticSeverity.Warning);

		public DiagnosticResult WithSeverity(DiagnosticSeverity severity)
		{
			return new(
				_spans,
				_suppressMessage,
				_message,
				severity,
				Id,
				MessageFormat,
				MessageArguments,
				SuppressedId);
		}

		public DiagnosticResult WithArguments(params object[] arguments)
		{
			return new(
				_spans,
				_suppressMessage,
				_message,
				Severity,
				Id,
				MessageFormat,
				arguments,
				SuppressedId);
		}

		public DiagnosticResult WithMessage(string message)
		{
			return new(
				_spans,
				message is null,
				message,
				Severity,
				Id,
				MessageFormat,
				MessageArguments,
				SuppressedId);
		}

		public DiagnosticResult WithMessageFormat(LocalizableString messageFormat)
		{
			return new(
				_spans,
				_suppressMessage,
				_message,
				Severity,
				Id,
				messageFormat,
				MessageArguments,
				SuppressedId);
		}

		public DiagnosticResult WithNoLocation()
		{
			return new(
				ImmutableArray<DiagnosticLocation>.Empty,
				_suppressMessage,
				_message,
				Severity,
				Id,
				MessageFormat,
				MessageArguments,
				SuppressedId);
		}

		public DiagnosticResult WithSuppressedId(string suppressedId)
		{
			return new(
				_spans,
				_suppressMessage,
				_message,
				Severity,
				Id,
				MessageFormat,
				MessageArguments,
				suppressedId);
		}

		public DiagnosticResult WithLocation(int line, int column)
			=> WithLocation(string.Empty, new LinePosition(line - 1, column - 1));

		public DiagnosticResult WithLocation(LinePosition location)
			=> WithLocation(string.Empty, location);

		public DiagnosticResult WithLocation(string path, int line, int column)
			=> WithLocation(path, new LinePosition(line - 1, column - 1));

		public DiagnosticResult WithLocation(string path, LinePosition location)
			=> AppendSpan(new FileLinePositionSpan(path, location, location), DiagnosticLocationOptions.IgnoreLength);

		public DiagnosticResult WithLocation(string path, LinePosition location, DiagnosticLocationOptions options)
			=> AppendSpan(new FileLinePositionSpan(path, location, location), options | DiagnosticLocationOptions.IgnoreLength);

		public DiagnosticResult WithSpan(int startLine, int startColumn, int endLine, int endColumn)
			=> WithSpan(string.Empty, startLine, startColumn, endLine, endColumn);

		public DiagnosticResult WithSpan(string path, int startLine, int startColumn, int endLine, int endColumn)
			=> AppendSpan(new FileLinePositionSpan(path, new LinePosition(startLine - 1, startColumn - 1), new LinePosition(endLine - 1, endColumn - 1)), DiagnosticLocationOptions.None);

		public DiagnosticResult WithSpan(FileLinePositionSpan span)
			=> AppendSpan(span, DiagnosticLocationOptions.None);

		public DiagnosticResult WithSpan(FileLinePositionSpan span, DiagnosticLocationOptions options)
			=> AppendSpan(span, options);

		public DiagnosticResult WithDefaultPath(string path)
		{
			if (Spans.IsEmpty) return this;

			var spans = Spans.ToBuilder();
			for (var i = 0; i < spans.Count; i++)
				if (spans[i].Span.Path == string.Empty)
					spans[i] = new DiagnosticLocation(new FileLinePositionSpan(path, spans[i].Span.Span), spans[i].Options);

			return new(
				spans.MoveToImmutable(),
				_suppressMessage,
				_message,
				Severity,
				Id,
				MessageFormat,
				MessageArguments,
				SuppressedId);
		}

		public DiagnosticResult WithLineOffset(int offset)
		{
			if (Spans.IsEmpty) return this;

			var result = this;
			var spansBuilder = result.Spans.ToBuilder();
			for (var i = 0; i < result.Spans.Length; i++)
			{
				var newStartLinePosition = new LinePosition(result.Spans[i].Span.StartLinePosition.Line + offset, result.Spans[i].Span.StartLinePosition.Character);
				var newEndLinePosition = new LinePosition(result.Spans[i].Span.EndLinePosition.Line + offset, result.Spans[i].Span.EndLinePosition.Character);

				spansBuilder[i] = new DiagnosticLocation(new FileLinePositionSpan(result.Spans[i].Span.Path, newStartLinePosition, newEndLinePosition), result.Spans[i].Options);
			}

			return new(
				spansBuilder.MoveToImmutable(),
				_suppressMessage,
				_message,
				Severity,
				Id,
				MessageFormat,
				MessageArguments,
				SuppressedId);
		}

		private DiagnosticResult AppendSpan(FileLinePositionSpan span, DiagnosticLocationOptions options)
		{
			return new(
				Spans.Add(new DiagnosticLocation(span, options)),
				_suppressMessage,
				_message,
				Severity,
				Id,
				MessageFormat,
				MessageArguments,
				SuppressedId);
		}

		public override string ToString()
		{
			var builder = new StringBuilder();
			if (HasLocation)
			{
				var location = Spans[0];
				builder.Append(location.Span.Path == string.Empty ? "?" : location.Span.Path);
				builder.Append("(");
				builder.Append(location.Span.StartLinePosition.Line + 1);
				builder.Append(",");
				builder.Append(location.Span.StartLinePosition.Character + 1);
				if (!location.Options.HasFlag(DiagnosticLocationOptions.IgnoreLength))
				{
					builder.Append(",");
					builder.Append(location.Span.EndLinePosition.Line + 1);
					builder.Append(",");
					builder.Append(location.Span.EndLinePosition.Character + 1);
				}

				builder.Append("): ");
			}

			builder.Append(Severity.ToString().ToLowerInvariant());
			builder.Append(" ");
			builder.Append(Id);

			try
			{
				var message = Message;
				if (message != null) builder.Append(": ").Append(message);
			}
			catch (FormatException)
			{
				// A message format is provided without arguments, so we print the unformatted string
				Debug.Assert(MessageFormat != null, $"Assertion failed: {nameof(MessageFormat)} != null");
				builder.Append(": ").Append(MessageFormat);
			}

			return builder.ToString();
		}
	}
}
