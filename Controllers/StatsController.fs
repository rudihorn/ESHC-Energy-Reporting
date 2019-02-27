namespace EnergyReporting.Controllers

open System
open System.Linq
open Microsoft.AspNetCore.Mvc
open Microsoft.Extensions.Configuration
open EnergyReporting.Database
open EnergyReporting.Helpers.MeterData
open EnergyReporting.Helpers.FormatHelper


type StatsController (config, energy) =
    inherit Controller()
    member val Configuration : IConfiguration = config with get, set
    member val EnergyDatabase : EnergyDatabase = energy with get,set

    member this.Meter (meter : int64) =
        let md = interpolate this.EnergyDatabase meter None
        let xs, ys = List.unzip md.data
        let fmt f v = fprintf f "%a\n%a\n" (formatCommaList formatInt) xs (formatCommaList (formatSome formatFloat)) ys
        buildString fmt ()

    member private this.FormatHeader b v =
        fprintf b "flat, serial, %a\n" (formatCommaList formatInt) v

    member private this.FormatData b v =
        let _, ys = List.unzip v.data
        fprintf b "Flat %s, %s, %a\n" v.meter.flat v.meter.serial (formatCommaList (formatSome formatFloat)) ys

    member private this.FormatDataList b v =
        fprintf b "%a" (formatEmptyList this.FormatData) v

    member private this.ReturnMany data = 
        let first = data |> List.map (fun md -> md.date) |> List.min
        let data = data |> List.map (fun dat -> realign first dat)
        let maxLen = data |> List.map (fun md -> List.length md.data) |> List.max
        let head = [0..maxLen - 1]
        let fmt b () = fprintf b "%a%a" this.FormatHeader head this.FormatDataList data
        buildString fmt ()

    member this.AllMeters () =
        let meters = this.EnergyDatabase.Meters.Select(fun m -> m.meter_id) |> Seq.toList
        let meterData = meters |> List.map (fun m -> interpolate this.EnergyDatabase m None)
        let meterData = meterData |> List.map derive
        this.ReturnMany meterData

    member this.MetersOfType meterType =
        let meters = this.EnergyDatabase.Meters.Where(fun m -> m.meter_type = meterType).Select(fun m -> m.meter_id) |> Seq.toList
        let meterData = meters |> List.map (fun m -> interpolate this.EnergyDatabase m None)
        let meterData = meterData |> List.map derive
        this.ReturnMany meterData
