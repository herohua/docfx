$(function() {
  var active = 'active';
  var expanded = 'expanded';
  var tocAnimationOptions = null;
  var navbarPath = $("meta[property='docfx\\:nav'").attr("content");
  var tocPath = $("meta[property='docfx\\:toc'").attr("content");
  var title = $("meta[property='docfx\\:appName'").attr("content");
  var tocTitle = $("meta[property='docfx\\:tocTitle'").attr("content");
  var rel = getRelativePath("rel");
  var navrel = getRelativePath("navrel");
  var tocrel = getRelativePath("tocrel");
  var getList = getListTemplate();
  renderMarkdown();
  setupAffix();
  var tocStatusKey = "toc_status";
  var currentPath = window.location.pathname;

  (function() {
    var header = $("#_header");
    var folded = Cookies.get(tocStatusKey) === 'folded';
    initToc(header, folded);
    if (tocPath) {
      $.get(tocPath, function(toc) {
        var container = header.find("#toc_wrapper>aside");
        container.html(toc);
        container.find('ul').addClass("nav");
        container.children('ul').addClass("toc_list");

        // check to find the active element
        container.find('a[href]').each(function(i, e) {
            var href = $(e).attr("href");
            if (isRelativePath(href)) {
              // Update href to be relative to current page
              if (tocrel) {
                href = tocrel + href;
                $(e).attr("href", href);
              }
              if ($('<a href="' + href + '"></a>')[0].pathname === currentPath) {
                var breadcrumb = [{name: tocTitle}];
                $(e).parent().addClass(active);
                var parent = $(e).parent().parent().parent().children('a');
                parent.addClass(active);
                parent.each(function(i, e){
                  breadcrumb.push({href: e.href, name: e.innerText});
                })
                breadcrumb.push({href: e.href, name: e.innerText});
                setupBreadCrumb(breadcrumb);
              }
            }
        });
      });
    }
    if (navbarPath) {
      $.get(navbarPath, function(nav) {
        var container = header.find("#header .article_btn>.dropdown-menu");
        container.html(nav);

        // Update href to be relative to current page
        if (navrel) {
          container.find('a[href]').each(function(i, e) {
            var href = $(e).attr("href");
            if (isRelativePath(href)) $(e).attr("href", navrel + href);
          })
        }
        // TODO: set active item;
      })
    }

    addEvents();
  })();

  function isRelativePath(href) {
    return !isAbsolutePath(href);
  }

  function isAbsolutePath(href) {
    return (/^(?:[a-z]+:)?\/\//i).test(href);
  }

  function getRelativePath(key) {
    var rel = $("meta[property='docfx\\:" + key + "'").attr("content");
    if (typeof rel === 'undefined') rel = ''; // If relative path is not set, set to current folder
    if (rel && rel[rel.length - 1] !== '/') rel += '/';
    return rel;
  }

  function getListTemplate(){
    var template = Handlebars.compile('<ul class="{{class}}"> \
    {{#items}} \
        <li>{{#if href}}<a href="{{href}}">{{name}}</a>{{else}}{{name}}{{/if}}</li>\
    {{#if this.items}}\
    <ul class="{{../../class}}">\
    {{/if}}\
    {{#this.items}}\
        <li>{{#if href}}<a href="{{href}}">{{name}}</a>{{else}}{{name}}{{/if}}</li>\
    {{/this.items}}\
    {{#if this.items}}\
    </ul>\
    {{/if}}\
    </li>\
    {{/items}}\
    </ul>');

    return function(item, classes){
      var model = {
        class: [].concat(classes).join(" "),
        items: item
      };
      return template(model);
    };
  }

  function addEvents() {
    // Must use $(document) so that default button behavior from jquery get called before stop propagtion
    $(document)
      .on('click', '[data-stopPropagation]', function(e) {
        e.stopPropagation();
      });
    $(document).scroll(function(e) {
      if ($("#_header .open").length > 0) {
        $("#header").addClass("foldfix")
      } else {
        $("#header").removeClass("foldfix")
      }
      if ($(this).scrollTop() > 200) {
        $("#header").addClass("fold");
      } else {
        $("#header").removeClass("fold");
      }
    });
    $(document).mousemove(function(e) {
      if ($("#_header .open").length > 0) {
        $("#header").addClass("foldfix")
      } else {
        $("#header").removeClass("foldfix")
      }
      if (e.clientY < 60) {
        $("#header").addClass("hovered");
      } else {
        $("#header").removeClass("hovered");
      }
    });
  }

  function renderMarkdown() {
    var md = (function() {
      marked.setOptions({
        gfm: true,
        pedantic: false,
        sanitize: false
      });
      var toHtml = function(markdown) {
        if (!markdown) return '';
        return marked(markdown);
      }
      return {
        toHtml: toHtml
      };
    })();
    $('.markdown').each(function(index, item) {
      $(item).html(md.toHtml(item.innerHTML));
    });
    // 'pre code' stands for code block
    $('pre code').each(function(index, item) {
      hljs.highlightBlock(item);
    });
  }

  function setupBreadCrumb(items){
    var html = getList(items, 'breadcrumb')
    $('#breadcrumb').html(html);
  }

  function setupAffix() {
    // supported headers are h1, h2, h3, and h4
    // The topest header is ignored
    var selector = "#content article";
    var affixSelector = "#affix";
    var headers = ['h4', 'h3', 'h2', 'h1'];
    var hierarchy = [];
    var toppestIndex = -1;
    var startIndex = -1;
    // 1. get header hierarchy
    for (var i = headers.length - 1; i >= 0; i--) {
      var headerSelector = selector + " " + headers[i];
      var header = $(headerSelector);
      var length = header.length;

      // If contains no header in current selector, find the next one
      if (length === 0) continue;
      // If the toppest header contains only one item, e.g. title, ignore
      if (length === 1 && hierarchy.length === 0 && toppestIndex < 0) {
        toppestIndex = i;
        continue;
      }

      // Get second level children
      var nextLevelSelector = i > 0 ? headers[i - 1] : null;
      var prevSelector;
      for (var j = length - 1; j >= 0; j--) {
        var e = header[j];
        var id = e.id;
        if (!id) continue; // For affix, id is a must-have
        var item = {
          name: e.innerText,
          href: "#" + id,
          items: []
        };
        if (nextLevelSelector) {
          var selector = '#' + id + "~" + nextLevelSelector;
          if (prevSelector) selector += ":not(" + prevSelector + ")";
          $(header[j]).siblings(selector).each(function(index, e) {
            if (e.id) {
              item.items.push({
                name: e.innerText,
                href: "#" + e.id

              })
            }
          })
          prevSelector = selector;
        }
        hierarchy.push(item);
      }

      break;
    };
    hierarchy.reverse();
    var html = getList(hierarchy, ['nav', 'bs-docs-sidenav']);
    $("#affix .title").after(html);
  }

  function initToc(header, folded) {
    var tocCollapseElement = header.find("#toc_btn");
    var tocExpandElement = header.find(".toc_folded");
    var tocElement = header.find("#toc_wrapper");
    var tocSwipeElement = header.find("#toc_swipe");
    var contentElement = $("#content");
    var headerElement = header.find("#header");
    tocSwipeElement.click(function(e){
      foldTocSideBar();
    })
    if (folded === true) {
      foldTocSideBar();
    } else {
      expandTocSideBar();
    }

    tocCollapseElement.on("click", foldTocSideBar);
    tocExpandElement.on("click", expandTocSideBar);

    function foldTocSideBar() {
      tocElement.hide(tocAnimationOptions);
      contentElement.addClass(active);
      headerElement.addClass(active);
      tocExpandElement.addClass(expanded);
      Cookies.set(tocStatusKey, 'folded');
    }

    function expandTocSideBar() {
      tocExpandElement.removeClass(expanded)
      contentElement.removeClass(active);
      headerElement.removeClass(active);
      tocElement.show(tocAnimationOptions);
      Cookies.set(tocStatusKey, 'expanded');
    }
  }
})
