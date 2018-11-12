using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace Parstruct.NET
{
    public class ParsedModel
    {
        internal ParsedModel() { }
        public bool Success { get; internal set; }
        public int Index { get; internal set; }
        public object Result { get; internal set; }
    }

    public class ParsedModel<T> : ParsedModel
    {
        internal ParsedModel(ParsedModel parsedModel)
        {
            Success = parsedModel.Success;
            Index = parsedModel.Index;
            string serialized = JsonConvert.SerializeObject(parsedModel.Result);
            Result = JsonConvert.DeserializeObject<T>(serialized);
        }

        public new T Result { get; internal set; }
    }
}
