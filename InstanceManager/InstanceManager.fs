namespace InstanceManager

open System

open InstanceManager.Softlayer
open InstanceManager.Aws

type Backend =
    | Aws
    | Softlayer

type Keys =
    | AmazonKeys of string list
    | SLKeys of SshKey list

type Manager = 
    | AmazonManager of AwsManager
    | SLManager of SoftlayerManager

type Options =
    | AmazonOptions of AwsOptions
    | SLOptions of SoftlayerOptions

type InstanceParams =
    | AmazonIns of AwsVmParams
    | SLIns of VmParams

type Instance =
    | AmazonInstance of AwsInstance
    | SLInstance of SoftlayerInstance

type DeleteResult = 
    | DeleteError of string
    | Success


module InstanceManagement =

    let getManager(backend) =
        match backend with
            | Backend.Aws -> 
                AmazonManager <| AwsManager()
            | Backend.Softlayer -> 
                SLManager <| SoftlayerManager()

    let getOptions(manager) =
        match manager with
            | AmazonManager m -> 
                AmazonOptions <| m.Options()
            | SLManager m -> 
                SLOptions <| m.Options()

    let getKeys(manager) =
        match manager with
            | AmazonManager m -> 
                AmazonKeys <| []
            | SLManager m -> 
                SLKeys <| m.Keys()

    let getInstances(manager) = 
        match manager with
            | AmazonManager m -> 
                m.List() 
                |> List.map (fun item -> AmazonInstance(item)) 
            | SLManager m -> 
                m.List() 
                |> List.map (fun item -> SLInstance(item))

    let getInstance(manager, instanceId) = 
        match manager with
            | AmazonManager m -> 
                let ids = Collections.Generic.List<string>([instanceId])
                AmazonInstance <| (m.List ids |> List.head)
            | SLManager m -> 
                SLInstance (m.GetInstance <| instanceId)

    let createInstances(manager, n, instanceTemplate) = 
        match manager, instanceTemplate with 
            | AmazonManager m, AmazonIns ins -> 
                let newInstances = m.Create(n, ins)
                newInstances
                |> List.map (fun item -> AmazonInstance item)
            | SLManager m, SLIns ins -> 
                let newInstances = m.Create(n, ins)
                newInstances 
                |> List.map (fun item -> SLInstance item)
            | _ -> 
                failwith <| "Not valid"

    let deleteInstace(manager, instanceId) =
        match manager with
            | AmazonManager m -> 
                let result = m.Delete(instanceId)
                Success
            | SLManager m -> 
                let result = m.Delete(instanceId)
                Success