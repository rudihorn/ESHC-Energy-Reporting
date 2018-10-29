namespace EnergyReporting

open System
open EnergyReporting.Database
open Helpers.MeterData

type AdminList = {
    auth : MasterAuth;
    flatData : flatData list;
}

type FlatMeterStatus = 
    | UnreportedMeters of Meter list
    | LastReported of DateTime

type FlatWarnLevel = 
    | Ok
    | Warning of string
    | Alert of string
    | StepIn

type FlatEmail = {
    email : string;
    subject : string;
    body : string;
}

type FlatStatus = {
    flat : string;
    meterStatus : FlatMeterStatus;
    lastReminderSent : DateTime;
    emails : FlatEmail list;
    warnLevel : FlatWarnLevel;
}

type ReportViewModel = FlatStatus list