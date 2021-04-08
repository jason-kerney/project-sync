module ProjectSync.Tests.App.``Arguments Should``

open ApprovalTests
open ProjectSync.Tests.SampleData
open ProjectSync.Types
open NUnit.Framework

[<SetUp>]
let Setup () =
    ()
    
let argsToString args = args |> joinBys ", " |> sprintf "[%s]"

[<Test>][<Ignore("Takes a long time to run")>]
let ``Parse all combinations of arguments`` () =
    let results =
        allRuntimeArgs
        |> List.map (fun (args, result) -> $"{args |> argsToString} {result.ToSimplifiedString ()}" )
    
    Approvals.VerifyAll (results, "argv")