// Copyright 2004-2007 Castle Project - http://www.castleproject.org/
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace Castle.Components.DictionaryAdapter
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Text;

	/// <summary>
	/// Identifies a property should be represented as a
	/// delimited string value.
	/// </summary>
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
	public class DictionaryStringListAttribute : Attribute, IDictionaryPropertyGetter
	{
		private char separator = ',';

		/// <summary>
		/// Gets the separator.
		/// </summary>
		public char Separator
		{
			get { return separator; }
			set { separator = value; }
		}

		#region IDictionaryPropertyGetter

		object IDictionaryPropertyGetter.GetPropertyValue(
			IDictionaryAdapterFactory factory, IDictionary dictionary,
			string key, object storedValue, PropertyDescriptor property)
		{
			Type propertyType = property.PropertyType;

			if (storedValue == null ||
			    !storedValue.GetType().IsInstanceOfType(propertyType))
			{
				if (propertyType.IsGenericType)
				{
					Type genericDef = propertyType.GetGenericTypeDefinition();

					if (genericDef == typeof(IList<>) ||
					    genericDef == typeof(ICollection<>) ||
					    genericDef == typeof(List<>) ||
					    genericDef == typeof(IEnumerable<>))
					{
						Type paramType = propertyType.GetGenericArguments()[0];
						TypeConverter converter = TypeDescriptor.GetConverter(paramType);

						if (converter != null && converter.CanConvertFrom(typeof(string)))
						{
							Type genericList = typeof(StringListWrapper<>).MakeGenericType(
								new Type[] { paramType });
							return Activator.CreateInstance(genericList, key, storedValue,
							                                separator, dictionary);
						}
					}
				}
			}

			return storedValue;
		}

		#endregion
	}

	#region StringList

	internal class StringListWrapper<T> : IList<T>
	{
		private readonly string key;
		private readonly char separator;
		private readonly IDictionary dictionary;
		private readonly List<T> inner;

		public StringListWrapper(string key, string list,
		                         char separator, IDictionary dictionary)
		{
			this.key = key;
			this.separator = separator;
			this.dictionary = dictionary;
			inner = new List<T>();

			ParseList(list);
		}

		#region IList<T> Members

		public int IndexOf(T item)
		{
			return inner.IndexOf(item);
		}

		public void Insert(int index, T item)
		{
			inner.Insert(index, item);
			SynchronizeDictionary();
		}

		public void RemoveAt(int index)
		{
			inner.RemoveAt(index);
			SynchronizeDictionary();
		}

		public T this[int index]
		{
			get { return inner[index]; }
			set
			{
				inner[index] = value;
				SynchronizeDictionary();
			}
		}

		#endregion

		#region ICollection<T> Members

		public void Add(T item)
		{
			inner.Add(item);
			SynchronizeDictionary();
		}

		public void Clear()
		{
			inner.Clear();
			SynchronizeDictionary();
		}

		public bool Contains(T item)
		{
			return inner.Contains(item);
		}

		public void CopyTo(T[] array, int arrayIndex)
		{
			inner.CopyTo(array, arrayIndex);
		}

		public int Count
		{
			get { return inner.Count; }
		}

		public bool IsReadOnly
		{
			get { return false; }
		}

		public bool Remove(T item)
		{
			if (inner.Remove(item))
			{
				SynchronizeDictionary();
				return true;
			}
			return false;
		}

		#endregion

		#region IEnumerable<T> Members

		public IEnumerator<T> GetEnumerator()
		{
			return inner.GetEnumerator();
		}

		#endregion

		#region IEnumerable Members

		IEnumerator IEnumerable.GetEnumerator()
		{
			return inner.GetEnumerator();
		}

		#endregion

		private void ParseList(string list)
		{
			if (list != null)
			{
				TypeConverter converter = TypeDescriptor.GetConverter(typeof(T));

				foreach(string item in list.Split(separator))
				{
					inner.Add((T) converter.ConvertFrom(item));
				}
			}
		}

		private void SynchronizeDictionary()
		{
			bool first = true;
			StringBuilder builder = new StringBuilder();

			foreach(T item in inner)
			{
				if (first)
				{
					first = false;
				}
				else
				{
					builder.Append(separator);
				}

				builder.Append(item.ToString());
			}

			dictionary[key] = builder.ToString();
		}
	}

	#endregion
}