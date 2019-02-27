namespace EnergyReporting.Controllers

open System
open System.Linq
open System.Web
open Microsoft.AspNetCore.Mvc
open Microsoft.Extensions.Configuration
open EnergyReporting
open EnergyReporting.Database
open EnergyReporting.Helpers
open EnergyReporting.Helpers.Ldap
open EnergyReporting.Helpers.EmailTemplate
open EnergyReporting.Helpers.MeterData
open EnergyReporting.Helpers.EmailSender;


type DaemonController (config, energy) =
    inherit Controller()

    member val Configuration : IConfiguration = config with get, set
    member val EnergyDatabase : EnergyDatabase = energy with get,set

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
            warnLevel = sprintf "Unreported Meters" |> FlatWarnLevel.Alert;
            emails = [{
                recipient = "test@example.com";
                subject = "Energy email";
                body = "Lorem ipsum dolor sit amet, consectetur adipisicing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.";
            };
            {
                recipient = "test@example.com";
                subject = "Energy email";
                body = "Lorem ipsum dolor sit amet, consectetur adipisicing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.";
            }];
            lastReminderSent = DateTime.Today;
        }])

    member this.Index (send_email : bool) =
        let today = DateTime.Today

        let get_reminder flat = 
            this.EnergyDatabase.Reminders
                .FirstOrDefault(fun r -> r.flat = flat)
            |> Null.option

        let update_reminder flat (reminder : Reminder option) = 
            match reminder with 
            | None -> 
                let reminder = new Reminder(flat = flat, lastSent = today)
                this.EnergyDatabase.Reminders.Add(reminder) |> ignore
            | Some r -> r.lastSent <- today

        let emailConfig = ConfigHelper.emailConfig this.Configuration
        let emailClient = EmailSender.client this.Configuration
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
            |> List.choose (fun u -> Ldap.normalizeFlat u.room |> Option.map (fun f -> (u,Ldap.serializeFlat f)))

        let flatUsers flat = users |> List.filter (fun (_,sf) -> flat = sf)

        let email recipient body = {
            recipient = recipient;
            body = body;
            subject = "ESHC Meter Readings"
        }

        let report = meterData |> List.map (fun data ->
            let flat = data.flat
            let meters = data.meters
            let users = flatUsers flat
            match data.state with 
            | LastReported lastReportedDay ->
                let emails = users |> List.map (fun (u,_) -> 
                    let auth = Authentication.getUserAuth this.EnergyDatabase u.user flat
                    let link = get_link auth
                        
                    let body = FormatHelper.buildString EmailTemplate.neededMetersEmail {
                        config = emailConfig;
                        meters = meters;
                        link = link;
                        name = u.name;
                    }

                    email u.email body
                )
                let requiresReporting = today.Subtract(lastReportedDay).TotalDays >= float policyConfig.readingDays in
                let reminder = if requiresReporting then get_reminder flat else None
                let daysSinceReport = reminder |> Option.map (fun r -> today.Subtract(r.lastSent).TotalDays)
                let requiresReminding = requiresReporting && daysSinceReport |> Option.map (fun r -> r >= float policyConfig.reminderDays) |> Option.defaultValue true
                if requiresReminding then update_reminder flat reminder
                let emails = if requiresReminding then emails else []
                (* there are meter with no values *)
                let state : FlatStatus = {
                    flat = flat;
                    meterStatus = EnergyReporting.LastReported lastReportedDay;
                    warnLevel = FlatWarnLevel.Alert "Needs to be reported";
                    emails = emails;
                    lastReminderSent = today;
                }

                state
            | Unreported unreported ->
                let emails = users |> List.map (fun (u,_) -> 
                    let auth = Authentication.getUserAuth this.EnergyDatabase u.user flat
                    let link = get_link auth
                        
                    let body = FormatHelper.buildString EmailTemplate.unreportedMetersEmail {
                        config = emailConfig;
                        unreported = unreported;
                        link = link;
                        name = u.name;
                    }

                    email u.email body
                )

                let remind = 
                    get_reminder flat
                    |> Option.map (fun r -> 
                        DateTime.Today.Subtract(r.lastSent).TotalDays >= float policyConfig.reminderDays
                    )
                    |> Option.defaultValue true
                let emails =  if remind then emails else []

                (* there are meters with no values *)
                let state = {
                    flat = flat;
                    meterStatus = UnreportedMeters unreported;
                    warnLevel = unreported |> List.map (fun m -> m.serial) |> sprintf "Unreported Meters %A" |> FlatWarnLevel.Alert;
                    emails = emails;
                    lastReminderSent = DateTime.Today;
                }

                state
        )

        this.EnergyDatabase.SaveChanges() |> ignore

        (* send emails *)
        if send_email then
            report 
            |> List.iter (fun flat -> 
                flat.emails
                |> List.iter (fun email -> 
                    EmailSender.send emailClient email
                )
            )
        this.View("Report", report)
