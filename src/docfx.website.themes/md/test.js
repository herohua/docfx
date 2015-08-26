fs = require('fs');
Mustache = require('./scripts/mustache');

var content = fs.readFileSync('markdown.tmpl.js', {encoding: 'utf8'});
var template = fs.readFileSync('markdown.tmpl', {encoding: 'utf8'});
eval(content);
console.log(content);
var input = JSON.stringify({
  class: "a"
})

var model = transform(input, "csharp");

output = Mustache.render(template, model);
if (output){
  if (!fs) console.log(output);
  if (fs){ fs.writeFileSync("a.md", output);}
} else{
  console.warn("Template generated nothing.");
}
