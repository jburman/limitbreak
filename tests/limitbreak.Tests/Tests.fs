namespace limitbreak.Tests

open Expecto
open FsCheck
open GeneratorsCode
open limitbreak
open System.Threading

module Tests =
    let config10k = { FsCheckConfig.defaultConfig with maxTest = 10000}
    // bug somewhere:  registering arbitrary generators causes Expecto VS test adapter not to work
    //let config10k = { FsCheckConfig.defaultConfig with maxTest = 10000; arbitrary = [typeof<Generators>] }
    let configReplay = { FsCheckConfig.defaultConfig with maxTest = 10000 ; replay = Some <| (1940624926, 296296394) }

    [<Tests>]
    let eventMonitorTests =
        testList "EventMonitor tests" [
            testCase "construct with empty list" <| fun () ->
                let eventMon = new EventMonitor(100, [])

                Expect.equal 100 eventMon.eventId "Expected eventId to be 100"

                let (status, count) = eventMon.GetStatus
                Expect.equal count 0 "Expected count of 0"
                Expect.equal status MonitorStatus.Ok "Expected Status Ok"

                eventMon.Increment
                let (status, count) = eventMon.GetStatus
                Expect.equal count 1 "Expected count of 1"
                Expect.equal status MonitorStatus.Ok "Expected Status Ok"
                

            testCase "value cap" <| fun () ->
                let eventMon = new EventMonitor(1, [limitbreak.createValueCap 100])
                
                let (status, count) = eventMon.GetStatus
                Expect.equal count 0 "Expected count of 0"
                Expect.equal status MonitorStatus.Ok "Expected Status Ok"

                for i in 1 .. 99 do
                    eventMon.Increment
                let (status, count) = eventMon.GetStatus
                Expect.equal count 99 "Expected count of 99"
                Expect.equal status MonitorStatus.Ok "Expected Status Ok"

                eventMon.Increment
                let (status, count) = eventMon.GetStatus
                Expect.equal count 100 "Expected count of 100"
                Expect.equal status MonitorStatus.Limited "Expected Status Limited"

                eventMon.Increment
                let (status, count) = eventMon.GetStatus
                Expect.equal count 100 "Expected count of 100"
                Expect.equal status MonitorStatus.Limited "Expected Status Limited"

            testCase "threshold" <| fun () ->
                let eventMon = new EventMonitor(1, [limitbreak.createThreshold 5])
                
                let (status, count) = eventMon.GetStatus
                Expect.equal count 0 "Expected count of 0"
                Expect.equal status MonitorStatus.Ok "Expected Status Ok"

                for i in 1 .. 4 do
                    eventMon.Increment
                let (status, count) = eventMon.GetStatus
                Expect.equal count 4 "Expected count of 4"
                Expect.equal status MonitorStatus.Ok "Expected Status Ok"

                eventMon.Increment
                let (status, count) = eventMon.GetStatus
                Expect.equal count 5 "Expected count of 5"
                Expect.equal status MonitorStatus.Limited "Expected Status Limited"

                eventMon.Increment
                let (status, count) = eventMon.GetStatus
                Expect.equal count 6 "Expected count of 6"
                Expect.equal status MonitorStatus.Limited "Expected Status Limited"

            testCase "decay rate" <| fun () ->
                let eventMon = new EventMonitor(1, [limitbreak.createDecayRate 5.0])
                
                let (status, count) = eventMon.GetStatus
                Expect.equal count 0 "Expected count of 0"
                Expect.equal status MonitorStatus.Ok "Expected Status Ok"

                for i in 1 .. 20 do
                    eventMon.Increment
                let (status, count) = eventMon.GetStatus
                Expect.equal count 20 "Expected count of 20"
                Expect.equal status MonitorStatus.Ok "Expected Status Ok"

                Thread.Sleep 2000

                let (status, count) = eventMon.GetStatus
                Expect.equal count 10 "Expected count of 10"
                Expect.equal status MonitorStatus.Ok "Expected Status Ok"

                Thread.Sleep 1000

                let (status, count) = eventMon.GetStatus
                Expect.equal count 5 "Expected count of 5"
                Expect.equal status MonitorStatus.Ok "Expected Status Ok"

            testCase "leaky bucket" <| fun () ->
                let eventMon = new EventMonitor(1, [limitbreak.createDecayRate 5.5; limitbreak.createValueCap 100; limitbreak.createThreshold 90])
                    
                let (status, count) = eventMon.GetStatus
                Expect.equal count 0 "Expected count of 0"
                Expect.equal status MonitorStatus.Ok "Expected Status Ok"

                for i in 1 .. 150 do
                    eventMon.Increment
                let (status, count) = eventMon.GetStatus
                Expect.equal count 100 "Expected count of 100"
                Expect.equal status MonitorStatus.Limited "Expected Status Limited"

                Thread.Sleep 2000
                let (status, count) = eventMon.GetStatus
                Expect.equal count 89 "Expected count of 89"
                Expect.equal status MonitorStatus.Ok "Expected Status Ok"

                eventMon.Increment
                let (status, count) = eventMon.GetStatus
                Expect.equal count 90 "Expected count of 90"
                Expect.equal status MonitorStatus.Limited "Expected Status Limited"
        ]

