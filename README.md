# JSPack
#### JavaScript packaging on Windows made easy.

It's easy to write shell scripts on a Mac or Linux machine to package your JavaScript.
It's terrible and painful to do it in Windows. So here you go, a flexible, easy
to use packaging system you can integrate into your Windows build process.

Right now **JSPack** uses [YUI Compressor](http://developer.yahoo.com/yui/compressor/) 
exclusively for minification. This as the real consequence of causing the packing 
process to fail if you have any JavaScript syntax errors, so be warned. If there 
is any demand, I'll make this pluggable. You will also need to have **Java** installed 
and available in your system path.

## Building

Load the project in Visual Studio and build. Wow, that was easy.

## Using

Scripts are packaged using an XML definition file we'll refer to as a map:

    <?xml version="1.0" encoding="utf-8" ?>
	<!-- This is an example map file to demonstrate common features. 
	     Note that all paths are relatative to src and target, 
	     which are relative to the map file. The version value can 
	     be used for cache-busting file names. -->
	<jspack src=".\src" target=".\build" version="1.0.0" minify="true" clean="false">
	  <!-- Name an output to import it later. You can also prevent cache-busting 
	       version appending by setting version="false" -->
	  <output name="jQuery" path=".\..\lib\jquery-1.3.2.js" version="false" minify="false">
	    <input path=".\..\lib\jquery\jquery-1.3.2.js"/>
	    <input path=".\..\lib\jquery\plugins\jquery.ancestry.js"/>
	    <input path=".\..\lib\jquery\plugins\jquery.json-1.3.js"/>
	    <input path=".\..\lib\jquery\plugins\jquery.parsequery.js"/>
	  </output>
	  <!-- Define a temporary output that you only want to use as an import later. -->
	  <output name="Common" path="common.js" minify="false" temporary="true">
	    <import name="jQuery"/>
	    <input path="MyCommonScript1.js"/>
	    <input path="MyCommonScript2.js"/>
	    <input path="MyCommonScript3.js"/>
	  </output>
	  <!-- This will be a finished script at .\build\public-1.0.0.js. It will be minified 
	       and include all of the scripts in Common, which itself includes all of the 
	       scripts in jQuery. -->
	  <output path="public.js">
	    <import name="Common"/>
	    <input path="MyPublicScript1.js"/>
	    <input path="MyPublicScript2.js"/>
	  </output>
	  <output path="admin.js">
	    <import name="Common"/>
	    <input path="MyAdminScript1.js"/>
	  </output>
	</jspack>
	
The root `jspack` element contains context definition parameters, each of which can be
overridden at the command line using the syntax `/param:value`. An example command
line invocation then:

    jspack /map:C:\path\to\map.xml /minify:false

Arguments:

-  **map**: Command-line only. Path to the map file defining the build.
-  **src**: Command-line or map-defined. Identifies the root directory of the script sources.
-  **target**: Command-line or map-defined. Identifies the root directory of the outputs.
-  **version**: Command-line or map-defined. Optionally identifies a version number to add to
   output path names for automatic cache-busting.
-  **minify**: Command-line or map-defined. A value indicating whether minification is enabled.
-  **clean**: Command-line or map-defined. A value indicating whether to delete the contents
   of the output directory prior to building.

The only required argument is **map**. Both **src** and **target** are relative to the map
file unless fully qualified. If not specified, they will both default to the same directory
as the map file. Leave **version** empty to prevent automatic versioning. When omitted, 
**minify** defaults to `true` and **clean** defaults to `false`.

After that you have a set of outputs. Each output represents a collection of scripts that
are concatenated and possibly minified. Output sources can either come from `input` or
`import` declarations. Inputs are paths to script files. Imports are names references
to previously-defined named outputs.

Output arguments:

- **name**: *Optional.* The name of the output for use as an import later.
- **path**: *Required.* The path to write the output to. Relative to `target`
  if not fully qualified.
- **version**: *Optional.* A value indicating whether to version the output
  if the map has a version number defined. Defaults to `true` if omitted.
- **minify**: *Optional.* A value indicating whether to minify the output
  if the map has minification enabled. Defaults to `true` if omitted.
- **temporary**: *Optional.* A value indicating whether the output should be
  deleted at the end of the build process. Defaults to `false` if omitted.