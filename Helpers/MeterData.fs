module EnergyReporting.Helpers.MeterData

    open EnergyReporting.Database
    open System.Security.Cryptography
    open System
    open System.Linq
    open System.Text.RegularExpressions

    type DB = EnergyDatabase
    type State = 
        | Unreported of Meter list
        | LastReported of DateTime

    type FlatData = {
        flat : string;
        meters : (Meter * MeterReading) list;
        state : State;
    }

    (* generates a random authentication key for the user *)
    let genAuthKey () =
        let rng = RandomNumberGenerator.Create ()
        let set = List.concat [['0'..'9'];['a'..'z'];['A'..'Z']] |> Array.ofList
        let nextChar () = 
            let bytes : byte [] = Array.create 4 (byte 0)
            rng.GetBytes(bytes)
            let i = BitConverter.ToUInt32(bytes, 0)
            let len = uint32 set.Length
            set.[i % len |> int]
        let key = [|1..32|] |> Array.map (fun _ -> nextChar ()) |> (fun r -> new String(r))
        key


    let getFlatsData (db:DB) : FlatData list = 
        let flats = 
            db.Meters
                .Where(fun m -> m.disabled = false)
                .Select(fun m -> m, m.Readings.OrderByDescending(fun r -> r.date).FirstOrDefault())
                .GroupBy(fun (m,_) -> m.flat)
            |> Seq.toList

        let report = flats |> List.map (fun f ->
            let flat = f.Key
            let meters = f |> Seq.toList

            let unreported = 
                f
                |> Seq.filter (fun (_,r) -> r = null)
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