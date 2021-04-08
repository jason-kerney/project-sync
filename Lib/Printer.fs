module ProjectSync.Lib.Printer

open ProjectSync.Types

type private ActualPrinter () =
    interface IPrinter with
        member this.PrintF format = printf format
        member this.PrintFn format = printfn format
        
    member private this.Printer with get () = this :> IPrinter
     
     
let getPrinter () =
    ()
    |> ActualPrinter
    :> IPrinter