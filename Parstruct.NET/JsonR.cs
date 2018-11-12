using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace Parstruct.NET
{
    // This class handles the interpretation of definitions written in json with regex
    internal static class JsonR
    {
        internal static Dictionary<string, object> BuildLangModel(string jsonR)
        {
            var model = JsonRParser.Parse<JObject>("object", jsonR);

            return model.Result.First(p => p.Name == "definitions").Value.Object
                .ToDictionary(p => p.Name, p => ResolveValue(p.Value));
        }
        
        static JsonR()
        {
            var langModel = new Dictionary<string, object> {
                {"identifier", new Regex("[A-Za-z_][A-Za-z0-9_]*")},
                {"doubleQuoteName", new Regex("\"((?:[^\"\\\\]|\\\\.)*)\"")},
                {"singleQuoteName", new Regex("'((?:[^'\\\\]|\\\\.)*)'")},
                {"regexValue", new Regex("\\/((?:[^\\/\\\\]|\\\\.)*)\\/")},
                {"numberValue", new Regex("[-+]?(?:\\d+\\.?\\d*|\\d*\\.?\\d+)")},
                {"boolValue", new Regex("(?:true|false)")},
                {"nullValue", new Regex("(null|undefined)")},
                {"stringValue", new Definition { First = new object[] {"doubleQuoteName", "singleQuoteName"}}},
                {"propertyName", new Definition { First = new object[] {"identifier", "stringValue"}}},
                {"property", new object[] {"propertyName:Name", new Regex("\\s*:\\s*"), "propertyValue:Value"}},
                {"propertyList", new Definition { Repeats = "property", Separator = new Regex("\\s*,\\s*")}},
                {"valueList", new Definition { Repeats= "propertyValue", Separator = new Regex("\\s*,\\s*")}},
                {"object", new Object[] {new Regex("\\{\\s*"), "propertyList::", new Regex("\\s*\\}")}},
                {"array", new Object[] {new Regex("\\[\\s*"), "valueList::", new Regex("\\s*\\]")}},
                {"propertyValue", new Definition {
                    First = new Object [] {"stringValue:String","regexValue:Regex","object:Object","array:Array","numberValue:Number","boolValue:Boolean","nullValue:Other"},
                    Nest = ENestingType.MatchOnly
                }}};
            JsonRParser = new Parser(langModel);
        }

        private static Parser JsonRParser { get; }
        
        private static object ResolveValue(JValue value)
        {
            if (value == null)
                return null;
            if (value.String != null)
                return value.String;
            if (value.Regex != null)
                return new Regex(value.Regex);
            if (value.Array != null)
                return ResolveArray(value.Array);
            if (value.Object != null)
                return ResolveObject(value.Object);
            if (value.Number != null)
                return value.Number.Value;
            if (value.Boolean != null)
                return value.Boolean;
            return null;
        }

        private static object[] ResolveArray(JArray array)
        {
            return array?.Select(ResolveValue).ToArray();
        }

        private static Definition ResolveObject(JObject obj)
        {
            if (obj == null) return null;
            JValue prop(string name) => obj.FirstOrDefault(p => p.Name == name)?.Value;
            var nest = prop("nest")?.String;
            var def = new Definition
            {
                Name = prop("name")?.String,
                Contains = ResolveArray(prop("contains")?.Array),
                First = ResolveArray(prop("first")?.Array),
                Repeats = ResolveValue(prop("repeats")),
                Separator = ResolveValue(prop("separator")),
                Min = (int)(prop("min")?.Number ?? 0),
                Max = (int)(prop("max")?.Number ?? int.MaxValue),
                Nest =
                    nest == "match" ? ENestingType.MatchOnly :
                    nest == "matchonly" ? ENestingType.MatchOnly :
                    nest == "full" ? ENestingType.FullObject :
                    nest == "object" ? ENestingType.FullObject :
                    nest == "fullobject" ? ENestingType.FullObject :
                    ENestingType.None
            };
            var defaultProp = obj.FirstOrDefault(p => p.Name == "default");
            if (defaultProp != null && !defaultProp.Value.IsUndefined) {
                def.Default = defaultProp?.Value?.String;
            }
            return def;
        }

        private static object[] ResolveOneOrManyDefs(JValue value)
        {
            if (value == null)
                return new object[0];
            if (value.String != null)
                return new object[] { value.String };
            if (value.Regex != null)
                return new object[] { value.Regex };
            if (value.Array != null)
                return ResolveArray(value.Array);
            if (value.Object != null)
                return new object[] { ResolveObject(value.Object) };
            return new object[0];
        }

        private class JArray : List<JValue>
        {
        }

        private class JObject : List<JProperty>
        {
        }

        private class JProperty
        {
            public string Name { get; set; }
            public JValue Value { get; set; }
        }

        private class JValue
        {
            public string String { get; set; }
            public string Regex { get; set; }
            public JObject Object { get; set; }
            public JArray Array { get; set; }
            public decimal? Number { get; set; }
            public bool? Boolean { get; set; }
            public string Other { get; set; }
            public bool IsNull => Other == "null";
            public bool IsUndefined => Other == "undefined";
        }
    }
}