module Program

open System
open System.Text
open System.Threading
open System.Threading.Tasks
open FSharp.Control.Tasks.ContextInsensitive
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open Microsoft.Extensions.Configuration
open Azure.Messaging.ServiceBus
open Domain

type Worker(configuration:IConfiguration, log : ILogger<Worker>) =
    inherit BackgroundService()

    let podName = configuration.GetValue<string> "CONTAINER_APP_REVISION"
    let serviceBusConnectionKey = configuration.GetValue<string> "ServiceBusConnectionKey"
    let queueName = configuration.GetValue<string> "ServiceBusQueueName"
    let serviceBusClient = ServiceBusClient serviceBusConnectionKey

    override _.ExecuteAsync stoppingToken =
        task {
            let messageProcessor = serviceBusClient.CreateProcessor queueName
            messageProcessor.add_ProcessMessageAsync(fun args ->
                task {
                    let text = Encoding.UTF8.GetString(args.Message.Body.ToArray())
                    let wish = Newtonsoft.Json.JsonConvert.DeserializeObject<Wish> text

                    do! args.CompleteMessageAsync(args.Message)
                    try
                        do! Handle.run log wish
                    with
                    | exn -> 
                        log.LogError(exn.Message)
                }
                :> Task
            )
            messageProcessor.add_ProcessErrorAsync(fun args -> task { raise (NotImplementedException()) } :> Task)

            let! _ = messageProcessor.StartProcessingAsync(stoppingToken)

            do! Task.Delay(Timeout.Infinite, stoppingToken)

            let! _ = messageProcessor.CloseAsync(stoppingToken)
            log.LogInformation($"{podName}: Message pump closed : {DateTimeOffset.UtcNow}")
        }
        :> Task

let configureServices (services : IServiceCollection) =
    services.AddHostedService<Worker>()
    |> ignore

[<EntryPoint>]
let main args =
    let host = Host.CreateDefaultBuilder(args)
    let host =
        host
            .ConfigureServices(configureServices)
            .Build()

    host.RunAsync()
    |> Async.AwaitTask
    |> Async.RunSynchronously

    0