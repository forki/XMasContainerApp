open Farmer
open Farmer.Builders.LogAnalytics
open Farmer.Builders.ContainerApps
open System
open Farmer.Builders.ServiceBus

let version = "0.0.4"


let containerRegistryDomain = "xmasregistry.azurecr.io"
let containerRegistry = "xmasregistry"
let dockerPassword = "8ZUuAavBZCbTllNZT5+KWSOEkX3R+jrR"

let resourceGroupName = "XMasContainerApps"

let xmasLogs = logAnalytics {
    name "xmaslogs"
    retention_period 30<Days>
    enable_ingestion
    enable_query
}

let registry = Builders.ContainerRegistry.containerRegistry {
    name containerRegistry
    sku ContainerRegistry.Basic
    enable_admin_user
}

let xmasEnv =
    containerEnvironment {
        name "xmascontainers"
        logAnalytics xmasLogs
        add_containers [
            containerApp {
                name "http"
                activeRevisionsMode ActiveRevisionsMode.Single
                docker_image containerRegistryDomain containerRegistry "http" version
                replicas 1 5
                setting "ServiceBusQueueName" "wishrequests"
                secret_setting "ServiceBusConnectionKey"
                ingress { External = true; TargetPort = 80; Transport = "auto" }
                dapr { AppId = "http" }
                add_scale_rule "http-rule" (ScaleRuleType.Http {| ConcurrentRequests = 100 |})
            }
            containerApp {
                name "servicebus"
                activeRevisionsMode ActiveRevisionsMode.Single
                docker_image containerRegistryDomain containerRegistry "servicebus" version
                replicas 0 3
                setting "ServiceBusQueueName" "wishrequests"
                secret_setting "ServiceBusConnectionKey"
                add_scale_rule 
                    "sb-keda-scale" 
                    (ScaleRuleType.ServiceBus {| 
                        QueueName = "wishrequests"
                        MessageCount = 5
                        SecretRef = "servicebusconnectionkey" |})
            }
        ]
    }


let serviceBus = 
    serviceBus {
        name "xmasbus"
        sku ServiceBus.Standard
        add_queues [
            queue { name "wishrequests" }
        ]
    }

let deployment = arm {
    location Location.NorthEurope
    add_resource serviceBus 
    add_resource registry
    add_resource xmasLogs
    add_resource (xmasEnv :> IBuilder)
}

let parameters = [    
    "docker-password-for-" + containerRegistry, Environment.GetEnvironmentVariable("ContainerRegistryPassword")    
    "ServiceBusConnectionKey", Environment.GetEnvironmentVariable("ServiceBusConnectionKey")    
]

let result =
    deployment
    |> Deploy.execute resourceGroupName parameters

printfn "Deploy OK"