namespace EnergyReporting.Controllers

open Microsoft.AspNetCore.Mvc

type HomeController () =
    inherit Controller()

    member this.Index () =
        this.View()
