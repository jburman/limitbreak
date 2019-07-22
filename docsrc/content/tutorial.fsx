(*** hide ***)
// This block of code is omitted in the generated HTML documentation. Use 
// it to define helpers that you do not want to show in the documentation.
#I "../../bin"

(**
Introducing your project
========================

TODO

*)
#r "limitbreak.dll"
open limitbreak

let eventId = 100
let eventMonitor = new EventMonitor(eventId, [limitbreak.createDecayRate 1.0; limitbreak.createValueCap 1360; limitbreak.createThreshold 1000])

let (status, count) = eventMonitor.GetStatus

printfn "Status %A - Counter %i" status count
(**

*)
