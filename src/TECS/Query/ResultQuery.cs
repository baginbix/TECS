using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace TECS.Query
{
    public readonly ref struct QueryResult<T,E> where T: allows ref struct
    {
        private readonly T _value;
        private readonly E _error;
        private readonly bool _success;

        public bool IsSuccess => _success;

        public bool IsError => !_success;
        public E Error => _error;

        private QueryResult(T value, E error, bool success)
        {
            _value = value;
            _error = error;
            _success = success;
        }

        public static implicit operator QueryResult<T,E>(T value) => Success(value);
        public static implicit operator QueryResult<T,E>(E error) => Failure(error);

        public static QueryResult<T,E> Success(T value) => new (value, default!, true);
        public static QueryResult<T,E> Failure(E error) => new (default!, error, false);

        public T Unwrap(){
            Debug.Assert(_success, $"Tried to Unwrap() value of type: {typeof(T) } with a error of: {_error}");
            return _value;
        }
    }
}