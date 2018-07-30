# Babyscript
A made-up regular language made to conform to X:Rebirth's subset of XML. The syntax looks like a C-like language if you squint, and by recognising XR's expression syntax the language frees you from having to wrap your attribute values in double quotes (in most cases). It's meant as an alternative way to write scripts for XR which, unlike XML, doesn't make your eyes bleed, or make you write a lot of cumbersome angle brackets, double quotes, or repeat the name of an element in its end tag.

## Syntax
Being something of a syntactic replacement of XML, it has XML's basic structure as a tree of elements, each of which can have zero or more attributes. If an element has children, these are listed in curly braces, otherwise the element is terminated with a semicolon:

```
rootNode
{
  firstChild
  {
    grandChild;
  }
  secondChild;
}
```

If an element has attributes these go inside parentheses, and have the syntax of `name=value`. `value` must be a valid expression in XR's script engine - if an attribute value is needed which doesn't satisfy this, it can be wrapped in double quotes (which will be removed during compilation): `name="value"`. Eg:

```
do_if(value=player.primaryship.hullpercent > 90)
{
  debug_text(text='Hello from cue at '+player.age, filter="general");
}
```

There's also a shortcut for exact `set_value`s which have an assignment-like syntax - eg

`$bob = 5;`

... is compiled to:

`<set_value name="$bob" exact="5" />`.

C-like `//single-line comments` and `/* block comments */` are both recognised. However, they are treated slightly differently. Single-line comments are ignored by the compiler, but block comments are compiled to XML comments (and so can only be placed in an element's child list, like a child element).

## Semantic shortcuts
The compiler has two systems it can use to further improve the convenience and readability of Babyscript files.

### Name Shortcuts
This is a one-to-one mapping of a valid element name to a user-defined shorthand version (although the default configuration, in this repository, is fairly common-sensical). It enables you to write the shortened version in your Babyscript file, which the compiler will change into its full version, as specified in the name shortcut config file. This is mostly a reaction to Egosoft's choice of overly-verbose tag names like `do_if`, `do_while` etc, when you really only want to type `if` or `while`.

### Anonymous attribute shortcuts
This is a one-to-many mapping of an element name (after name shortcut processing) onto one or more attribute names, with a specific order. If you're writing an element with name `foo`, and this is mapped onto `n` attribute names, you can omit the name of up to `n` attributes, and the compiler will add them in the order specified. This is useful for elements where you tend to use certain attribute(s) every time, and often in a particular order. For example, `do_if` is almost always written with a `value` attribute exclusively. Using anonymous attributes, you don't need to write out that attribute name every time:

```
do_if(player.isfemale)
{
  ...
}
```

## Bringing it all together
The combined effect of the syntax, plus the shortcuts, is that you can write XR scripts in a kind of C-like syntax, and not have to worry about typing XML (which isn't much meant for humans to be typing out, anyway).

If an element has attributes, but not children, it looks like a function call:
```
debug_text('Hello from cue at '+player.age, "general");
```

If an attribute has children, it looks like some a code block or statement:
```
if($enemyShip.exists)
{
  ...
}
cue
{
  actions
  {
    ...
  }
}
```

And if an attribute has neither, it looks like another kind of single-line statement:
```
break;
continue;
return;
```

There are many cases, of course, where this doesn't line up exactly. For example, when `return` has an attribute (which wouldn't need parentheses in real C-like languages) or a command has children (which I'm not sure is a thing in C-like languages).
