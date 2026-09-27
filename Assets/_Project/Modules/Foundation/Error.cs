using System;

namespace UnityTemplates.Foundation
{
	public readonly struct Error : IEquatable<Error>
	{
		private const string UninitializedCode = "foundation.uninitialized_result";
		private const string UninitializedMessage = "The result value was not initialized.";

		private readonly string _code;
		private readonly string _message;

		public Error(string code, string message, Exception exception = null)
		{
			if (string.IsNullOrWhiteSpace(code))
			{
				throw new ArgumentException("An error code is required.", nameof(code));
			}

			_code = code;
			_message = message ?? string.Empty;
			Exception = exception;
		}

		public static Error None => default;

		public static Error Uninitialized => new Error(UninitializedCode, UninitializedMessage);

		public string Code => _code ?? string.Empty;

		public string Message => _message ?? string.Empty;

		public Exception Exception { get; }

		public bool IsNone => string.IsNullOrEmpty(_code);

		public static Error FromException(Exception exception, string code = "exception")
		{
			return exception == null
				? throw new ArgumentNullException(nameof(exception))
				: new Error(code, exception.Message, exception);
		}

		public bool Equals(Error other)
		{
			return string.Equals(Code, other.Code, StringComparison.Ordinal)
				&& string.Equals(Message, other.Message, StringComparison.Ordinal)
				&& ReferenceEquals(Exception, other.Exception);
		}

		public override bool Equals(object obj)
		{
			return obj is Error other && Equals(other);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				int hashCode = StringComparer.Ordinal.GetHashCode(Code);
				hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(Message);
				hashCode = (hashCode * 397) ^ (Exception != null ? Exception.GetHashCode() : 0);
				return hashCode;
			}
		}

		public override string ToString()
		{
			return IsNone ? "None" : $"{Code}: {Message}";
		}

		public static bool operator ==(Error left, Error right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(Error left, Error right)
		{
			return !left.Equals(right);
		}
	}
}
