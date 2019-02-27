module EnergyReporting.ConfigHelper

open Microsoft.Extensions.Configuration

type PolicyConfig = {
    readingDays : int;
    reminderDays : int;
}

type EmailConfig = {
    from : string;
    urlBase : string;
}

let policyConfig (config : IConfiguration) =
    {
        readingDays = int config.["Policy:ReadingDays"];
        reminderDays = int config.["Policy:ReminderDays"];
    }

let emailConfig (config : IConfiguration) =
    {
        from = config.["Email:From"];
        urlBase = config.["Email:UrlBase"]
    }