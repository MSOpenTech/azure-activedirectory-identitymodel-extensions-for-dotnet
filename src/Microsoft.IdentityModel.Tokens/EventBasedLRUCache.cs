﻿//------------------------------------------------------------------------------
//
// Copyright (c) Microsoft Corporation.
// All rights reserved.
//
// This code is licensed under the MIT License.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.CryptoProviderCacheOptions
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//------------------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Logging;

namespace Microsoft.IdentityModel.Tokens
{
    /// <summary>
    /// This is an LRU cache implementation that relies on an event queue rather than locking to achieve thread safety.
    /// This approach has been decided on in order to optimize the performance of the get and set operations on the cache.
    /// This cache contains a doubly linked list in order to maintain LRU order, as well as a dictionary (map) to keep track of
    /// keys and expiration times. The linked list (a structure which is not thread-safe) is NEVER modified directly inside
    /// an API call (e.g. get, set, remove); it is only ever modified sequentially by a background thread. On the other hand,
    /// the map is a <see cref="ConcurrentDictionary{TKey, TValue}"/> which may be modified directly inside an API call or
    /// through eventual processing of the event queue. This implementation relies on the principle of 'eventual consistency':
    /// though the map and it's corresponding linked list may be out of sync at any given point in time, they will eventually line up.
    /// </summary>
    /// <typeparam name="TKey">The key type to be used by the cache.</typeparam>
    /// <typeparam name="TValue">The value type to be used by the cache</typeparam>
    internal class EventBasedLRUCache<TKey, TValue> : ILRUCache<TKey,TValue>, IDisposable
    {
        private readonly int _capacity;
        // The percentage of the cache to be removed when _maxCapacityPercentage is reached.
        private double _compactionPercentage = .20;
        private readonly LinkedList<LRUCacheItem<TKey, TValue>> _doubleLinkedList = new LinkedList<LRUCacheItem<TKey, TValue>>();
        private readonly BlockingCollection<Action> _eventQueue = new BlockingCollection<Action>();
        private readonly ConcurrentDictionary<TKey, LRUCacheItem<TKey, TValue>> _map;
        // When the current cache size gets to this percentage of _capacity, _compactionPercentage% of the cache will be removed.
        private double _maxCapacityPercentage = .95;
        private bool _disposed = false;

        internal EventBasedLRUCache(int capacity, IEqualityComparer<TKey> comparer = null)
        {
            _capacity = capacity > 0 ? capacity : throw LogHelper.LogExceptionMessage(new ArgumentOutOfRangeException(nameof(capacity)));
            _map = new ConcurrentDictionary<TKey, LRUCacheItem<TKey, TValue>>(comparer ?? EqualityComparer<TKey>.Default);
            new Task(OnStart).Start();
            _ = RemoveExpiredValuesPeriodically(TimeSpan.FromMinutes(5));
        }

        private void OnStart()
        {
            while (!_disposed)
            {
                if (_eventQueue.TryTake(out var action, 500))
                    action.Invoke();
            }
        }

        public bool Contains(TKey key)
        {
            if (key == null)
                throw LogHelper.LogArgumentNullException(nameof(key));

            return _map.ContainsKey(key);
        }

        /// <summary>
        /// FOR TESTING PURPOSES ONLY.
        /// </summary>
        internal void WaitForProcessing()
        {
            while (_eventQueue.Count != 0)
                continue;

            return;
        }

        internal int RemoveExpiredValues()
        {
            int numItemsRemoved = 0;
            var node = _doubleLinkedList.First;
            while (node != null)
            {
                var nextNode = node.Next;
                if (node.Value.ExpirationTime < DateTime.UtcNow)
                {
                    _doubleLinkedList.Remove(node);
                    _map.TryRemove(node.Value.Key, out _);
                    numItemsRemoved++;
                }

                node = nextNode;
            }

            return numItemsRemoved;
        }

        internal void RemoveLRUs()
        {
            // use the _capacity for the newCacheSize calculation in the case where the cache is experiencing overflow
            int currentCount = _map.Count <= _capacity ? _map.Count : _capacity;
            var newCacheSize = currentCount - (int)(currentCount * _compactionPercentage);
            while (_map.Count > newCacheSize)
            {
                var lru = _doubleLinkedList.Last;
                _map.TryRemove(lru.Value.Key, out _);
                _doubleLinkedList.Remove(lru);
            }
        }

        async Task RemoveExpiredValuesPeriodically(TimeSpan interval)
        {
            while (true)
            {
                _eventQueue.Add(() => RemoveExpiredValues());
                await Task.Delay(interval).ConfigureAwait(false);
            }
        }

        public void SetValue(TKey key, TValue value)
        {
            SetValue(key, value, DateTime.MaxValue);
        }

