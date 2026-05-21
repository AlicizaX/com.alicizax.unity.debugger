using Cysharp.Text;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace AlicizaX.Console.Pooling
{
    public class Pool<T> where T : class, new()
    {
        private readonly Stack<T> _objs;

        public Pool()
        {
            _objs = new Stack<T>();
        }

        public Pool(int objCount)
        {
            _objs = new Stack<T>(objCount);
            for (int i = 0; i < objCount; i++)
            {
                _objs.Push(new T());
            }
        }

        public T GetObject()
        {
            if (_objs.Count > 0)
            {
                return _objs.Pop();
            }
            else
            {
                return new T();
            }
        }

        public void Release(T obj)
        {
            _objs.Push(obj);
        }
    }

    public class ConcurrentPool<T> where T : class, new()
    {
        private readonly ConcurrentBag<T> _objs;

        public ConcurrentPool()
        {
            _objs = new ConcurrentBag<T>();
        }

        public ConcurrentPool(int objCount)
        {
            _objs = new ConcurrentBag<T>();
            for (int i = 0; i < objCount; i++)
            {
                _objs.Add(new T());
            }
        }

        public T GetObject()
        {
            if (_objs.TryTake(out T obj))
            {
                return obj;
            }

            return new T();
        }

        public void Release(T obj)
        {
            _objs.Add(obj);
        }
    }

    public static class StringBuilderPool
    {
        public static Utf16ValueStringBuilder GetStringBuilder(int minCapacity = 0)
        {
            return ZString.CreateStringBuilder();
        }

        public static string ReleaseAndToString(Utf16ValueStringBuilder stringBuilder)
        {
            try
            {
                return stringBuilder.ToString();
            }
            finally
            {
                stringBuilder.Dispose();
            }
        }
    }
}
