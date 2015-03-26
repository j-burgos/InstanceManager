namespace InstanceManager

open System

open InstanceManager.Softlayer
open InstanceManager.Aws

type Backend =
    | Aws
    | Softlayer

type Manager = 
    | AmazonManager of AwsManager
    | SLManager of SoftlayerManager

type Options =
    | AmazonOptions of AwsOptions
    | SLOptions of SoftlayerOptions

type Instance =
    | AmazonInstance of AwsInstance
    | AmazonIns of AwsVmParams
    | SLInstance of SoftlayerInstance
    | SLIns of VmParams
    | InstanceError of string

type DeleteResult = 
    | DeleteError of string
    | Success

type InstanceManager(backend) = 

    let manager = 
        match backend with
        | Backend.Aws -> 
            AmazonManager <| AwsManager()
        | Backend.Softlayer -> 
            SLManager <| SoftlayerManager()

    member this.Options() =
        match manager with
        | AmazonManager m -> 
            AmazonOptions <| m.Options()
        | SLManager m -> 
            SLOptions <| m.Options()
    
    member this.List() =
        match manager with
        | AmazonManager m -> 
            m.List() 
            |> List.map (fun item -> AmazonInstance(item)) 
        | SLManager m -> 
            m.List() 
            |> List.map (fun item -> SLInstance(item))
    
    member this.Create(numberOfInstances, instance) =
        match manager, instance with 
        | AmazonManager m, AmazonIns ins -> 
            AmazonInstance <| m.Create(numberOfInstances, ins)
        | SLManager m, SLIns ins -> 
            SLInstance <| m.Create(numberOfInstances, ins)
        | _ -> 
            InstanceError <| "Not valid"
    
    member this.Delete(id) =
        match manager with
        | AmazonManager m -> 
            let result = m.Delete(id)
            Success
        | SLManager m -> 
            let result = m.Delete(id)
            Success