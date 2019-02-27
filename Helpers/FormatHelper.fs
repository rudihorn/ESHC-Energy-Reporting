module EnergyReporting.Helpers.FormatHelper

open System.IO

let formatInt b v = fprintf b "%d" v

let formatSome f b v = Option.iter (fun v -> f b v) v 

let formatFloat b v = fprintf b "%f" v

let formatString b v = fprintf b "%s" v

let commaSep b () = fprintf b ", "

let lineSep b () = fprintf b "\n"

let empty b () = ()

let formatList sep f b v = 
    List.iteri (fun i v -> 
        if i > 0 then sep b () else ()
        f b v
    ) v

let formatCommaList f b v =
    formatList commaSep f b v

let formatLineList f b v =
    formatList lineSep f b v 

let formatEmptyList f b v =
    formatList empty f b v

let buildString f v = 
    using (new StringWriter()) (fun sw ->
        fprintf sw "%a" f v
        sw.GetStringBuilder().ToString()
    )

