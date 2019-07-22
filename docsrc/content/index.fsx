(*** hide ***)
// This block of code is omitted in the generated HTML documentation. Use 
// it to define helpers that you do not want to show in the documentation.
#I "../../bin"

(**
limitbreak
======================

Documentation

<div class="row">
  <div class="span1"></div>
  <div class="span6">
    <div class="well well-small" id="nuget">
      The limitbreak library can be <a href="https://nuget.org/packages/limitbreak">installed from NuGet</a>:
      <pre>PM> Install-Package limitbreak</pre>
    </div>
  </div>
  <div class="span1"></div>
</div>

Example
-------

Create a "leaky bucket" style event limit.
EventId 100 has a decay rate of 1/second and a maximum value of 1360. 
Once the event counter surpasses 1000 it will be have a MonitorStatus of Limited and will not return to Ok until 
the counter dacays back under 1000.
*)
#r "limitbreak.dll"
open limitbreak

let eventId = 100
let eventMonitor = new EventMonitor(eventId, [limitbreak.createDecayRate 1.0; limitbreak.createValueCap 1360; limitbreak.createThreshold 1000])

let (status, count) = eventMonitor.GetStatus

printfn "Status %A - Counter %i" status count

(**
The EventMonitor takes a list of "Event Processor" functions that are chained together. Each function has access to the current event counter value and is able to make updates to it.
Each Event Processor also receives the current MonitorStatus and can return an updated MonitorStatus which will be supplied to the next function in the chain.

Samples & documentation
-----------------------

The library comes with comprehensible documentation. 
It can include tutorials automatically generated from `*.fsx` files in [the content folder][content]. 
The API reference is automatically generated from Markdown comments in the library implementation.

 * [Tutorial](tutorial.html) contains a further explanation of this sample library.

 * [API Reference](reference/index.html) contains automatically generated documentation for all types, modules
   and functions in the library. This includes additional brief samples on using most of the
   functions.
 
Contributing and copyright
--------------------------

The project is hosted on [GitHub][gh] where you can [report issues][issues], fork 
the project and submit pull requests. If you're adding a new public API, please also 
consider adding [samples][content] that can be turned into a documentation. You might
also want to read the [library design notes][readme] to understand how it works.

The library is available under Public Domain license, which allows modification and 
redistribution for both commercial and non-commercial purposes. For more information see the 
[License file][license] in the GitHub repository. 

  [content]: https://github.com/jburman/limitbreak/tree/master/docs/content
  [gh]: https://github.com/jburman/limitbreak
  [issues]: https://github.com/jburman/limitbreak/issues
  [readme]: https://github.com/jburman/limitbreak/blob/master/README.md
  [license]: https://github.com/jburman/limitbreak/blob/master/LICENSE.txt
*)
