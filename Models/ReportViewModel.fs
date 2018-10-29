namespace EnergyReporting

open System
open EnergyReporting.Database
open Helpers.MeterData
open Helpers.EmailSender

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

type FlatStatus = {
    flat : string;
    meterStatus : FlatMeterStatus;
    lastReminderSent : DateTime;
    emails : Email list;
    warnLevel : FlatWarnLevel;
}

type ReportViewModel = FlatStatus list