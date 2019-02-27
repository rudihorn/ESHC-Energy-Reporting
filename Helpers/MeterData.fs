module EnergyReporting.Helpers.MeterData

    open EnergyReporting.Database
    open System
    open System.Linq
    open MathNet.Numerics
    open MathNet.Numerics


    type Database = EnergyDatabase
    type State = 
        | Unreported of Meter list
        | LastReported of DateTime

    type FlatData = 
        {
            flat : string;
            meters : (Meter * MeterReading) list;
            state : State;
        }

        member this.Days() = 
            match this.state with
            | LastReported d -> DateTime.Today.Subtract(d).TotalDays |> int
            | _ -> invalidArg "state" "Expected a last reported state"

    let getFlatsData (db:Database) : FlatData list = 
        let flats = 
            db.Meters
                .Where(fun m -> not m.disabled)
                .Select(fun m -> (m, m.Readings.OrderByDescending(fun r -> r.date).FirstOrDefault()))
                .GroupBy(fun (m,_) -> m.flat)
            |> Seq.toList

        let report = flats |> List.map (fun f ->
            let flat = f.Key
            let meters = f |> Seq.toList

            let unreported = 
                f
                |> Seq.filter (fun (_,r) -> isNull r)
                |> Seq.map (fun (m,_) -> m)
                |> Seq.toList

            if List.isEmpty unreported then
                (* all meters have reported values*)
                let lastReported = 
                    f 
                    |> Seq.sortBy (fun (_,r) -> r.date)
                    |> Seq.head
                let lastReportedDay = lastReported |> (fun (_,r) -> r.date)

                { flat = flat; meters = meters; state = LastReported lastReportedDay }
            else
                { flat = flat; meters = meters; state = Unreported unreported }
        )

        report

    type DateRange = {
        from: DateTime;
        until: DateTime;
    }

    type MeterData = {
        meter: Meter;
        date: DateTime;
        data: (int * float option) list
    }

    let rec addUntilAbove v by until =
        if v >= until then
            v
        else
            addUntilAbove (v + by) by until

    let interpolate (db:Database) meterId range =
        let meter = db.Meters.First(fun m -> m.meter_id = meterId)
        let readings = db.MeterReadings.Where(fun m -> m.meter_id = meter.meter_id).OrderBy(fun x -> x.date) |> List.ofSeq
        let readings =
            match range with 
            | None -> readings
            | Some range ->
                let ind1 = 
                    try 
                        readings |> List.findIndex (fun x -> x.date >= range.from) 
                    with 
                    | _ -> 0 
                let ind1 = Math.Max(0, ind1 - 1)
                let len = List.length readings
                let ind2 = 
                    try
                        readings |> List.findIndexBack (fun x -> x.date <= range.until) |> (+) 1 
                    with 
                    | _ -> len
                readings |> Seq.take ind2 |> Seq.skip ind1 |> List.ofSeq
        let fst = List.head readings
        let last = List.last readings
        let lastDay = last.date.Subtract(fst.date).TotalDays |> int
        let readings = 
            readings 
            |> List.map (fun x -> (x.date.Subtract(fst.date).TotalDays, x.value |> float))
        let last = ref 0.0 
        (* fix meter overflow *)
        let readings = readings |> List.map (fun (x,y) ->
            last := addUntilAbove y (float meter.reset_value) !last
            (x, !last)
        )
        let xs, ys = List.unzip readings 
        let interp = Interpolation.CubicSpline.InterpolateNatural(xs, ys)
        let values = [0..lastDay] |> List.map (fun day -> (day, interp.Interpolate(day |> float) |> Some))
        { meter = meter; date = fst.date; data = values }

    let derive md = 
        let xs, ys = List.unzip md.data 
        let old = ref (List.head ys)
        let ys = List.tail ys |> List.map (fun v -> 
            let d = Option.map2 (-) v !old
            old := v
            d)
        let xs = List.length ys |> fun v -> List.take v xs
        { md with data = List.zip xs ys }

    let realign (date : DateTime) (md : MeterData) =
        let xs, ys = List.unzip md.data
        let by = md.date.Subtract(date).TotalDays |> int
        if by >= 0 then
            let xs = List.map (fun v -> v + by) xs
            let xs = List.append [0..by-1] xs 
            let ys = List.init by (fun _ -> None) |> (fun v -> List.append v ys)
            { md with data = List.zip xs ys }
        else
            let by = -by
            { md with data = List.skip by md.data; date = date }


