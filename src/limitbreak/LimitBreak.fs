namespace limitbreak

open System
open System.Threading

type MonitorStatus =
    | Ok = 0
    | Limited = 1

type IEventCounter =
    interface
    abstract member EventId:unit -> int
    abstract member Increment:unit -> unit
    abstract member DecrementBy:int -> unit
    abstract member Reset:int -> unit
    abstract member Observed:unit -> int
end

type EventProcessor = delegate of IEventCounter * MonitorStatus -> MonitorStatus

/// Encapsulates a thread safe counter associated with an eventId.
type EventCounter(eventId: int, initialValue: int) =

    let observed = ref initialValue;
    let limit = System.Int32.MaxValue - 10_000;

    new(eventId: int) = EventCounter(eventId, 0)

    interface IEventCounter with
        member this.EventId() = eventId

        member this.Increment() =
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

        member this.Observed() =
            observed.contents

/// Encapsulates an EventCounter and a set of "event processor" functions that operate on the counter 
/// in order. Each process has the opportunity to read the counter and modify it and dtermine if the 
/// event should be "limited" or not.
type EventMonitor (counter: IEventCounter, eventProcessors: seq<EventProcessor>) =
    
    new(eventId: int, eventProcessors: seq<EventProcessor>) = 
        EventMonitor(new EventCounter(eventId) :> IEventCounter, eventProcessors)
    
    // convert delegates to function values
    member private this.processors = 
        eventProcessors |> Seq.map (fun f -> f.Invoke)

    member this.EventId = 
        counter.EventId()

    member this.GetStatus() =
        let status = 
            this.processors |> Seq.fold (fun (status: MonitorStatus) proc -> proc(counter, status)) MonitorStatus.Ok
        let observed = counter.Observed()
        status, observed

    member this.Increment() =
        counter.Increment()

    member this.Reset() =
        counter.Reset(0)

module limitbreak = 
  
    /// Returns a MonitorStatus of limited when counter equals or exceeds a given value.
    let createThreshold limit =
        let proc = fun (eventCounter: IEventCounter) (monitorStatus: MonitorStatus) ->
            if eventCounter.Observed() >= limit then
                MonitorStatus.Limited
            else
                monitorStatus
        new EventProcessor(proc)

    // Decrements the event counter at a given rate (based on the last time the value was decremented)
    // Does not change the monitor's status.
    let createDecayRate (decayPerSecond: float) =
        let decayPerMillis = decayPerSecond / 1000.0
        let syncLock = Object
        let mutable lastRun = DateTime.Now
        let proc = fun (eventCounter: IEventCounter) (monitorStatus: MonitorStatus) ->
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
        new EventProcessor(proc)
    
    // Applies a value cap (maximum value) on the event counter.
    // Does not change monitor status
    let createValueCap cap =
        let proc = fun (eventCounter: IEventCounter) (monitorStatus: MonitorStatus) ->
            if eventCounter.Observed() > cap then
                eventCounter.Reset cap
            monitorStatus
        new EventProcessor(proc)
