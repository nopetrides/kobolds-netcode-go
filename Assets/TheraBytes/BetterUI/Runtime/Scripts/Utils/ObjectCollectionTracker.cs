using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TheraBytes.BetterUi
{
    public class ObjectCollectionTracker<T> : IEnumerable<T>
    {
        HashSet<T> currentItems = new HashSet<T>();
        List<T> itemIterator;

        bool isListDirty;

        public int Count { get { return currentItems.Count; } }

        /// <summary>
        /// This method returns a list of the items which will not change unless <see cref="GetItems()"/> is invoked again.
        /// This way, the returned elements can be used in foreach iterations while it is possible to remove items from the collection.
        /// </summary>
        /// <returns>a readonly list of the items which will not be changed, even if Add or Remove is called.</returns>
        public IReadOnlyList<T> GetItems()
        {
            if (isListDirty || itemIterator == null || itemIterator.Count != currentItems.Count)
            {
                itemIterator = currentItems.ToList();
            }

            return itemIterator;
        }

        /// <summary>
        /// This method can be used to  iterate over all items that are not null. Null-entries will be removed on the go.
        /// </summary>
        /// <returns>an iterator for the valid items.</returns>
        public IEnumerable<T> CleanUpIterator()
        {
            int removedItemsCount = 0;
            foreach (var item in this)
            {
                if (item == null)
                {
                    // We can remove this here as this will modify the Hashset,
                    // not the list that is used for iteration.
                    Remove(item);
                    removedItemsCount++;
                    continue;
                }

                yield return item;
            }

            if (removedItemsCount > 0)
            {
                Debug.LogWarning($"{removedItemsCount} items were null and have been removed");
            }
        }

        public void Add(T item)
        {
            isListDirty = currentItems.Add(item) || isListDirty;
        }

        public void Remove(T item)
        {
            isListDirty = currentItems.Remove(item) || isListDirty;
        }

        public bool Contains(T item)
        {
            return currentItems.Contains(item);
        }

        public IEnumerator<T> GetEnumerator()
        {
            var items = GetItems();
            return items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}