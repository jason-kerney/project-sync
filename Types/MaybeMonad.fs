[<AutoOpen>]
module ProjectSync.Types.MaybeMonad
let asMaybe value: _ maybe = value |> Ok
let private delayMaybe f: _ maybe = f ()
let combineMaybeWith maybe2 (maybe1: _ maybe): _ maybe =
    try
        match maybe1 with
        | Ok _ -> maybe2
        | Error e -> Error e
    with
    | e -> e |> ExceptionFailure |> Error

let bindWith rest (maybe: _ maybe): _ maybe =
    try
        match maybe with
        | Ok value -> value |> rest
        | Error e -> Error e
    with
    | e -> e |> ExceptionFailure |> Error

let ready: _ maybe = () |> Ok
    
let handleError handler param f: _ maybe =
    try
        param |> f |> Ok
    with
    | e -> e |> handler |> Error

let private defaultErrorHandling = (fun e -> e |> ExceptionFailure)
    
let andM (value: _ maybe) (check: _ maybe) =
    match check with
    | Ok () -> value
    | Error e -> Error e
    
let inline (&!>) a b = a |> andM b

let apply (maybeF: _ maybe) (maybeX: _ maybe) : _ maybe = 
    match maybeF,maybeX with
    | Ok f, Ok x -> Ok (f x)
    | Error failure, Ok _ -> Error failure
    | Ok _, Error failure -> Error failure
    | Error failure1, Error failure2 ->
        failure1
        |> combineWith failure2
        
let (^>) a f = a |> apply f
let (<^) f a = a |> apply f
        
let lift f : _ maybe -> _ maybe = Result.map f
let llift value f = (lift f) value

let partialLift (f: _ -> _ maybe) : _ maybe -> _ maybe = Result.bind f

let withItM value f = f value

let toBool (value: bool maybe) =
    match value with
    | Ok v -> v
    | Error _ -> false
    
let isError (value: _ maybe) =
    match value with
    | Ok _ -> false
    | _ -> true
    
let maybeOrDefault defaultValue (value: _ maybe) =
    match value with
    | Ok v -> v
    | _ -> defaultValue
    
type MaybeBuilder () =
    member __.Return value: _ maybe = value |> asMaybe
    member __.ReturnFrom maybe: _ maybe = maybe
    member __.Delay f = f |> delayMaybe
    member __.Bind (maybe, rest) = maybe |> bindWith rest
    member __.Combine (maybe1, maybe2) = maybe1 |> combineMaybeWith maybe2
    member __.Zero () = ready

let maybe = MaybeBuilder ()

let maybeGetValueOrDefault (value: _ maybe) (defaultValue: _ maybe) : _ maybe =
    match value with
    | Ok (Some v) -> Ok v
    | _ -> defaultValue
    
let orMaybe (a: _ maybe) (b: _ maybe) : _ maybe =
    match b, a with
    | Ok v, _ -> Ok v
    | _, Ok v -> Ok v
    | _ -> b

let withIt value maybeF = 
    maybe {
        let! maybeF = maybeF
        let! value = value
        
        return maybeF value
    }
    
let simplify (value: _ maybe maybe) =
    maybe {
        let! value = value
        return! value
    }

let toMaybeString (value: _ maybe) : _ maybe =
    match value with
    | Ok v -> v |> sprintf "%A" |> Ok
    | Error e -> Error e