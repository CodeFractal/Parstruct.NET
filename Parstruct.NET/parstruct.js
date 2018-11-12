// -----------------------------------------
// Parstruct.js

(function() {
  function Parstruct(structure) {
  	function ENestingType(string) {
    	if (string == null) return ENestingType.None;
    	string = string.toLowerCase();
      return (
      //string === 'none' ? ENestingType.None :
        string === 'match' ? ENestingType.MatchOnly :
        string === 'matchonly' ? ENestingType.MatchOnly :
        string === 'full' ? ENestingType.FullObject :
        string === 'object' ? ENestingType.FullObject :
        string === 'fullobject' ? ENestingType.FullObject :
        ENestingType.None
      );
    }
    ENestingType.None = 1;
    ENestingType.MatchOnly = 2;
    ENestingType.FullObject = 3;
  
  	function Node(definition) {
    	if (definition == null) return null;
    	if (definition.constructor === String) {
      	var idAlias = definition.split(':');
        var identifier = idAlias[0];
        var alias = 
        	idAlias.length > 1 ?
          	idAlias[1] ? idAlias[1] :
            null :
          identifier;
      
      	var result = { 
        	name: alias,
          isThis: definition.endsWith("::")
        }
      	if (nodes[identifier]) {        	
      		result.parse = nodes[identifier].parse;
        }
        else {
        	result.parse = function(source, ctx) {
          	return nodes[identifier].parse(source, ctx);
          }
        }
        return result;
      } 
      else if (definition.constructor === Array) {
      	return Node({contains:definition});
      } 
      else if (definition.constructor === Object) {
      	if (definition.contains) {        	
          var items = [];
          for (var i = 0; i < definition.contains.length; i++) {
            items.push(Node(definition.contains[i]));
          }
          
          var separator = Node(definition.separator);
          var required = definition.default === undefined;
          return {
            name: definition.name,
            parse: function(source, ctx) {
              var result = {};
              
              var _this = null;
              var overrideThis = false;
              var startIndex = ctx.index;
              for (var i = 0; i < items.length; i++) {
                var comp = items[i];
                var itemResult = comp.parse(source, ctx);
                if (ctx.success && comp.isThis) {
                  overrideThis = true;
                  _this = itemResult;
                }
                if (comp.name) {
                  result[comp.name] = itemResult;
                }      
                if (!ctx.success) break;
                if (i < items.length - 1) {
                	if (separator) separator.parse(source, ctx);
                  if (!ctx.success) break;
                }
              }

              if (ctx.success && overrideThis) result = _this;
              if (!ctx.success && !required) {
                  result = definition.default;
                  ctx.success = true;
                  ctx.index = startIndex;
              }
              return result;
            }
          }
        } 
        else if (definition.repeats) {
        	var min = definition.min || 0;
          var max = definition.max || Infinity;
          var separator = definition.separator ? Node(definition.separator) : null;
          var repeated = Node(definition.repeats);
        	return {
          	name: definition.name,
          	parse: function(source, ctx) {
            	var result = [];
              
              var activeIndex = ctx.index;
              var failureResult = null;
              while (result.length < max && ctx.success) {
              	var itemResult = repeated.parse(source, ctx);
                if (ctx.success) {
              		result.push(itemResult);
                  activeIndex = ctx.index;
                  if (separator !== null) {
                		separator.parse(source, ctx);
                    if (ctx.success) {                    
                  		activeIndex = ctx.index;
                    }
                  }
                }
                else {
                	failureResult = itemResult;
                }
              }
                            
              ctx.success = result.length >= min;
              if (ctx.success)
              	ctx.index = activeIndex;
              else if (failureResult)
              	result.push(failureResult);
                
              return result;
            }
          }
        }
        else if (definition.first) {      	
            var items = [];
            for (var i = 0; i < definition.first.length; i++) {
              items.push(Node(definition.first[i]));
            }
            var nest = ENestingType(definition.nest);
          	var required = definition.default === undefined;
            return {
            	name: definition.name,
            	parse: function(source, ctx) {
              	var result = null;
                var fullNest = (nest === ENestingType.FullObject);
                var singleNest = (nest === ENestingType.MatchOnly);
                var nesting = (fullNest || singleNest);
                
                if (nesting) result = {};
                
                var startIndex = ctx.index;
                var maxFailureIndex = ctx.index;
                var bestFailureResult = null;
                var matched = false;
                for (var i = 0; i < items.length && (!matched || fullNest); i++) {
                	var comp = items[i];
                  if (!matched) ctx.index = startIndex;
                  var itemResult = matched ? null : comp.parse(source, ctx);
                  if (ctx.index > maxFailureIndex) maxFailureIndex = ctx.index;
                  var name = comp.name || ("property"+(i+1));
                  if (ctx.success) {
                  	matched = true;
                    if (nesting) result[name] = itemResult;
                    else result = itemResult;
                  }
                  else {
                  	if (ctx.index > maxFailureIndex) {
                    	maxFailureIndex = ctx.index;
                      bestFailureResult = itemResult;
                    }
                  	ctx.index = startIndex;
                    if (fullNest) {
                      result[name] = null;
                    }
                  }
                }
                
                if (!ctx.success && !required) {
                    result = definition.default;
                    ctx.success = true;
                }
                if (!ctx.success) {
                	ctx.index = maxFailureIndex;
                  result = bestFailureResult;
                }
                return result;
              }
            }
        }
      } 
      else if (definition.constructor === RegExp) {
      	var regStr = definition.toString();
        var regex = new RegExp(regStr.substr(1, regStr.lastIndexOf("/")-1), 'y');
      	return {
        	required: true,
        	parse: function(source, ctx) {
          	var result = null;
          	regex.lastIndex = ctx.index;
            var match = regex.exec(source);
            var success = (match != null);
            ctx.success = success;
            if (success) {
            	ctx.index = regex.lastIndex
              result = match.length > 1 ? match[1] : match[0];
            }
            return result;
          }
        }
      } 
      else return null;
    }
  	var nodes = {};
    var keys = Object.keys(structure.definitions);
    for (var i = 0; i < keys.length; i++) {
    	var node = Node(structure.definitions[keys[i]]);
      if (node == null) throw("Unrecognized definition type (" + keys[i] + ")");
      node.name = keys[i];
    	nodes[keys[i]] = node;
    }
    this.Parse = function(type, source) {
    	var context = {
      	success: true,
        index: 0
      };
    	context.result = nodes[type].parse(source, context);
      return context;
    }
  }  
  this.Parstruct = Parstruct;
})();

