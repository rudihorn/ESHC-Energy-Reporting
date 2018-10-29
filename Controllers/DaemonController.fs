namespace EnergyReporting.Controllers

open System
(* open System.DirectoryServices.Protocols *)
open System.Linq
open System.Web
open Microsoft.AspNetCore.Mvc
open Microsoft.Extensions.Configuration
open EnergyReporting
open EnergyReporting.Helpers
open EnergyReporting.Helpers.EmailTemplate
open EnergyReporting.Database
open EnergyReporting.Helpers.Ldap
open EnergyReporting.Helpers.MeterData
open EnergyReporting.Helpers
open EnergyReporting.Helpers.EmailSender;


type DaemonController (config, energy) =
    inherit Controller()
    member val Configuration : IConfiguration = config with get, set
    member val EnergyDatabase : EnergyDatabase = energy with get,set


    (*
    member private this.SendEmails emails = 
        ()
    *)

    member private this.SerializeFlat (bld, flat, room) = 
        sprintf "%02d/%02d" bld flat


    member this.TestEmail email = 
        let client = EmailSender.client this.Configuration
        let mail = {
            recipient = email;
            subject = "Test Email";
            body = "Test Email";
        }
        EmailSender.send client mail
        "Sent!"

    member this.TestReport () =
        this.View("Report", [{
            flat = "34/99Z";
            meterStatus = UnreportedMeters [];
            warnLevel = sprintf "Unreported Meters" |> Alert;
            emails = [{
                email = "test@example.com";
                subject = "Energy email";
                body = "Lorem ipsum dolor sit amet, consectetur adipisicing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.";
            };
            {
                email = "test@example.com";
                subject = "Energy email";
                body = "Lorem ipsum dolor sit amet, consectetur adipisicing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.";
            }];
            lastReminderSent = DateTime.Today;
        }])

    member this.Index (send_email : bool) =
        let get_reminder flat = 
            this.EnergyDatabase.Reminders
                .FirstOrDefault(fun r -> r.flat = flat)

        let update_reminder flat reminder = 
            let mutable reminder = reminder
            if reminder = null then 
                reminder <- new Reminder(flat = flat)
                this.EnergyDatabase.Reminders.Add(reminder) |> ignore
            else    
                reminder.lastSent <- DateTime.Today


        let get_user_auth u flat = 
            let mutable user = 
                this.EnergyDatabase.UserAuths
                    .FirstOrDefault(fun u -> u.user = u.user)
            if user = null then
                user <- new UserAuth (
                    user = u.user, 
                    flat = flat, 
                    key = MeterData.genAuthKey ()
                )
                this.EnergyDatabase.UserAuths.Add(user) |> ignore
            else
                user.flat <- flat
            user

        let emailConfig = ConfigHelper.emailConfig this.Configuration
        let policyConfig = ConfigHelper.policyConfig this.Configuration


        let get_link (userauth : UserAuth) = 
            sprintf "%s/Reporting/Report?auth=%s&user=%s"
                emailConfig.urlBase
                (HttpUtility.UrlEncode userauth.key)
                (HttpUtility.UrlEncode userauth.user)

        let meterData = MeterData.getFlatsData this.EnergyDatabase

        let users = Ldap.users this.Configuration 
        let users = 
            users 
            |> List.choose (fun u -> Ldap.normalizeFlat u.room |> Option.map (fun f -> (u,this.SerializeFlat f)))

        let flatUsers flat = users |> List.filter (fun (_,sf) -> flat = sf)

        let report = meterData |> List.map (fun data ->
            let flat = data.flat
            let meters = data.meters
            let users = flatUsers flat
            match data.state with 
            | LastReported lastReportedDay ->
                let emails = users |> List.map (fun (u,_) -> 
                    let auth = get_user_auth u flat
                    let link = get_link auth
                        
                    let emailbody = EmailTemplate.build_string EmailTemplate.neededMetersEmail {
                        config = emailConfig;
                        meters = meters;
                        link = link;
                        name = u.name;
                    }

                    {
                        email = u.email;
                        body = emailbody;
                        subject = "ESHC Meter Readings"
                    }
                )

                let emails = 
                    if DateTime.Today.Subtract(lastReportedDay).TotalDays >= float policyConfig.readingDays then  
                        let reminder = get_reminder flat
                        if reminder <> null && DateTime.Today.Subtract(reminder.lastSent).TotalDays >= float policyConfig.reminderDays then
                            update_reminder flat reminder
                            emails
                        else 
                            []
                    else
                        []

                (* there are meter with no values *)
                let state : FlatStatus = {
                    flat = flat;
                    meterStatus = EnergyReporting.LastReported lastReportedDay;
                    warnLevel = Alert "Needs to be reported";
                    emails = emails;
                    lastReminderSent = DateTime.Today;
                }

                state
            | Unreported unreported ->
                let emails = users |> List.map (fun (u,_) -> 
                    let auth = get_user_auth u flat
                    let link = get_link auth
                        
                    let emailbody = EmailTemplate.build_string EmailTemplate.unreportedMetersEmail {
                        config = emailConfig;
                        unreported = unreported;
                        link = link;
                        name = u.name;
                    }

                    {
                        email = u.email;
                        body = emailbody;
                        subject = "ESHC Meter Readings"
                    }
                )

                let reminder = get_reminder flat
                let emails = 
                    if reminder <> null && DateTime.Today.Subtract(reminder.lastSent).TotalDays >= float policyConfig.reminderDays then
                        update_reminder flat reminder
                        emails
                    else 
                        []

                (* there are meter with no values *)
                let state = {
                    flat = flat;
                    meterStatus = UnreportedMeters unreported;
                    warnLevel = unreported |> List.map (fun m -> m.serial) |> sprintf "Unreported Meters %A" |> Alert;
                    emails = emails;
                    lastReminderSent = DateTime.Today;
                }

                state
        )

        this.EnergyDatabase.SaveChanges() |> ignore

        sprintf "%A" report
