namespace EnergyReporting.Controllers

open System
open System.Linq
open Microsoft.AspNetCore.Mvc
open Microsoft.Extensions.Configuration
open EnergyReporting
open EnergyReporting.Helpers
open EnergyReporting.Database

type ReportingController (config, energy) =
    inherit Controller()

    member val Configuration : IConfiguration = config with get, set
    member val EnergyDatabase : EnergyDatabase = energy with get, set

    member private this.ReportEnergyViewModel flat = 
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
                let lastDate = Null.map_def (fun _ -> r.date.ToString("yyyy-MM-dd")) "" r 
                let lastValue = Null.map_def (fun _ -> r.value.ToString()) "<no value>" r
                EnergyMeterViewModel (Serial = m.serial, LastDate = lastDate, LastValue = lastValue)
            )

        let date = DateTime.Today.Subtract(TimeSpan.FromDays(3.0))
        let vm = 
            ReportEnergyViewModel (Flat = flat, EnergyMeters = Array.ofSeq mrs)
        vm

    member this.Index () = 
        "Reporting"

    member this.AuthAny uid key flat f =
        match Authentication.authAny this.EnergyDatabase uid key flat with
        | None -> this.View("AccessDenied") :> ActionResult
        | Some auth -> f auth

    member this.AdminList uid key =
        let admin = Authentication.authMaster this.EnergyDatabase uid key 
        match admin with
        | None -> this.View("AccessDenied")
        | Some admin -> 
            let flatData = MeterData.getFlatsData this.EnergyDatabase
            this.View({
                auth = admin;
                flatData = flatData;
                policy = ConfigHelper.policyConfig this.Configuration})


    member this.Report (uid, key, flat : string) =
        this.AuthAny uid key flat (fun auth -> 
            let model = this.ReportEnergyViewModel flat
            this.View(model) :> ActionResult
        )

    [<HttpPost>]
    member this.Report (uid, key, flat : string, model: ReportEnergyViewModel) =
        this.AuthAny uid key flat (this.PostReport flat model)

    member private this.PostReport flat model auth : ActionResult =
        let meterValues = 
            model.EnergyMeters
            |> Seq.mapi (fun i m -> (i, m))
            |> Seq.choose (fun (i,m) -> m.NewValueOpt() |> Option.map (fun v -> (i, m, v)))

        let actualMeters = 
            meterValues
            |> Seq.map (fun (i,m,v) -> 
                let m' = this.EnergyDatabase.Meters.FirstOrDefault(fun m' -> m'.flat = flat && m'.serial = m.Serial)
                let r = m' |> Null.map (fun m ->
                    this.EnergyDatabase.MeterReadings
                        .OrderByDescending(fun r -> r.date)
                        .FirstOrDefault(fun r -> r.meter_id = m.meter_id)
                )
                (i, m, m', r, v)
            )
            |> List.ofSeq

        actualMeters |> List.iter (fun (i,m,m',r,v) ->
            match m' with
            | null ->
                let message = sprintf "The meter '%s' does not exist or the user is not authorized." m.Serial
                this.ModelState.AddModelError("", message)
            | _ -> 
                let meterType = this.EnergyDatabase.MeterTypes.Find(m'.meter_type)

                let days = Math.Max(0.5, DateTime.Today.Subtract(r.date).TotalDays)
                let difference = (v - r.value + m'.reset_value) % m'.reset_value
                let quota = float difference / days

                if quota > m'.MeterType.daily_quota then
                    this.ModelState.AddModelError(sprintf "EnergyMeters[%d].NewValue" i, "This value doesn't seem right. If you are sure it is correct please send us an email with the meter picture.")
        )

        if this.ModelState.IsValid then 
            actualMeters
            |> List.iter (fun (i,m,m',r,v) -> 
                let entry = MeterReading (value = v, date = DateTime.Today, meter_id = m'.meter_id)
                this.EnergyDatabase.MeterReadings.Add(entry) |> ignore
            )
            this.EnergyDatabase.SaveChanges() |> ignore
            let model = this.ReportEnergyViewModel flat

            match auth.is_master with
            | true -> 
                let route = dict [
                    ("uid", box auth.uid);
                    ("key", box auth.key);
                    ]
                this.RedirectToAction("AdminList", null, route, "flat-" + flat.Replace('/', '-')) :> ActionResult
            | false -> 
                this.ModelState.Clear()
                this.View(model) :> ActionResult
        else 
            this.View(model) :> ActionResult
