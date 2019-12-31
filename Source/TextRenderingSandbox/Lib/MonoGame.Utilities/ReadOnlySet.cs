using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace MonoGame.Utilities.Collections
{
    [DebuggerDisplay("Count = {Count}")]
    public class ReadOnlySet<T> : IReadOnlySet<T>
    {
        private readonly ISet<T> _set;
        private readonly IReadOnlySet<T> _roSet;
        private readonly IEqualityComparer<T> _comparer;

        /// <summary>
        /// Gets whether this set always contains the same elements.
        /// </summary>
        public bool IsImmutable { get; }

        public bool IsReadOnly => IsImmutable || _set.IsReadOnly;
        public int Count => _set.Count;

        /// <summary>
        /// Constructs a <see cref="ReadOnlySet{T}"/> that uses an <see cref="ISet{T}"/> as it's backing store.
        /// </summary>
        /// <param name="set">The set to wrap.</param>
        public ReadOnlySet(ISet<T> set)
        {
            if (set == null)
                throw new ArgumentNullException(nameof(set));

            while (set is ReadOnlySet<T> roSet)
                set = roSet._set;
            _set = set;
        }

        /// <summary>
        /// Constructs an immutable <see cref="ReadOnlySet{T}"/>
        /// by copying elements from an <see cref="IEnumerable{T}"/>.
        /// <para>
        /// If the <paramref name="enumerable"/> is an immutable <see cref="ReadOnlySet{T}"/> and 
        /// has the same <see cref="IEqualityComparer{T}"/>, it's backing store will be reused.
        /// </para>
        /// </summary>
        /// <param name="enumerable">The enumerable whose elements are copied from.</param>
        /// <param name="comparer">
        /// The comparer to use when comparing values in the set,
        /// or <see langword="null"/> to use <see cref="EqualityComparer{T}.Default"/>.
        /// </param>
        public ReadOnlySet(IEnumerable<T> enumerable, IEqualityComparer<T> comparer)
        {
            if (enumerable == null)
                throw new ArgumentNullException(nameof(enumerable));

            _comparer = comparer ?? EqualityComparer<T>.Default;
            IsImmutable = true;

            while (enumerable is ReadOnlySet<T> roSet && roSet.IsImmutable && roSet._comparer == _comparer)
            {
                enumerable = roSet._set;
                _roSet = roSet;
            }

            if (_roSet == null)
                _set = new HashSet<T>(enumerable, _comparer);
        }

        public bool Contains(T item) => _set != null ? _set.Contains(item) : _roSet.Contains(item);
        public bool IsProperSubsetOf(IEnumerable<T> other) => _set != null ? _set.IsProperSubsetOf(other) : _roSet.IsProperSubsetOf(other);
        public bool IsProperSupersetOf(IEnumerable<T> other) => _set != null ? _set.IsProperSupersetOf(other) : _roSet.IsProperSupersetOf(other);
        public bool IsSubsetOf(IEnumerable<T> other) => _set != null ? _set.IsSubsetOf(other) : _roSet.IsSubsetOf(other);
        public bool IsSupersetOf(IEnumerable<T> other) => _set != null ? _set.IsSupersetOf(other) : _roSet.IsSupersetOf(other);
        public bool Overlaps(IEnumerable<T> other) => _set != null ? _set.Overlaps(other) : _roSet.Overlaps(other);
        public bool SetEquals(IEnumerable<T> other) => _set != null ? _set.SetEquals(other) : _roSet.SetEquals(other);

        public Enumerator GetEnumerator() => _set != null ? new Enumerator(_set) : new Enumerator(_roSet);
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => _set != null ? _set.GetEnumerator() : _roSet.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _set != null ? _set.GetEnumerator() : _roSet.GetEnumerator();

        public struct Enumerator : IEnumerator<T>, IEnumerator, IDisposable
        {
            private HashSet<T>.Enumerator _hashSetEnumerator;
            private IEnumerator<T> _genericEnumerator;
            private IEnumerator _boxedCache;

            internal Enumerator(IEnumerable<T> enumerable)
            {
                if (enumerable is HashSet<T> hashSet)
                {
                    _hashSetEnumerator = hashSet.GetEnumerator();
                    _genericEnumerator = null;
                }
                else
                {
                    _hashSetEnumerator = default;
                    _genericEnumerator = enumerable.GetEnumerator();
                }
                _boxedCache = null;
            }

            public T Current
            {
                get
                {
                    if (_genericEnumerator != null)
                        return _genericEnumerator.Current;
                    return _hashSetEnumerator.Current;
                }
            }

            object IEnumerator.Current
            {
                get
                {
                    if (_genericEnumerator != null)
                        return _genericEnumerator.Current;
                    return GetBoxedCache().Current;
                }
            }

            public bool MoveNext()
            {
                if (_genericEnumerator != null)
                    return _genericEnumerator.MoveNext();
                return _hashSetEnumerator.MoveNext();
            }

            void IEnumerator.Reset()
            {
                if (_genericEnumerator != null)
                    _genericEnumerator.Reset();
                else
                    GetBoxedCache().Reset();
            }

            public void Dispose()
            {
                if (_genericEnumerator != null)
                    _genericEnumerator.Dispose();
                else
                    _hashSetEnumerator.Dispose();
            }

            private IEnumerator GetBoxedCache()
            {
                if (_boxedCache == null)
                    _boxedCache = _hashSetEnumerator as IEnumerator;
                return _boxedCache;
            }
        }
    }
}
