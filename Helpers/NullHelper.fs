

module EnergyReporting.Helpers.Null

let option v =
    if v = null then
        None
    else 
        Some v

let value x v =
    if x = null then
        v
    else 
        x

let map f x =
    if x = null then
        null
    else 
        f x

let map_def f def x =
    if x = null then
        def 
    else 
        f x