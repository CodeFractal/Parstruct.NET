using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Text.RegularExpressions;

namespace Parstruct.NET {
    internal class Component
    {
        private Component(Parser parser)
        {
            Parser = parser;
        }
        
        public string Name { get; private set; }
        public Parser Parser { get; }
        private Regex Regex { get; set; }
        private Component[] Contains { get; set; }
        private Component[] First { get; set; }
        private Component Repeated { get; set; }
        private Component Separator { get; set; }
        private int Min { get; set; }
        private int Max { get; set; }
        private ENestingType Nest { get; set; } = ENestingType.None;
        private string ProxyFor { get; set; }
        private bool IsThis { get; set; }
        private bool Required { get; set; } = true;
        private string Default { get; set; }

        public delegate object ParseDelegate(string source, ParsingContext context);

        public ParseDelegate Parse { get; private set; }

        internal static Component Create(Parser parser, object def)
        {
            switch (def) {
                case Regex expression:
                    return Create(parser, expression);
                case Definition definition:
                    return Create(parser, definition);
                case string identifier:
                    return Create(parser, identifier);
                case object[] series:
                    return Create(parser, series);
                case null:
                    return null;
            }

            throw new NotImplementedException();
        }

        private static Component Create(Parser parser, Regex expression)
        {
            var result = new Component(parser);
            result.Regex = expression;
            result.Parse = result.ParseWithRegex;
            return result;
        }

        private static Component Create(Parser parser, Definition definition)
        {
            Component result = null;
            if (definition.Contains != null) {
                var items = new List<Component>();
                foreach (var def in definition.Contains) {
                    items.Add(Create(parser, def));
                }

                result = new Component(parser);
                result.Contains = items.ToArray();
                result.Separator = Create(parser, definition.Separator);
                result.Parse = result.ParseWithContains;
            }
            else if (definition.Repeats != null) {
                result = new Component(parser);
                result.Min = definition.Min;
                result.Max = definition.Max;
                if (definition.Separator != null)
                    result.Separator = Create(parser, definition.Separator);
                result.Repeated = Create(parser, definition.Repeats);
                result.Parse = result.ParseWithRepeats;
            }
            else if (definition.First != null) {
                var items = new List<Component>();
                foreach (var def in definition.First) {
                    items.Add(Create(parser, def));
                }

                result = new Component(parser);
                result.First = items.ToArray();
                result.Nest = definition.Nest;
                result.Parse = result.ParseWithFirst;
            }

            if (result != null) {
                result.Name = definition.Name;
                result.Required = definition.Required;
                result.Default = definition.Default;
            }

            return result;
        }

        private static Component Create(Parser parser, string definition)
        {
            var idAlias = definition.Split(':');
            var identifier = idAlias[0];
            string alias = 
                idAlias.Length > 1 ?
                    !string.IsNullOrEmpty(idAlias[1]) ? idAlias[1] :
                    null :
                identifier;

            var result = new Component(parser);
            result.Name = alias;
            result.IsThis = definition.EndsWith("::");
            if (parser.Components.ContainsKey(identifier)) {
                result.Parse = parser.Components[identifier].Parse;
            }
            else {
                result.ProxyFor = identifier;
                result.Parse = result.ParseAsProxy;
            }
            return result;
        }

        private static Component Create(Parser parser, object[] series)
        {
            return Create(parser, new Definition {Contains = series});
        }

        private string ParseWithRegex(string source, ParsingContext ctx)
        {
            string result = null;
            var match = Regex.Match(source, ctx.Index);
            bool success = match.Success && match.Index == ctx.Index;
            ctx.Success = success;
            if (success) {
                ctx.Index += match.Length;
                result = match.Groups.Count > 1 ? match.Groups[1].Value : match.Value;
            }

            return result;
        }

        private object ParseWithContains(string source, ParsingContext ctx)
        {
            object result = new ExpandoObject();

            object _this = null;
            bool overrideThis = false;
            int startIndex = ctx.Index;
            for (var i = 0; i < Contains.Length; i++) {
                var comp = Contains[i];
                var itemResult = comp.Parse(source, ctx);
                if (ctx.Success && comp.IsThis) {
                    overrideThis = true;
                    _this = itemResult;
                }
                if (comp.Name != null) {
                    ((IDictionary<string, object>)result)[comp.Name] = itemResult;
                }
                if (!ctx.Success) break;
                if (i < Contains.Length - 1) {
                    Separator?.Parse(source, ctx);
                    if (!ctx.Success) break;
                }
            }

            if (ctx.Success && overrideThis) result = _this;
            if (!ctx.Success && !Required) {
                result = Default;
                ctx.Success = true;
                ctx.Index = startIndex;
            }
            return result;
        }

        private object ParseWithFirst(string source, ParsingContext ctx)
        {
            object result = null;
            bool fullNest = Nest == ENestingType.FullObject;
            bool singleNest = Nest == ENestingType.MatchOnly;
            bool nesting = fullNest || singleNest;

            if (nesting) result = new ExpandoObject();

            int startIndex = ctx.Index;
            int maxFailureIndex = ctx.Index;
            object bestFailureResult = null;
            bool matched = false;
            for (var i = 0; i < First.Length && (!matched || fullNest); i++) {
                var comp = First[i];
                if (!matched) ctx.Index = startIndex;
                var itemResult = matched ? null : comp.Parse(source, ctx);
                if (ctx.Index > maxFailureIndex) maxFailureIndex = ctx.Index;
                var name = comp.Name ?? $"property{i + 1}";
                if (ctx.Success) {
                    matched = true;
                    if (nesting) ((IDictionary<string, object>)result)[name] = itemResult;
                    else result = itemResult;
                }
                else {
                    if (ctx.Index > maxFailureIndex) {
                        maxFailureIndex = ctx.Index;
                        bestFailureResult = itemResult;
                    }
                    ctx.Index = startIndex;
                    if (fullNest) {
                        ((IDictionary<string, object>)result)[name] = null;
                    }
                }
            }

            if (!ctx.Success && !Required) {
                result = Default;
                ctx.Success = true;
            }
            if (!ctx.Success) {
                ctx.Index = maxFailureIndex;
                result = bestFailureResult;
            }
            return result;
        }

        private object[] ParseWithRepeats(string source, ParsingContext ctx)
        {
            var result = new List<object>();

            int activeIndex = ctx.Index;
            object failureResult = null;
            while (result.Count < Max && ctx.Success) {
                var itemResult = Repeated.Parse(source, ctx);
                if (ctx.Success) {
                    result.Add(itemResult);
                    activeIndex = ctx.Index;
                    if (Separator != null) {
                        Separator.Parse(source, ctx);
                        if (ctx.Success) {
                            activeIndex = ctx.Index;
                        }
                    }
                }
                else {
                    failureResult = itemResult;
                }
            }
            
            ctx.Success = result.Count >= Min;
            if (ctx.Success)
                ctx.Index = activeIndex;
            else if (failureResult != null)
                result.Add(failureResult);

            return result.ToArray();
        }

        private object ParseAsProxy(string source, ParsingContext ctx)
        {
            return Parser.Components[ProxyFor].Parse(source, ctx);
        }
    }
}