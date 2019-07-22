namespace limitbreak

open System
open System.Threading

type MonitorStatus =
| Ok
| Limited

/// Encapsulates a thread safe counter associated with an eventId.
type EventCounter(eventId: int) =
    let observed = ref 0;
    let limit = System.Int32.MaxValue - 10_000;

    member this.eventId = eventId

    member this.Increment =
        let newValue = Interlocked.Increment observed
        if newValue > limit then
            Interlocked.CompareExchange (observed, limit, newValue) |> ignore

    member this.DecrementBy (amount: int) =
        let rec tryDecrement decrAmount =
            let currentObserved = observed.contents
            let newValue = Math.Min(Math.Max(0, currentObserved - decrAmount), limit)
            match Interlocked.CompareExchange (observed, newValue, currentObserved) with
            | returnValue when returnValue = currentObserved -> newValue
            | _ -> tryDecrement decrAmount
        tryDecrement amount |> ignore

    member this.Reset value =
        Interlocked.Exchange (observed, Math.Max(0, Math.Min(value, limit))) |> ignore

    member this.Observed =
        observed.contents

/// Encapsulates an EventCounter and a set of "event processor" functions that operate on the counter 
/// in order. Each process has the opportunity to read the counter and modify it and dtermine if the 
/// event should be "limited" or not.
type EventMonitor (eventId: int, eventProcessors: (EventCounter -> MonitorStatus -> MonitorStatus) list) =
    let counter = new EventCounter(eventId)

    member this.processors = eventProcessors

    member this.eventId = counter.eventId

    member this.GetStatus =
        let status = 
            List.fold (fun (status: MonitorStatus) proc -> proc counter status) MonitorStatus.Ok this.processors
        let observed = counter.Observed
        status, observed

    member this.Increment =
        counter.Increment

    member this.Reset =
        counter.Reset

module limitbreak = 
  
    /// Returns a MonitorStatus of limited when counter equals or exceeds a given value.
    let createThreshold limit =
        fun (eventCounter: EventCounter) (monitorStatus: MonitorStatus) ->
            if eventCounter.Observed >= limit then
                MonitorStatus.Limited
            else
                monitorStatus

    // Decrements the event counter at a given rate (based on the last time the value was decremented)
    // Does not change the monitor's status.
    let createDecayRate (decayPerSecond: float) =
        let decayPerMillis = decayPerSecond / 1000.0
        let syncLock = Object
        let mutable lastRun = DateTime.Now
        fun (eventCounter: EventCounter) (monitorStatus: MonitorStatus) ->
            try
                Monitor.Enter syncLock

                let now = DateTime.Now
                let lastRunVal = lastRun
                let currentTime = now

                let elapsedMillis = (currentTime - lastRunVal).TotalMilliseconds
                let decayAmount = (int (Math.Floor(elapsedMillis * decayPerMillis)))
                if decayAmount > 0 then
                    lastRun <- now
                    eventCounter.DecrementBy decayAmount |> ignore
            finally
                Monitor.Exit syncLock
          
            // this function does not change the monitor status. Only applies a decrement
            monitorStatus
    
    // Applies a value cap (maximum value) on the event counter.
    // Returns a MonitorStatus of limited once the event counter reches the cap value.
    let createValueCap cap =
        fun (eventCounter: EventCounter) (monitorStatus: MonitorStatus) ->
            if eventCounter.Observed = cap then
                MonitorStatus.Limited
            elif eventCounter.Observed > cap then
                eventCounter.Reset cap
                MonitorStatus.Limited
            else
                monitorStatus