        public bool SetValue(TKey key, TValue value, DateTime expirationTime)
        {
            if (key == null)
                throw LogHelper.LogArgumentNullException(nameof(key));

            if (value == null)
                throw LogHelper.LogArgumentNullException(nameof(value));

            // item already expired
            if (expirationTime < DateTime.UtcNow)
                return false;

            // just need to update value and move it to the top
            if (_map.TryGetValue(key, out var cacheItem))
            {
                cacheItem.Value = value;
                cacheItem.ExpirationTime = expirationTime;
                _eventQueue.Add(() =>
                {
                    _doubleLinkedList.Remove(cacheItem);
                    _doubleLinkedList.AddFirst(cacheItem);
                });
            }
            else
            {
                // if cache is at _maxCapacityPercentage, trim it by _compactionPercentage
                if ((double)_map.Count / _capacity >= _maxCapacityPercentage)
                {
                    _eventQueue.Add(() =>
                    {
                        RemoveLRUs();
                    });
                }
                // add the new node
                var newCacheItem = new LRUCacheItem<TKey, TValue>(key, value, expirationTime);
                _eventQueue.Add(() =>
                {
                    // Add a remove operation in case two threads are trying to add the same value. Only the second remove will succeed in this case.
                    _doubleLinkedList.Remove(newCacheItem);
                    _doubleLinkedList.AddFirst(newCacheItem);
                });
                _map[key] = newCacheItem;
            }

            return true;
        }

        /// Each time a node gets accessed, it gets moved to the beginning (head) of the list.
        public bool TryGetValue(TKey key, out TValue value)
        {
            if (key == null)
                throw LogHelper.LogArgumentNullException(nameof(key));

            if (!_map.ContainsKey(key))
            {
                value = default;
                return false;
            }

            // make sure node hasn't been removed by a different thread
            if (_map.TryGetValue(key, out var cacheItem))
                _eventQueue.Add(() =>
                {
                    _doubleLinkedList.Remove(cacheItem);
                    _doubleLinkedList.AddFirst(cacheItem);
                });

            value = cacheItem != null ? cacheItem.Value : default;
            return cacheItem != null;
        }

        /// Removes a particular key from the cache.
        public bool TryRemove(TKey key, out TValue value)
        {
            if (key == null)
                throw LogHelper.LogArgumentNullException(nameof(key));

            if (!_map.TryGetValue(key, out var cacheItem))
            {
                value = default;
                return false;
            }

            value = cacheItem.Value;
            _eventQueue.Add(() => _doubleLinkedList.Remove(cacheItem));
            return _map.TryRemove(key, out _);
        }


        /// <summary>
        /// FOR TESTING ONLY.
        /// </summary>
        /// <returns></returns>
        internal LinkedList<LRUCacheItem<TKey, TValue>> LinkedList => _doubleLinkedList;

        /// <summary>
        /// FOR TESTING ONLY.
        /// </summary>
        public long LinkedListCount => _doubleLinkedList.Count;

        /// <summary>
        /// FOR TESTING ONLY.
        /// </summary>
        public long MapCount => _map.Count;

        /// <summary>
        /// FOR TESTING ONLY.
        /// </summary>
        /// <returns></returns>
        internal ICollection<LRUCacheItem<TKey, TValue>> MapValues => _map.Values;

        /// <summary>
        /// FOR TESTING ONLY.
        /// </summary>
        public long EventQueueCount => _eventQueue.Count;

        /// <summary>
        /// Calls <see cref="Dispose(bool)"/> and <see cref="GC.SuppressFinalize"/>
        /// </summary>
        public void Dispose()
        {
            // Dispose of unmanaged resources.
            Dispose(true);
            // Suppress finalization.
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// If <paramref name="disposing"/> is true, this method disposes of <see cref="_eventQueue"/>.
        /// </summary>
        /// <param name="disposing">True if called from the <see cref="Dispose()"/> method, false otherwise.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                _disposed = true;
                if (disposing)
                {
                    _eventQueue.Dispose();
                }
            }
        }
    }
    internal class LRUCacheItem<TKey, TValue>
    {
        internal TKey Key { get; }
        internal TValue Value { get; set; }
        internal DateTime ExpirationTime { get; set; }
        internal LRUCacheItem(TKey key, TValue value)
        {
            Key = key ?? throw LogHelper.LogArgumentNullException(nameof(key));
            Value = value ?? throw LogHelper.LogArgumentNullException(nameof(value));
        }
        internal LRUCacheItem(TKey key, TValue value, DateTime expirationTime)
        {
            Key = key ?? throw LogHelper.LogArgumentNullException(nameof(key));
            Value = value ?? throw LogHelper.LogArgumentNullException(nameof(value));
            ExpirationTime = expirationTime;
        }
        public override bool Equals(object obj)
        {
            LRUCacheItem<TKey, TValue> item = obj as LRUCacheItem<TKey, TValue>;
            if (item == null)
                return false;
            else
                return Key.Equals(item.Key);
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}

