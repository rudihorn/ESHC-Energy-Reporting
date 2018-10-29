namespace EnergyReporting.Controllers

open System
open System.Linq
open Microsoft.AspNetCore.Mvc
open Microsoft.Extensions.Configuration
open EnergyReporting
open EnergyReporting.Helpers
open EnergyReporting.Database

type ReportingController (energy) =
    inherit Controller()

    member val EnergyDatabase : EnergyDatabase = energy with get, set
    member val Configuration : IConfiguration = null with get, set

    member private this.reportEnergyViewModel flat = 
        let meters = query {
            for m in this.EnergyDatabase.Meters do
            where (m.flat = flat)
            select m 
        }

        let readings = 
            meters.Select(fun m -> m.Readings.OrderByDescending(fun r -> r.date).FirstOrDefault()).ToList()

        let mrs = 
            Seq.zip meters readings
            |> Seq.map (fun (m,r) -> 
                new EnergyMeterViewModel (
                    Serial = m.serial,
                    LastDate = (if r = null then "" else r.date.ToString("yyyy-MM-dd")),
                    LastValue = (if r = null then "<no value>" else r.value.ToString())
                )) 

        let date = DateTime.Today.Subtract(TimeSpan.FromDays(3.0))
        let vm = 
            new ReportEnergyViewModel (
                Flat = flat,
                EnergyMeters = Array.ofSeq mrs
            )
        vm



    member this.Index () = 
        "Reporting"


    member private this.AuthMaster uid key =    
        let ma = this.EnergyDatabase.MasterAuths.FirstOrDefault(fun ma -> ma.user = uid && ma.key = key)
        Null.option ma
        
    member this.AuthUser uid key flat =
        let auth = this.EnergyDatabase.UserAuths.FirstOrDefault(fun auth -> auth.user = uid && auth.key = key && auth.flat = flat)
        Null.option auth

    member this.AdminList uid key =
        let admin = this.AuthMaster uid key 
        match admin with
        | None -> this.View("AccessDenied")
        | Some admin -> 
            let flatData = MeterData.getFlatsData this.EnergyDatabase
            this.View({
                auth = admin;
                flatData = flatData;
            })


    member this.Report (uid, key, flat : string) =
        let admin = this.AuthMaster uid key 
        let model = this.reportEnergyViewModel flat
        this.View(model)

    [<HttpPost>]
    member this.Report (uid, key, flat : string, model: ReportEnergyViewModel) =
        let auth = 
            this.AuthMaster uid key 
            |> Option.map (fun auth -> Some auth.name)
            |> Option.defaultWith (fun () -> this.AuthUser uid key flat |> Option.map (fun auth -> auth.name)) 

        let meterValues = 
            model.EnergyMeters
            |> Seq.mapi (fun i m -> i,m)
            |> Seq.choose (fun (i,m) -> m.NewValueOpt() |> Option.map (fun v -> (i,m,v)))

        let actualMeters = 
            meterValues
            |> Seq.map (fun (i,m,v) -> 
                let m' = this.EnergyDatabase.Meters.FirstOrDefault(fun m' -> m'.flat = flat && m'.serial = m.Serial)
                let r = m' |> Null.map (fun m ->
                    this.EnergyDatabase.MeterReadings
                        .OrderByDescending(fun r -> r.date)
                        .FirstOrDefault(fun r -> r.meterId = m.meterId)
                )
                i,m, m', r, v
            )
            |> List.ofSeq

        actualMeters |> List.iter (fun (i,m,m',r,v) ->
            if m' = null then
                let message = sprintf "The meter '%s' does not exist or the user is not authorized." m.Serial
                this.ModelState.AddModelError("", message)

            if r <> null then
                let days = Math.Min(0.5, DateTime.Today.Subtract(r.date).TotalDays)
                let difference = (v - r.value + m'.reset_value) % m'.reset_value
                let quota = float difference / days

                if quota > m'.daily_quota then
                    this.ModelState.AddModelError(sprintf "EnergyMeters[%d].NewValue" i, "This value doesn't seem right. If you are sure it is correct please send us an email with the meter picture.")
        )

        if this.ModelState.IsValid then 
            actualMeters
            |> List.iter (fun (i,m,m',r,v) -> 
                let entry = new MeterReading (value = v, date = DateTime.Today, meterId = m'.meterId)
                this.EnergyDatabase.MeterReadings.Add(entry) |> ignore
            )
            this.EnergyDatabase.SaveChanges() |> ignore

        let model = this.reportEnergyViewModel flat

        this.View(model)
