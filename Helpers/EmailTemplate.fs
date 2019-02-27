module EnergyReporting.Helpers.EmailTemplate

open System
open System.IO
open System.Web
open EnergyReporting.Database
open EnergyReporting.ConfigHelper


type UnreportedMetersEmail = {
    config : EmailConfig;
    link: string;
    name: string;
    unreported: Meter list;
}

type NeededMetersEmail = {
    config : EmailConfig;
    link: string;
    name: string;
    meters: (Meter * MeterReading) list;
}

let html b v =
    HttpUtility.HtmlEncode(v) |> fprintf b "%s" 

let htmltemplate f v b () = 
    fprintf b "
<html>
    <head></head>
    <body>
        %a
    </body>
</html>
" 
        f v

let salutation b name =
    fprintf b "<p> Dear %a, </p>" html name

let signature b c =
    fprintf b """
    <p>
        Thanks, <br/>
        %a 
    </p>
"""
        html c.from

let unreportedMeter b (meter : Meter) =
    fprintf b "%a" html meter.serial

let list f b l =
    l |> List.iter (fun li ->
        fprintf b "<li>%a</li>" f li
    )

let unreportedMeters b m =
    fprintf b """
    <p>
        %a: </br>
        <ul>
            %a
        </ul>
    </p>
""" 
        html "The following meters currently have no reported values"
        (list unreportedMeter) m

let shortDate b (d : DateTime) =     
    fprintf b "%a" html (d.ToString("yyyy-MM-dd"))

let neededMeter b (meter : Meter, reading : MeterReading) =
    fprintf b "%a last reported %a" html meter.serial shortDate reading.date

let neededMeters b m =
    fprintf b """
    <p>
        %a: </br>
        <ul>
            %a
        </ul>
    </p>
""" 
        html "The following meters require reporting"
        (list neededMeter) m

let url b (v : string) =
    HttpUtility.UrlEncode(v) |> fprintf b "%s"

let link b (text, link) =
    fprintf b """
<a href="%s">%a</a>
"""
        link 
        html text

let reportMessage b l = 
    fprintf b """
    <p>
        Please report your meter readings using the following %a.
    </p>
"""
        link ("link", l)

let neededMetersEmail b (mail : NeededMetersEmail) =
    fprintf b """
    %a
    %a
    %a
    %a
"""
        salutation mail.name
        neededMeters mail.meters
        reportMessage mail.link
        signature mail.config

let unreportedMetersEmail b (mail : UnreportedMetersEmail) =
    fprintf b """
    %a
    %a
    %a
    %a
"""
        salutation mail.name
        unreportedMeters mail.unreported
        reportMessage mail.link
        signature mail.config
