namespace EnergyReporting.Database 

open System.Collections.Generic
open Microsoft.EntityFrameworkCore.Metadata.Internal
open System
open System.IO
open System.Linq

open System.ComponentModel.DataAnnotations;
open System.ComponentModel.DataAnnotations.Schema
open Microsoft.EntityFrameworkCore
open Microsoft.Extensions.DependencyInjection
open System.Collections


[<AllowNullLiteral>]
[<Table("meters")>]
type public Meter() =

    [<Key()>]
    member val meter_id : int64 = 0L with get,set 

    member val disabled : bool = false with get,set 
    member val mpan : string = "" with get,set 
    member val serial : string = "" with get,set 
    member val flat : string = "" with get,set 
    member val reset_value : int = 10000 with get,set

    member val meter_type : int = 0 with get,set

    [<DefaultValue>] val mutable readings : ICollection<MeterReading>
    [<ForeignKey("meter_id")>]
    member x.Readings with get () = x.readings and set v = x.readings <- v

    [<DefaultValue>] val mutable meterType : MeterType
    [<ForeignKey("meter_type")>]
    member x.MeterType with get () = x.meterType and set v = x.meterType <- v

and [<Table("meter_types")>][<AllowNullLiteral>] public MeterType() =
    [<Key()>]
    member val type_id : int = 0 with get,set

    member val name : string = "" with get,set

    member val daily_quota : float = 30.0 with get,set

    [<DefaultValue>] val mutable meters : ICollection<Meter>
    [<InverseProperty("MeterType")>]
    member x.Meters with get () = x.meters and set v = x.meters <- v


and [<Table("meter_readings")>][<AllowNullLiteral>] public MeterReading() =
    [<Key()>]
    member val reading_id : int64 = 0L with get,set

    member val value : int = 0 with get,set

    member val date : DateTime = DateTime.Today with get,set

    [<ForeignKey("meter")>]
    member val meter_id : int64 = 0L with get,set
    [<DefaultValue>] val mutable meter : Meter
    member x.Meter with get () = x.meter and set v = x.meter <- v

and [<Table("reminders")>][<AllowNullLiteral>] public Reminder() =
    [<Key()>]
    member val flat : string = "" with get,set

    member val lastSent : DateTime = DateTime.Today with get,set

    member val since : DateTime = DateTime.Today with get,set

and [<Table("user_auths")>][<AllowNullLiteral>] public UserAuth() =
    [<Key()>]
    member val user : string = "" with get,set

    member val key : string = "" with get,set

    member val flat : string = "" with get,set

    member val name : string = "" with get,set


and [<Table("master_auths")>][<AllowNullLiteral>] public MasterAuth() = 
    [<Key()>]
    member val user : string = "" with get,set

    member val name : string = "" with get,set

    member val key : string = "" with get,set


[<AllowNullLiteral>]
type public EnergyDatabase(options) = 
    inherit DbContext(options)
    
    [<DefaultValue>] val mutable private meters : DbSet<Meter>
    member x.Meters with get () = x.meters and set v = x.meters <- v

    [<DefaultValue>] val mutable private meterReadings : DbSet<MeterReading>
    member x.MeterReadings with get () = x.meterReadings and set v = x.meterReadings <- v

    [<DefaultValue>] val mutable private meterTypes : DbSet<MeterType>
    member x.MeterTypes with get () = x.meterTypes and set v = x.meterTypes <- v

    [<DefaultValue>] val mutable private reminders : DbSet<Reminder>
    member x.Reminders with get () = x.reminders and set v = x.reminders <- v

    [<DefaultValue>] val mutable private userAuths : DbSet<UserAuth>
    member x.UserAuths with get () = x.userAuths and set v = x.userAuths <- v

    [<DefaultValue>] val mutable private masterAuths : DbSet<MasterAuth>
    member public x.MasterAuths with get () = x.masterAuths and set v = x.masterAuths <- v