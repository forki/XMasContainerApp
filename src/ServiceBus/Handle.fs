module Handle

open Microsoft.Extensions.Logging
open FSharp.Control.Tasks.ContextInsensitive
open Domain

let run (log:ILogger) (wish:Wish) = task {
    log.LogInformation (sprintf "Processed Wish: %A" wish)
    ()
}