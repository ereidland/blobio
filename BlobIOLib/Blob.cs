using System;
using System.Collections;
using System.Collections.Generic;

namespace BlobIO
{
    #region Blob Type Enum

    public enum BlobEncoding : byte
    {
        Null    = 0,
        String  = 1,
        Map     = 2,
        Array   = 3,
        Int     = 4,
        Float   = 5,
        Bool    = 6,
    }

    #endregion

    #region Blob Base Class
    public abstract class Blob
    {
        public virtual Blob this[string index]
        {
            get { return null; }
            set {}
        }

        public virtual Blob this[int index]
        {
            get { return null; }
            set {}
        }

        public BlobEncoding Encoding { get; private set; }

        public virtual Blob Remove(string key) { return null; }
        public virtual Blob Remove(int index) { return null; }

        public virtual void Add(Blob blob) {}
        public virtual void Add(int index, Blob blob) {}
        public virtual void Add(string key, Blob blob) {}

        public virtual IEnumerable<Blob> Children { get { yield break; } }
        public IEnumerable<Blob> DeepChildren
        {
            get
            {
                foreach (var child in Children)
                    if (child != null)
                        foreach (var deepChild in child.DeepChildren)
                            yield return deepChild;
            }
        }

        public virtual int AsInt
        {
            get
            {
                int result;
                TryGetInt(out result);
                return result;
            }
        }

        public override string ToString()
        {
            string value = Value;
            if (value != null)
                return "\"" + value.Replace("\"", "\\\"") + "\""; //Escape string.
            return "\"\"";
        }

        public virtual bool AsBool
        {
            get
            {
                bool result;
                TryGetBool(out result);
                return result;
            }
        }

        public virtual float AsFloat
        {
            get
            {
                float result;
                TryGetFloat(out result);
                return result;
            }
        }

        public virtual bool TryGetInt(out int result)
        {
            return Value != null ? int.TryParse(Value, out result) : false;
        }

        public virtual bool TryGetBool(out bool result)
        {
            return Value != null ? bool.TryParse(Value, out result) : false;
        }

        public virtual bool TryGetFloat(out float result)
        {
            return Value != null ? float.TryParse(Value, out result) : false;
        }

        public virtual int Count { get { return 0; } }
        public virtual string Value { get { return null; } }

        public Blob(BlobEncoding encoding) { Encoding = encoding; }
    }

    #endregion

    #region Blob Primitives
    public class BlobString : Blob
    {
        string _value;

        public override string Value
        {
            get { return _value; }
        }

        public BlobString(string value = "") : base(BlobEncoding.String) { _value = value; }
    }

    public class BlobInt : Blob
    {
        int _value;

        public override string Value
        {
            get { return _value.ToString(); }
        }

        public override bool TryGetInt(out int result)
        {
            result = _value;
            return true;
        }

        public override bool TryGetFloat(out float result)
        {
            result = _value;
            return true;
        }

        public override bool TryGetBool(out bool result)
        {
            result = _value == 0;
            return true;
        }

        public BlobInt(int number = 0) : base(BlobEncoding.Int) { _value = number; }
    }

    public class BlobFloat : Blob
    {
        float _value;

        public override string Value
        {
            get { return _value.ToString(); }
        }

        public override bool TryGetInt(out int result)
        {
            result = (int)_value;
            return true;
        }

        public override bool TryGetFloat(out float result)
        {
            result = _value;
            return true;
        }

        public override bool TryGetBool(out bool result)
        {
            result = _value == 0;
            return true;
        }

        public BlobFloat(float number = 0) : base (BlobEncoding.Float) { _value = number; }
    }

    public class BlobBool : Blob
    {
        bool _value;

        public override string Value
        {
            get { return _value.ToString(); }
        }

        public override bool TryGetInt(out int result)
        {
            result = _value ? 1 : 0;
            return true;
        }

        public override bool TryGetFloat(out float result)
        {
            result = _value ? 1 : 0;
            return true;
        }

        public override bool TryGetBool(out bool result)
        {
            result = _value;
            return true;
        }

        public BlobBool(bool state = false) : base (BlobEncoding.Bool) { _value = state; }
    }
    #endregion

    #region Blob Array

    public class BlobArray : Blob, IEnumerable
    {
        private List<Blob> _list;

        public override Blob this[int index]
        {
            get
            {
                if (index >= 0 && index < _list.Count)
                    return _list[index];
                return null;
            }
            set
            {
                if (index >= 0)
                {
                    if (index < _list.Count)
                        _list[index] = value;
                    else
                        _list.Insert(index, value);
                }
            }
        }

        public override int Count { get { return _list.Count;  } }

        public override Blob Remove(int index)
        {
            if (index >= 0 && index < _list.Count)
            {
                var existing = _list[index];
                _list.RemoveAt(index);
                return existing;
            }
            return null;
        }

        public override void Add(Blob blob)
        {
            _list.Add(blob);
        }

        public override void Add(int index, Blob blob)
        {
            if (index >= 0)
                _list.Insert(index, blob);
        }

        public override IEnumerable<Blob> Children
        {
            get
            {
                foreach (var blob in _list)
                    yield return blob;
            }
        }

        public IEnumerator GetEnumerator() { return _list.GetEnumerator(); }

        public override string ToString ()
        {
            string str = "[";
            for (int i = 0; i < _list.Count; i++)
            {
                var item = _list[i];
                if (item != null)
                    str += item.ToString();
                else
                    str += "\"\"";

                if (i < _list.Count - 1)
                    str += ", ";
            }
            str += "]";
            return str;
        }

        public BlobArray() : base (BlobEncoding.Array)
        {
            _list = new List<Blob>();
        }
    }

    #endregion

    #region Blob Map

    public class BlobMap : Blob, IEnumerable
    {
        private Dictionary<string, Blob> _dic;

        public override Blob this[string index]
        {
            get
            {
                if (index != null)
                {
                    Blob result;
                    _dic.TryGetValue(index, out result);
                    return result;
                }
                return null;
            }
            set
            {
                if (index != null)
                    _dic[index] = value;
            }
        }

        public override int Count
        {
            get { return _dic.Count; }
        }

        public IEnumerator GetEnumerator() { return _dic.GetEnumerator(); }

        public override IEnumerable<Blob> Children
        {
            get
            {
                foreach (var pair in _dic)
                    yield return pair.Value;
            }
        }

        public override void Add(string key, Blob value) { this[key] = value; }

        public override Blob Remove(string key)
        {
            Blob removed;
            if (_dic.TryGetValue(key, out removed))
                _dic.Remove(key);
            return removed;
        }

        public BlobMap() : base (BlobEncoding.Map) { _dic = new Dictionary<string, Blob>(); }
    }

    #endregion
}

