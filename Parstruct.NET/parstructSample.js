// -----------------------------------------
// Sample definitions and usage of parstruct

var $input = $('#input');
var $output = $('#output');
var input = $input.html().trim();

var commandParser = new Parstruct({
	definitions: {
  	'nothing': /.{0}/,
  	'member': /[A-Za-z_][A-Za-z0-9_]+/,
    'pluginName': 'member',
    'commandName': 'member',
    'paramAbbr': /[A-Za-z0-9]/,
    'paramName': /[A-Za-z0-9][A-Za-z0-9-]*/,
    'argument': {first:[[/-/, 'paramAbbr:Abbr'],[/--/,'paramName:Name']]},
    'value': {first:[/"((?:[^"\\]|\\.)*)"/,/[^-][^\s;]*/]},
    'argumentValue': {first:[[/(\s+|:)/,'value::'], 'nothing']},
    'argumentAssignment': ['argument:Parameter','argumentValue:Value'],
    'argumentList': {repeats: 'argumentAssignment', separator: /\s+/ },
  	'statement': {
    	contains: [
      	{ name: 'Command',
        	first: [['pluginName:PluginName', /\./,'commandName:Name'], ['commandName:Name']]},
        { name: 'Arguments',
        	contains: [/\s+/,'argumentList::'],
          default: null}],
      default: null
    },
  	'block': {
    	repeats: 'statement',
      separator: /\s*;\s*/}
  }
});

var cSharpParser = new Parstruct({
	definitions: {
  	'identifier': /[A-Za-z_][A-Za-z0-9_]*/,
    'methodName': 'identifier',
    'dataType': 'identifier',
    'argumentName': 'identifier',
    'argumentDefinition':  ['dataType', /\s+/, 'argumentName'],
    'argumentDefinitionList': { repeats: 'argumentDefinition', separator: /\s*,\s*/ },
    'methodSignature': {
    	contains: ['dataType', 'methodName', /\(/, 'argumentDefinitionList', /\)/],
      separator: /\s*/ }
  }
});

var jsonrParser = new Parstruct({
	definitions: {
  	'identifier': /[A-Za-z_][A-Za-z0-9_]*/,
    'doubleQuoteName': /"((?:[^"\\]|\\.)*)"/,
    'singleQuoteName': /'((?:[^'\\]|\\.)*)'/,
    'regexValue': /\/((?:[^\/\\]|\\.)*)\//,
    'numberValue': /[-+]?(?:\d+\.?\d*|\d*\.?\d+)/,
    'boolValue' : /(?:true|false)/,
    'nullValue' : /(?:null|undefined)/,
    'stringValue': { first: ['doubleQuoteName', 'singleQuoteName'] },
    'propertyName': { first: ['identifier', 'stringValue'] },
    'property': ['propertyName:name', /\s*:\s*/, 'propertyValue:value'],
    'propertyList': { repeats: 'property', separator: /\s*,\s*/ },
    'valueList': { repeats: 'propertyValue', separator: /\s*,\s*/ },
    'object': [/\{\s*/, 'propertyList::', /\s*\}/],
    'array': [/\[\s*/, 'valueList::', /\s*\]/],
    'propertyValue': {
    	first: ['stringValue:string','regexValue:regex','object','array',
    	'numberValue:number','boolValue:boolean','nullValue:other'], nest: 'match' }
  }
});

var result = commandParser.Parse('block', input);
$output.html(JSON.stringify(result, null, 4));

