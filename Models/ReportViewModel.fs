namespace EnergyReporting

open System
open EnergyReporting.Database
open Helpers.MeterData
open Helpers.EmailSender

type AdminList = {
    auth : MasterAuth;
    flatData : FlatData list;
}

type FlatMeterStatus = 
    | UnreportedMeters of Meter list
    | LastReported of DateTime


module FlatWarnLevel =

    type T = 
        | Ok
        | Warning of string
        | Alert of string
        | StepIn

    let warnLevelClass =
        function
        | Ok -> ""
        | Warning _ ->  "table-info"
        | Alert _ -> "table-warning"
        | StepIn -> "table-danger"

    let warnLevelMsg =
        function
        | Warning str -> str
        | Alert str -> str
        | _ -> ""

type FlatStatus = {
    flat : string;
    meterStatus : FlatMeterStatus;
    lastReminderSent : DateTime;
    emails : Email list;
    warnLevel : FlatWarnLevel.T;
}

type ReportViewModel = FlatStatus list
