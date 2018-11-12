using System.Collections.Generic;

namespace Parstruct.NET
{
    public class Parser
    {
        public Parser(Dictionary<string, object> definitions)
        {
            foreach (var kvp in definitions) {
                Components.Add(kvp.Key, Component.Create(this, kvp.Value));
            }
        }

        public Parser(string json)
        {
            var definitions = JsonR.BuildLangModel(json);
            foreach (var kvp in definitions) {
                Components.Add(kvp.Key, Component.Create(this, kvp.Value));
            }
        }

        internal Dictionary<string, Component> Components { get; } = new Dictionary<string, Component>();

        public ParsedModel Parse(string component, string input)
        {
            var model = new ParsedModel();
            var context = new ParsingContext();
            context.Success = true;
            model.Result = Components[component].Parse(input, context);
            model.Success = context.Success;
            model.Index = context.Index;
            return model;
        }

        public ParsedModel<T> Parse<T>(string component, string input)
        {
            var model = Parse(component, input);
            return new ParsedModel<T>(model);
        }
    }
}
