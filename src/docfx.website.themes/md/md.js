
function transform(input, lang){
  var model = JSON.parse(input);

  if (model && model.items && model.items.length > 0) {
    model = preProcessModel(model, lang);
  }

  return model;
}

function preProcessModel(raw, lang){
  var item = raw.items[0];
  if (!item.type) return raw;
  expandItem(item, raw, lang);
  switch (item.type.toLowerCase()) {
    case 'namespace':
      model = {
        'namespace': item,
      }
      break;
    default:
      model = {
        'class': item,
      }
      break;
  }
  return model;
}

function changeExt(path, ext){
  if (!path) return;
  var pathWithoutExt = path.substring(0, path.lastIndexOf('.'));
  if (ext && ext[0] !== '.') return pathWithoutExt + '.' + ext;
  return pathWithoutExt + ext;
}

// Replace newline with </br> for markdown tables
// names such as Tuple<string, int> should already be html-encoded.
function normalize(content){
  return content.replace(/\n/g, '</br>');
}

function getSpec(ref, lang){
  if (!ref) return null;
  var name = "";
  var spec = ref["spec."+lang];
  if (spec){
    for (var i = 0; i < spec.length; i++) {
      var s =spec[i];
      name += s.href?("[" + s.name + "]("+s.href+")"):s.fullName;
    };
  }else{
    name = ref.href?("["+langFallback(ref,"name",lang)+"]("+ref.href+")"):langFallback(ref,"fullName",lang);
  }
    //  console.log(name);
  return name;
}

function langFallback(item, selector, lang){
  var value = item[selector+"."+lang];
  return value?value:item[selector];
}

function expandItem(item, raw, lang){
  var refs = mapping(raw.references, {});
  refs = mapping(raw.items, refs);
  item.name = langFallback(item, "name", lang);
  item.children = expand(refs, item.children, function(result, ref){
    // normalize
    if (result.expanded === null) result.expanded = {};
    expanded = result.expanded;
    if(ref.summary) {
      ref.summary=normalize(ref.summary);
    }

    // Change from .yml to .md
    ref.href = changeExt(ref.href, '.md');
    var syntax = ref.syntax;
    if(syntax){
      syntax.content=langFallback(syntax, "content", lang);
      if(syntax.parameters){
        for (var i = 0; i < syntax.parameters.length; i++) {
          expandWithSpec(syntax.parameters[i], "type", refs, lang);
        };
      }
      if(syntax.return){
        expandWithSpec(syntax.return, "type", refs, lang);
      }
    }
    var type = ref.type;
    if (!expanded[type]) expanded[type] = [];
    expanded[type].push(ref);
  });
  item.inheritance = expand(refs, item.inheritance, function(result, ref, i){
    if (result.expanded === null) result.expanded = [];
    expanded = result.expanded;
    ref.name = getSpec(ref, lang);
    // Add index for hierarchy view
    ref.index = i;
    expanded.push(ref);
  });
  // Set level for current item as for hierarchy view
  if (item.inheritance) item.level = item.inheritance.length;
  item.inheritedMembers = expand(refs, item.inheritedMembers);
}

function expandWithSpec(val,valKey,refs,lang){
  var refKey = val[valKey];
  var ref = refs[refKey];
  if (ref) val[valKey] = getSpec(ref, lang);
}

function expand(refs, arr, handler){
  // Use an object to save expanded so that it can be changed inside handler
  var result = {expanded: null};
  if (!arr || !refs) return null;
  for (var i=0; i< arr.length; i++) {
    var ref = refs[arr[i]];
    if (ref) {
      if (handler) {
        handler(result, ref, i);
      }else{
        // If no handler is provided, add ref
        if (result.expanded === null) result.expanded = [];
        result.expanded.push(ref);
      }
    }
  }
  return result.expanded;
}

function isArray(arr){
  if(Object.prototype.toString.call(arr) === '[object Array]') return true;
  return false;
}

function mapping(arr, obj){
  if (arr){
    for (var i = 0; i < arr.length; i++) {
      var ref = arr[i];
      obj[ref.uid] = ref;
    };
  }
  return obj;
}

function include(arr, obj){
  return (arr.indexOf(obj) != -1);
}

function select(arr, selector){
  for(var i=0; i<arr.length; i++){
    if (selector(arr[i])) return arr[i];
  }
}