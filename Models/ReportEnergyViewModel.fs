namespace EnergyReporting

open System

type EnergyMeterViewModel () = 
    member val Serial : string = "" with get, set
    member val LastValue : string = "" with get, set
    member val LastDate : string = "" with get, set
    member val NewValue : Nullable<int> = Unchecked.defaultof<Nullable<int>> with get, set

    member this.NewValueOpt () = if this.NewValue.HasValue then Some this.NewValue.Value else None


type RouteTest = {
    building : int;
    flat : int;
}

type ReportEnergyViewModel () =

    member val Flat : string = "" with get, set

    member val EnergyMeters : EnergyMeterViewModel array = [| |] with get, set
