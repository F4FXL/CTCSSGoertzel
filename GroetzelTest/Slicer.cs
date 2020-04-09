using System;
using System.Collections.Generic;

namespace GroetzelTest
{
    public sealed class Slicer<T>
    {
        private readonly int _sliceSize;
        private readonly List<T> _remainingItemsFromPreviousCalls;

        public Slicer(int sliceSize)
        {
            if (sliceSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(sliceSize), "Must be greater 0");

            _sliceSize = sliceSize;
            _remainingItemsFromPreviousCalls = new List<T>(sliceSize * 2);
        }

        public IEnumerable<T[]> GetSlices(T[] items)
        {
            if (items == null)
                yield break;

            _remainingItemsFromPreviousCalls.AddRange(items);
            if (_remainingItemsFromPreviousCalls.Count < _sliceSize)
                yield break;

            bool hadIncompleteSlice = false;
            for (int i = 0; i < _remainingItemsFromPreviousCalls.Count; i += _sliceSize)
            {
                var slice = new T[Math.Min(_remainingItemsFromPreviousCalls.Count - i, _sliceSize)];
                _remainingItemsFromPreviousCalls.CopyTo(i, slice, 0, slice.Length);

                if (slice.Length < _sliceSize)
                {
                    _remainingItemsFromPreviousCalls.Clear();
                    _remainingItemsFromPreviousCalls.AddRange(slice);
                    hadIncompleteSlice = true;
                    break;
                }

                yield return slice;
            }

            if (!hadIncompleteSlice)
            {
                _remainingItemsFromPreviousCalls.Clear();
            }
        }

        public T[] GetRemainingItems()
        {
            var remainingitems = _remainingItemsFromPreviousCalls.ToArray();
            _remainingItemsFromPreviousCalls.Clear();
            return remainingitems;
        }
    }
}