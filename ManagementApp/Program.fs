
open System
open System.Threading

open Mono.Options

open InstanceManager

open InstanceManager.Aws
open InstanceManager.Softlayer

open InstanceManagement
open ManagementApp
open ManagementApp.OptionParser


let create(manager, n, template) =
    match template with
        | SLIns i ->
            createInstances(manager, n, SLIns i)
        | AmazonIns i ->
            createInstances(manager, n, AmazonIns i)

let delete(manager, id) =
    deleteInstace(manager, id)

let printOptions options = 
    match options with 
        | SLOptions opts ->
            printfn "= Cpus ="
            for p in opts.processors do
                printfn " %d - %s | $%M/hr - $%M/mth" p.value p.description p.hourlyFee p.recurringFee
            printfn ""

            printfn "= Memory ="
            for m in opts.memory do
                printfn " %d - %s | $%M/hr - $%M/mth" m.value m.description m.hourlyFee m.recurringFee
            printfn ""

            printfn "= Operating systems ="
            for os in opts.operatingSystems do
                printfn " %s - %s" os.code os.description
            printfn ""

            printfn "= Datacenters ="
            for dc in opts.datacenters do
                printfn " %s" dc.name
            printfn ""

        | AmazonOptions opts ->
            printfn "= Images ="
            for img in opts.images do
                printfn " %s - %s" img.imageId img.imageName
            printfn ""


let printInstance instance =
    match instance with
        | SLInstance ins -> 
            printfn "[%s] %s" ins.fullHostname ins.status
            printfn " Id: %d" ins.id
            printfn " Created at: %A" ins.createDate
            printfn " Cpus: %d - Memory: %dGB" ins.cpus (ins.memory/1024)
            printfn " PublicIP: %s - PrivateIP: %s" ins.publicIp ins.privateIp
            printfn ""
        | AmazonInstance ai -> 
            printfn "[%s] %s" ai.name ai.status
            printfn " Id: %s - ImageId: %s" ai.id ai.imageId
            printfn " Created at: %A" ai.launchTime
            printfn " Type: %s" ai.instanceType
            printfn " KeyName: %s" ai.keyName
            printfn ""

let printInstances instances =
    let l = Seq.length instances
    if l = 0 then 
        printfn "No available instances"
    else
        for instance in instances do
            printInstance instance

[<EntryPoint>]
let main argv =

    try

        let cmdOptions = OptionParser.parse argv

        let manager = 
            match cmdOptions.Backend with
            | Backend.Aws -> 
                getManager Backend.Aws
            | Backend.Softlayer -> 
                getManager Backend.Softlayer

        match cmdOptions.Action with
            | Action.ListOptions -> 
                let options = getOptions manager
                printOptions options

            | Action.ListInstances -> 
                let instances = getInstances manager
                printInstances instances
            
            | Action.Instance ->
                let instanceId = cmdOptions.Id
                let instance = getInstance(manager, instanceId)
                printfn "Instance: %A" instance

            | Action.Create ->
                let instance = 
                    match cmdOptions.Backend with
                    | Backend.Softlayer ->
                        SLIns {
                            startCpus = cmdOptions.Cpus
                            maxMemory = cmdOptions.Memory
                            hostname = cmdOptions.Hostname
                            domain = cmdOptions.Domain
                            datacenter = { name = cmdOptions.Datacenter }
                            hourlyBillingFlag = cmdOptions.HourlyBilling
                            localDiskFlag = cmdOptions.LocalDisk
                            operatingSystemReferenceCode = cmdOptions.OperatingSystem
                            sshKeys = None
                        }
                    | Backend.Aws ->
                        AmazonIns {
                            imageId = cmdOptions.ImageId
                            keyName = cmdOptions.KeyName
                            instanceType = cmdOptions.InstanceType
                        }

                let instances = create(manager, cmdOptions.Quantity, instance)
                printInstances instances

            | Action.Delete -> 
                let instanceId = cmdOptions.Id
                printfn "Deleting instance %s ..." instanceId
                delete(manager, instanceId) |> ignore
            | _ -> 
                printfn ""
        0

    with
        | _ as ex ->
            printfn "%s" ex.Message
            failwithf "%s" ex.Message
            -1