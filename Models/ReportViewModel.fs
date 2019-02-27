namespace EnergyReporting

open System
open EnergyReporting.Database
open Helpers.MeterData
open Helpers.EmailSender
open Helpers.Authentication

type AdminList = {
    auth : UserAuth;
    flatData : FlatData list;
    policy : ConfigHelper.PolicyConfig;
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

    let warnLevelClass t =
        match t with
        | Ok -> ""
        | Warning _ ->  "table-info"
        | Alert _ -> "table-warning"
        | StepIn -> "table-danger"

    let warnLevelMsg t =
        match t with
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
