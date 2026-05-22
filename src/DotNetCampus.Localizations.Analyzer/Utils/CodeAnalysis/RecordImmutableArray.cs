using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace DotNetCampus.Localizations.Utils.CodeAnalysis;

public readonly record struct RecordImmutableArray<T> : IReadOnlyList<T>, IList<T>, IEqualityComparer<RecordImmutableArray<T>>, IEqualityComparer
{
    public static RecordImmutableArray<T> Empty { get; } = new(ImmutableArray<T>.Empty);

    public readonly ImmutableArray<T> _array;

    public RecordImmutableArray(ImmutableArray<T> array) => _array = array;

    public RecordImmutableArray(IEnumerable<T> array) => _array = [..array];

    public T this[int index] => _array[index];

    public int Count => _array.Length;

    public bool Contains(T item) => _array.Contains(item);

    public void CopyTo(T[] array, int arrayIndex) => _array.CopyTo(array, arrayIndex);

    public int IndexOf(T item) => _array.IndexOf(item);

    bool ICollection<T>.IsReadOnly => true;

    public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)_array).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    void ICollection<T>.Add(T item) => throw new NotSupportedException();

    void ICollection<T>.Clear() => throw new NotSupportedException();

    void IList<T>.Insert(int index, T item) => throw new NotSupportedException();

    bool ICollection<T>.Remove(T item) => throw new NotSupportedException();

    void IList<T>.RemoveAt(int index) => throw new NotSupportedException();

    T IList<T>.this[int index]
    {
        get => this[index];
        set => throw new NotSupportedException();
    }

    public bool Equals(RecordImmutableArray<T> x, RecordImmutableArray<T> y)
    {
        if (x.Count != y.Count)
        {
            return false;
        }

        if (x.Count is 0 && y.Count is 0)
        {
            return true;
        }

        for (var i = 0; i < x.Count; i++)
        {
            var xItem = x[i];
            var yItem = y[i];
            if (xItem is null && yItem is null)
            {
                continue;
            }
            if (xItem is null || yItem is null)
            {
                return false;
            }
            if (!xItem.Equals(yItem))
            {
                return false;
            }
        }

        return true;
    }

    public int GetHashCode(RecordImmutableArray<T> obj)
    {
        return _array.GetHashCode();
    }

    bool IEqualityComparer.Equals(object x, object y)
    {
        if (x is RecordImmutableArray<T> xArray && y is RecordImmutableArray<T> yArray)
        {
            return Equals(xArray, yArray);
        }

        return false;
    }

    int IEqualityComparer.GetHashCode(object obj)
    {
        if (obj is RecordImmutableArray<T> array)
        {
            return GetHashCode(array);
        }

        return obj?.GetHashCode() ?? 0;
    }
}
