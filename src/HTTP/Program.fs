open System
open Saturn
open Giraffe
open Microsoft.AspNetCore.Http
open FSharp.Control.Tasks.ContextInsensitive
open Azure.Messaging.ServiceBus
open Domain

let serviceBusSender = lazy(
    let queueName = Environment.GetEnvironmentVariable "ServiceBusQueueName"    
    let serviceBusConnectionKey = Environment.GetEnvironmentVariable "ServiceBusConnectionKey"
    let client = ServiceBusClient serviceBusConnectionKey
    client.CreateSender queueName
)

let defaultRouter = router {
    post "/newwish" (fun next (ctx: HttpContext) -> task {
        let! message = ctx.ReadBodyFromRequestAsync()

        let wish = {
            OriginalMessage = message
        }
        
        let message = ServiceBusMessage(Newtonsoft.Json.JsonConvert.SerializeObject wish)
        do! serviceBusSender.Force().SendMessageAsync message

        return! Response.ok ctx ""
    })
}

let app = application {
    use_router defaultRouter
}

run app