module EnergyReporting.Helpers.EmailSender

open System.Net.Mail
open System.Net
open Microsoft.Extensions.Configuration

type Client = {
    smtpClient : SmtpClient;
    from : string;
}

let client (config : IConfiguration) = 
    let host = config.["Email:Host"]
    let port = int config.["Email:Port"]
    let creds = new NetworkCredential(config.["Email:User"], config.["Email:Pass"])
    let client = new SmtpClient(host)
    client.Credentials <- creds
    client.EnableSsl <- true
    { smtpClient = client; from = config.["Email:From"] }

type Email = {
    subject : string;
    recipient : string;
    body : string;
}

let send client mail = 
    let message = new MailMessage(client.from, mail.recipient, mail.subject, mail.body)
    message.IsBodyHtml <- true
    client.smtpClient.Send(message)