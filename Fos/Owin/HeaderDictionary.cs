using System;
using System.Linq;
using System.Collections.Generic;

namespace Fos
{
    internal class HeaderDictionary : IDictionary<string, string[]>
    {
        private Dictionary<string, string[]> Headers;

        #region IDictionary implementation

        public void Add(string key, string[] value)
        {
            Headers.Add(key, value);
        }

        public bool ContainsKey(string key)
        {
            return Headers.ContainsKey(key);
        }

        public bool Remove(string key)
        {
            return Headers.Remove(key);
        }

        private string[] CreateArrayCopy(string[] original)
        {
            string[] copy = new string[original.Length];
            Array.Copy(original, copy, original.Length);
            return copy;
        }

        public bool TryGetValue(string key, out string[] value)
        {
            string[] original;
            if (Headers.TryGetValue(key, out original))
            {
                // Return a copy of the array (owin spec)
                value = CreateArrayCopy(original);

                return true;
            }
            else
            {
                value = null;
                return false;
            }
        }

        public string[] this[string key]
        {
            get
            {
                // Return a copy of the array (owin spec)
                return CreateArrayCopy(Headers[key]);
            }
            set
            {
                Headers[key] = value;
            }
        }

        public ICollection<string> Keys
        {
            get
            {
                return Headers.Keys;
            }
        }

        public ICollection<string[]> Values
        {
            get
            {
                var listOfValues = Headers.Values.ToList();
                for (int i = 0; i < listOfValues.Count; ++i)
                {
                    // Make a copy of the array (owin spec)
                    string[] original = listOfValues[i];
                    listOfValues[i] = new string[1] { original[0] };
                }

                return listOfValues;
            }
        }

        #endregion

        #region ICollection implementation

        public void Add(KeyValuePair<string, string[]> item)
        {
            Headers.Add(item.Key, item.Value);
        }

        public void Clear()
        {
            Headers.Clear();
        }

        public bool Contains(KeyValuePair<string, string[]> item)
        {
            return ((ICollection<KeyValuePair<string, string[]>>)Headers).Contains(item);
        }

        public void CopyTo(KeyValuePair<string, string[]>[] array, int arrayIndex)
        {
            ((ICollection<KeyValuePair<string, string[]>>)Headers).CopyTo(array, arrayIndex);
        }

        public bool Remove(KeyValuePair<string, string[]> item)
        {
            return ((ICollection<KeyValuePair<string, string[]>>)Headers).Remove(item);
        }

        public int Count
        {
            get
            {
                return Headers.Count;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        #endregion

        #region IEnumerable implementation

        public IEnumerator<KeyValuePair<string, string[]>> GetEnumerator()
        {
            return ((IEnumerable<KeyValuePair<string, string[]>>)Headers).GetEnumerator();
        }

        #endregion

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public HeaderDictionary()
        {
            Headers = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        }
    }
}

