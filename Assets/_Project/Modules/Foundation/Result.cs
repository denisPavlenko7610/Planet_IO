using System;
using System.Collections.Generic;

namespace UnityTemplates.Foundation
{
	public readonly struct Result : IEquatable<Result>
	{
		private const byte SuccessState = 1;
		private const byte FailureState = 2;

		private readonly byte _state;
		private readonly Error _error;

		private Result(byte state, Error error)
		{
			_state = state;
			_error = error;
		}

		public bool IsSuccess => _state == SuccessState;

		public bool IsFailure => !IsSuccess;

		public Error Error
		{
			get
			{
				if (IsSuccess)
				{
					return Error.None;
				}

				return _state switch
				{
					FailureState => _error,
					_ => Error.Uninitialized
				};
			}
		}

		public static Result Success()
		{
			return new Result(SuccessState, Error.None);
		}

		public static Result Failure(Error error)
		{
			return error.IsNone
				? throw new ArgumentException("A failed result requires an error.", nameof(error))
				: new Result(FailureState, error);
		}

		public bool Equals(Result other)
		{
			return IsSuccess == other.IsSuccess && Error.Equals(other.Error);
		}

		public override bool Equals(object obj)
		{
			return obj is Result other && Equals(other);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				return (IsSuccess.GetHashCode() * 397) ^ Error.GetHashCode();
			}
		}

		public override string ToString()
		{
			return IsSuccess ? "Success" : $"Failure({Error})";
		}

		public static bool operator ==(Result left, Result right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(Result left, Result right)
		{
			return !left.Equals(right);
		}
	}

	public readonly struct Result<T> : IEquatable<Result<T>>
	{
		private const byte SuccessState = 1;
		private const byte FailureState = 2;

		private readonly byte _state;
		private readonly T _value;
		private readonly Error _error;

		private Result(byte state, T value, Error error)
		{
			_state = state;
			_value = value;
			_error = error;
		}

		public bool IsSuccess => _state == SuccessState;

		public bool IsFailure => !IsSuccess;

		public Error Error
		{
			get
			{
				if (IsSuccess)
				{
					return Error.None;
				}

				return _state switch
				{
					FailureState => _error,
					_ => Error.Uninitialized
				};
			}
		}

		public T Value
		{
			get
			{
				if (IsFailure)
				{
					throw new InvalidOperationException(
						$"A value cannot be read from a failed result: {Error}",
						Error.Exception
					);
				}

				return _value;
			}
		}

		public static Result<T> Success(T value)
		{
			return new Result<T>(SuccessState, value, Error.None);
		}

		public static Result<T> Failure(Error error)
		{
			return error.IsNone
				? throw new ArgumentException("A failed result requires an error.", nameof(error))
				: new Result<T>(FailureState, default, error);
		}

		public bool TryGetValue(out T value)
		{
			value = IsSuccess ? _value : default;
			return IsSuccess;
		}

		public Result WithoutValue()
		{
			return IsSuccess ? Result.Success() : Result.Failure(Error);
		}

		public bool Equals(Result<T> other)
		{
			return IsSuccess == other.IsSuccess
				&& Error.Equals(other.Error)
				&& (!IsSuccess || EqualityComparer<T>.Default.Equals(_value, other._value));
		}

		public override bool Equals(object obj)
		{
			return obj is Result<T> other && Equals(other);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				int hashCode = IsSuccess.GetHashCode();
				hashCode = (hashCode * 397) ^ Error.GetHashCode();
				if (IsSuccess && !ReferenceEquals(_value, null))
				{
					hashCode = (hashCode * 397) ^ EqualityComparer<T>.Default.GetHashCode(_value);
				}

				return hashCode;
			}
		}

		public override string ToString()
		{
			return IsSuccess ? $"Success({_value})" : $"Failure({Error})";
		}

		public static bool operator ==(Result<T> left, Result<T> right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(Result<T> left, Result<T> right)
		{
			return !left.Equals(right);
		}
	}
}
