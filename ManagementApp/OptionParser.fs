

module ManagementApp.OptionParser

open System
open Mono.Options
open InstanceManager


type Action =
    | ListOptions
    | ListKeys
    | ListInstances
    | Instance
    | Create
    | Delete
    | NoAction

type CmdOptions = {

    Action          : Action
    Backend         : Backend

    Id              : string
    Quantity        : int
    Key             : string

    Memory          : int 
    Cpus            : int 
    Hostname        : string
    Domain          : string
    Datacenter      : string
    HourlyBilling   : bool
    LocalDisk       : bool
    OperatingSystem : string

    ImageId         : string
    InstanceType    : string
    Region          : string
}

let parse args =
    
    let cmdOptions = ref {

        Action          = Action.NoAction
        Backend         = Backend.Softlayer

        Id              = ""
        Quantity        = 0
        Key             = ""

        Memory          = 0
        Cpus            = 0
        Hostname        = ""
        Domain          = ""
        Datacenter      = ""
        HourlyBilling   = true
        LocalDisk       = true
        OperatingSystem = ""

        ImageId         = ""
        InstanceType    = ""
        Region          = ""
    }

    let optionSet = OptionSet()

    let addOption optionName description fn = 
        optionSet.Add<string>(optionName, description ,Action<string> fn) |> ignore
     
    addOption "b|backend=" "Backend to use. Options: amazon|softlayer" 
        (fun backend ->
            let chosenBackend : Backend Option =
                match backend with
                | "amazon" -> Some Backend.Aws
                | "softlayer" -> Some Backend.Softlayer
                | _ -> None

            if chosenBackend = None then
                printfn "No backend"
                failwithf "Backend '%s' not available" backend
            else
                cmdOptions := { !cmdOptions with Backend = chosenBackend.Value }
        )

    addOption "o|options|list-options" "Lists the options for instance creation"
        (fun _ ->
            cmdOptions := { !cmdOptions with Action = Action.ListOptions }
        )

    addOption "l|list|list-instances" "Lists the available instances"
        (fun _ ->
            cmdOptions := { !cmdOptions with Action = Action.ListInstances }
        )

    addOption "instance" "Show instance properties. Requires an instance id"
        (fun _ ->
            cmdOptions := { !cmdOptions with Action = Action.Instance }
        )

    addOption "c|create|create-instance" "Create one or multiple instances"
        (fun _ ->
            cmdOptions := { !cmdOptions with Action = Action.Create }
        )

    addOption "d|delete|delete-instance" "Deletes an instance. Requires an instance id"
        (fun _ ->
            cmdOptions := { !cmdOptions with Action = Action.Delete }
        )

    addOption "id:|instance-id:" "Id of the instance"
        (fun instanceId ->
            cmdOptions := { !cmdOptions with Id = instanceId } 
        )

    addOption "n:|quantity:" "Number of instances to be created" 
        (fun strQuantity ->
            let quantity = 
                let quantityRef = ref 0
                if Int32.TryParse(strQuantity,quantityRef) then !quantityRef else 0
            cmdOptions := { !cmdOptions with Quantity = quantity }
        )
   
    addOption "cpus:" "Number of cores" 
        (fun strCpus ->
            let cpus = 
                let cpusRef = ref 0
                if Int32.TryParse(strCpus,cpusRef) then !cpusRef else 0

            cmdOptions := { !cmdOptions with Cpus = cpus }
        )

    addOption "memory:" "Amount of RAM memory. Must be in MB or GB" 
        (fun strMemory ->
            let memory = 
                let memoryRef = ref 0
                if Int32.TryParse(strMemory,memoryRef) then !memoryRef else 0
            cmdOptions := { !cmdOptions with Memory = memory }
        )

    addOption "os:" "Operating system" 
        (fun os ->
            cmdOptions := { !cmdOptions with OperatingSystem = os } 
        )

    addOption "hostname:" "Hostname for the new instance" 
        (fun hostname ->
            cmdOptions := { !cmdOptions with Hostname = hostname } 
        )

    addOption "domain:" "Domain name for the new instance" 
        (fun domain ->
            cmdOptions := { !cmdOptions with Domain = domain } 
        )

    addOption "datacenter:" "Datacenter where the instance will be located" 
        (fun datacenter ->
            cmdOptions := { !cmdOptions with Datacenter = datacenter } 
        )


    addOption "image:" "ImageId of the base image for the instance. Used for Amazon backend" 
        (fun image ->
            cmdOptions := { !cmdOptions with ImageId = image } 
        )

    addOption "type:" "Type of instance to create. Used for Amazon backend" 
        (fun insType ->
            cmdOptions := { !cmdOptions with InstanceType = insType } 
        )

    addOption "key:" "Key label to provision root user" 
        (fun key ->
            cmdOptions := { !cmdOptions with Key = key } 
        )

    addOption "region:" "Region to create the instance in. Used for Amazon backend" 
        (fun region ->
            cmdOptions := { !cmdOptions with Region = region } 
        )

    addOption "h|help" "Shows command help" (fun _ ->
        optionSet.WriteOptionDescriptions Console.Out
    )
     
    try
        optionSet.Parse(args) |> ignore
    with
        | :? OptionException as ex ->
            failwithf "Option error: %s" ex.Message
    
    !cmdOptions
