using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace TextRenderingSandbox
{
    public class ListArray<T> :
        ICollection<T>, IList<T>, IReadOnlyCollection<T>,
        IReadOnlyList<T>, IEnumerable<T>
    {
        public delegate void VersionChangedDelegate(int oldVersion, int newVersion);
        public event VersionChangedDelegate Changed;

        private const int DefaultCapacity = 4;

        private ReadOnlyCollection<T> _readonly;
        private T[] _innerArray;
        private int _version;
        private int _count;

        public bool IsReadOnly { get; private set; }
        public bool IsFixedCapacity { get; private set; }

        public T[] InnerArray => _innerArray;
        public int Version => _version;
        public int Count => _count;

        public T this[int index]
        {
            get
            {
                if (index >= _count)
                    throw new IndexOutOfRangeException();
                return _innerArray[index];
            }
            set
            {
                AssertAccessible();
                _innerArray[index] = value;
                UpdateVersion();
            }
        }

        public int Capacity
        {
            get => _innerArray.Length;
            set
            {
                if (IsFixedCapacity)
                    throw new InvalidOperationException(
                        "This collection has a fixed capacity and cannot be resized.");

                AssertAccessible();

                if (value != _innerArray.Length)
                {
                    if (value < _count)
                        throw new ArgumentException(
                            "The new capacity is not enough to contain existing items.", nameof(value));

                    if (value > 0)
                    {
                        var newItems = new T[value];
                        if (_count > 0)
                            Array.Copy(_innerArray, 0, newItems, 0, _count);
                        _innerArray = newItems;
                    }
                    else
                        _innerArray = Array.Empty<T>();

                    UpdateVersion();
                }
            }
        }

        public ListArray()
        {
            _innerArray = Array.Empty<T>();
        }

        public ListArray(int capacity)
        {
            _innerArray = new T[capacity];
        }

        public ListArray(int capacity, bool fixedCapacity) : this(capacity)
        {
            IsFixedCapacity = fixedCapacity;
        }

        public ListArray(T[] items, int startOffset, int count)
        {
            _innerArray = items;
            _count = count;
            Capacity = items.Length;
            IsFixedCapacity = true;

            if (startOffset != 0)
            {
                Array.ConstrainedCopy(items, startOffset, items, 0, count);
                Array.Clear(items, count, Capacity - count);
            }
        }

        public ListArray(T[] items, int count) : this(items, 0, count)
        {
        }

        public ListArray(T[] items) : this(items, 0, items.Length)
        {
        }

        public ListArray(IEnumerable<T> items, bool makeReadOnly)
        {
            if (items is ICollection<T> c)
            {
                int count = c.Count;
                if (count != 0)
                {
                    _innerArray = new T[count];
                    c.CopyTo(_innerArray, 0);
                    _count = count;
                }
                else
                    _innerArray = Array.Empty<T>();
            }
            else
            {
                _count = 0;
                _innerArray = Array.Empty<T>();
                AddRange(items);
            }

            IsReadOnly = makeReadOnly;
        }

        public ListArray(IEnumerable<T> items) : this(items, makeReadOnly: false)
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateVersion()
        {
            int oldVersion = _version;
            unchecked(_version)++;
            Changed?.Invoke(oldVersion, _version);
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AssertAccessible()
        {
            if (IsReadOnly)
                throw new InvalidOperationException("This collection is marked as read-only.");
        }

        public ref T GetReferenceAt(int index)
        {
            if (index >= _count)
                throw new ArgumentOutOfRangeException(nameof(index));
            return ref _innerArray[index];
        }

        public void Add(T item)
        {
            AssertAccessible();

            if (_count == _innerArray.Length)
                EnsureCapacity(_count + 1);

            _innerArray[_count++] = item;
            UpdateVersion();
        }

        public void AddRange(IEnumerable<T> collection)
        {
            InsertRange(_count, collection);
        }

        public void Sort(IComparer<T> comparer)
        {
            Sort(0, _count, comparer);
        }

        public void Sort(int index, int count, IComparer<T> comparer)
        {
            AssertAccessible();

            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index));

            if (count < 0)
                throw new ArgumentOutOfRangeException(
                    nameof(count), "Needs a non-negative number.");

            if (_count - index < count)
                throw new ArgumentException("Invalid offset length.");

            Array.Sort(_innerArray, index, count, comparer);
            UpdateVersion();
        }

        public void Sort(Comparison<T> comparison)
        {
            if (_count > 0)
                Sort(new FunctorComparer(comparison));
        }

        internal sealed class FunctorComparer : IComparer<T>
        {
            private Comparison<T> _comparison;

            public FunctorComparer(Comparison<T> comparison)
            {
                _comparison = comparison;
            }

            public int Compare(T x, T y)
            {
                return _comparison(x, y);
            }
        }

        public void Clear()
        {
            AssertAccessible();

            if (_count > 0)
            {
                Array.Clear(_innerArray, 0, _count);

                _count = 0;
                UpdateVersion();
            }
        }

        public void TrimExcess()
        {
            AssertAccessible();

            int threshold = (int)(_innerArray.Length * 0.9);
            if (_count < threshold)
                Capacity = _count;
        }

        public bool Contains(T item)
        {
            return IndexOf(item) != -1;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            Array.ConstrainedCopy(_innerArray, 0, array, arrayIndex, _count);
        }

        public bool Remove(T item)
        {
            int index = IndexOf(item);
            if (index != -1)
            {
                RemoveAt(index);
                return true;
            }
            return false;
        }

        public void RemoveAt(int index)
        {
            if ((uint)index >= (uint)_count)
                throw new ArgumentOutOfRangeException(nameof(index));
            AssertAccessible();

            _count--;
            if (index < _count)
                Array.Copy(_innerArray, index + 1, _innerArray, index, _count - index);

            _innerArray[_count] = default;
            UpdateVersion();
        }

        public T GetAndRemoveAt(int index)
        {
            if ((uint)index >= (uint)_count)
                throw new ArgumentOutOfRangeException(nameof(index));
            AssertAccessible();

            _count--;
            if (index < _count)
                Array.Copy(_innerArray, index + 1, _innerArray, index, _count - index);

            var item = _innerArray[_count];
            _innerArray[_count] = default;
            UpdateVersion();
            return item;
        }

        public T GetAndRemoveLast()
        {
            if (_count > 0)
                return GetAndRemoveAt(_count - 1);
            return default;
        }

        public void RemoveRange(int index, int count)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index));

            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(index));

            if (_count - index < count)
                throw new ArgumentException(
                    $"{nameof(index)} and {nameof(count)} do not denote a valid range of elements.");

            AssertAccessible();

            if (count > 0)
            {
                _count -= count;
                if (index < _count)
                    Array.Copy(_innerArray, index + count, _innerArray, index, _count - index);

                Array.Clear(_innerArray, _count, count);
                UpdateVersion();
            }
        }

        public int FindIndex(Predicate<T> predicate)
        {
            for (int i = 0; i < _count; i++)
                if (predicate.Invoke(_innerArray[i]))
                    return i;
            return -1;
        }

        public int IndexOf(T item)
        {
            return Array.IndexOf(_innerArray, item, 0, _count);
        }

        public void Insert(int index, T item)
        {
            AssertAccessible();

            if (index > Capacity)
                throw new ArgumentOutOfRangeException(nameof(index));

            if (_count == _innerArray.Length)
                EnsureCapacity(_count + 1);

            if (index < _count)
                Array.Copy(_innerArray, index, _innerArray, index + 1, _count - index);

            _innerArray[index] = item;
            _count++;
            UpdateVersion();
        }

        public void InsertRange(int index, IEnumerable<T> collection)
        {
            if (index > _count)
                throw new ArgumentOutOfRangeException(nameof(index));

            if (collection == null)
                throw new ArgumentNullException(nameof(collection));

            AssertAccessible();

            if (collection is ICollection<T> c)
            {
                int count = c.Count;
                if (count > 0)
                {
                    EnsureCapacity(_count + count);
                    if (index < _count)
                        Array.Copy(_innerArray, index, _innerArray, index + count, count - index);

                    if (c == this)
                    {
                        Array.Copy(_innerArray, 0, _innerArray, index, index);
                        Array.Copy(_innerArray, index + count, _innerArray, index * 2, _count - index);
                    }
                    else
                        c.CopyTo(_innerArray, index);

                    _count += count;
                }
            }
            else
            {
                foreach (var item in collection)
                    Insert(index++, item);
            }
        }

        public void InsertRange(int index, int count, T item)
        {
            if (index > _count)
                throw new ArgumentOutOfRangeException(nameof(index));

            AssertAccessible();

            if (count > 0)
            {
                EnsureCapacity(_count + count);
                if (index < _count)
                    Array.Copy(_innerArray, index, _innerArray, index + count, count - index);

                for (int i = 0; i < count; i++)
                    _innerArray[index + i] = item;

                _count += count;
            }
        }

        public ReadOnlyCollection<T> AsReadOnly()
        {
            // no locks needed, doesn't really matter if we create multiple
            // instances by race conditions

            if (_readonly == null)
                _readonly = new ReadOnlyCollection<T>(this);
            return _readonly;
        }

        private void EnsureCapacity(int min)
        {
            if (_innerArray.Length < min)
            {
                int newCapacity = _innerArray.Length == 0 ? DefaultCapacity : _innerArray.Length * 2;
                if (newCapacity < min)
                    newCapacity = min;

                Capacity = newCapacity;
            }
        }

        public Enumerator GetEnumerator() => new Enumerator(this);
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public struct Enumerator : IEnumerator<T>, IEnumerator
        {
            private ListArray<T> _list;
            private int _index;
            private readonly int _version;

            public T Current { get; private set; }

            object IEnumerator.Current
            {
                get
                {
                    if (_index == 0 || _index == _list._count + 1)
                        throw new InvalidOperationException(
                            "Either MoveNext has not been called or index is beyond item count.");
                    return Current;
                }
            }

            public Enumerator(ListArray<T> list)
            {
                _list = list;
                _index = 0;
                _version = _list._version;
                Current = default;
            }

            public bool MoveNext()
            {
                var localList = _list;
                if (_version == localList._version &&
                    _index < localList._count)
                {
                    Current = localList._innerArray[_index];
                    _index++;
                    return true;
                }
                return MoveNextRare();
            }

            private bool MoveNextRare()
            {
                if (_version != _list._version)
                    throw GetVersionException();

                _index = _list._count + 1;
                Current = default;
                return false;
            }

            void IEnumerator.Reset()
            {
                if (_version != _list._version)
                    throw GetVersionException();

                _index = 0;
                Current = default;
            }

            private InvalidOperationException GetVersionException()
            {
                return new InvalidOperationException(
                    "The underlying list has changed.");
            }

            public void Dispose()
            {
            }
        }
    }
}
