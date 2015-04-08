
namespace InstanceManager.Aws

open System
open System.Collections.Generic
open System.IO

open FSharp.Data
open FSharp.Data.JsonExtensions

open Amazon
open Amazon.EC2.Model

type AwsImage = 
    {   
        imageId     : string
        imageName   : string
    }

type AwsOptions = 
    {
        images          : AwsImage list
        instanceTypes   : string list
        keyNames        : string list
    }

type AwsInstance = 
    {
        id              : string
        name            : string
        imageId         : string
        instanceType    : string
        architecture    : string
        keyName         : string
        privateDnsName  : string
        privateIpAddress: string
        publicDnsName   : string
        publicIpAddress : string
        launchTime      : DateTime
        status          : string
    }

type AwsVmParams =
    {
        imageId         : string
        keyName         : string
        instanceType    : string
        hostname        : string
    }

type AwsManager() = 
    
    let path = Path.Combine [| Environment.GetEnvironmentVariable "HOME"; ".aws" ; "credentials.json" |]
    do if not (File.Exists path) then failwith ("Expected credentials file at: " + path)

    let credentials = JsonValue.Load path
    let awsKeyId = (credentials?awsKey).AsString()
    let awsSecretKey = (credentials?awsSecret).AsString()
    let client = AWSClientFactory.CreateAmazonEC2Client(awsKeyId,awsSecretKey, RegionEndpoint.USWest1) 

    member this.Options() = 

        let insTypes = EC2.InstanceType("").GetType().GetFields()

        let req = DescribeImagesRequest()
        //let filters = List<string>(["available"])
        req.Owners <- List<string>(["self"; "amazon"; "aws-marketplace"])
        //req.Filters <- List<Filter>([Filter("state",filters); Filter("platform")])
        let res = client.DescribeImages req
        let images = 
            List.ofSeq res.Images 
            |> List.map (fun item -> 
                {
                    imageId = item.ImageId 
                    imageName = item.Name
                })
        let kpairs = client.DescribeKeyPairs()
        let keyPairs = 
            List.ofSeq kpairs.KeyPairs
            |> List.map (fun item ->
                item.KeyName
                )
        let iTypes = 
            List.ofSeq insTypes 
            |> List.map (fun item ->
                let tName = item.Name.ToLower()
                let index = tName.LastIndexOfAny([|'1'; '2'; '3'; '4'; '8'|])
               
                match index with
                | index when index >= 2 ->
                    tName.Insert(index, ".")
                | _ ->
                    tName.Insert(index + 1, ".")
            )
        {
            images = images
            instanceTypes = iTypes
            keyNames = keyPairs
        }

    member this.List(?instanceIds) = 
        let req = DescribeInstancesRequest()
        if instanceIds.IsSome then
            req.InstanceIds <- instanceIds.Value

        let res = client.DescribeInstances(req)
        let reservations = List.ofSeq res.Reservations
        let instances = seq {
          for reservation in reservations do
              for instance in reservation.Instances do
                  let nameTag = instance.Tags.Find(fun item -> item.Key = "Name")
                  let name = if nameTag = null then "" else nameTag.Value
                  yield {
                      id = instance.InstanceId
                      name = name
                      imageId = instance.ImageId
                      instanceType = instance.InstanceType.Value
                      architecture = instance.Architecture.Value
                      keyName = instance.KeyName
                      launchTime = instance.LaunchTime
                      privateDnsName = instance.PrivateDnsName
                      privateIpAddress = instance.PrivateIpAddress
                      publicDnsName = instance.PublicDnsName
                      publicIpAddress = if instance.PublicIpAddress = null then "" else instance.PublicIpAddress
                      status = instance.State.Name.Value
                  }
        }
        Seq.toList <| instances

    member this.Create(instanceNumber,instance) = 

        let req = RunInstancesRequest(instance.imageId,instanceNumber,instanceNumber)
        req.InstanceType <- EC2.InstanceType(instance.instanceType)
        req.KeyName <- instance.keyName
        req.SecurityGroupIds.Add("sg-d0c824b5")
        let res = client.RunInstances(req)
        let instances = Seq.toList <| res.Reservation.Instances

        let tagReq = CreateTagsRequest()
        if instances.Length > 1 then
            for index = 0 to instances.Length - 1 do
                tagReq.Resources.Clear()
                tagReq.Resources.Add(instances.[index].InstanceId)
                tagReq.Tags.Clear()
                tagReq.Tags.Add(Tag("Name", (sprintf "%s%04d" instance.hostname index) ))
                client.CreateTags(tagReq) |> ignore
        else
            let tag = Tag("Name", instance.hostname)
            tagReq.Resources.Add(instances.Head.InstanceId)
            tagReq.Tags.Add(tag)
            client.CreateTags(tagReq) |> ignore

        instances |> List.map (fun ins ->

            let nameTag = ins.Tags.Find(fun item -> item.Key = "Name")
            let name = if nameTag = null then "" else nameTag.Value

            {
                id = ins.InstanceId
                name = name
                imageId = ins.ImageId
                instanceType = ins.InstanceType.Value
                architecture = ins.Architecture.Value
                keyName = ins.KeyName
                privateDnsName = ins.PrivateDnsName
                privateIpAddress = ins.PrivateIpAddress
                publicDnsName = ins.PublicDnsName
                publicIpAddress = ins.PublicIpAddress
                launchTime = ins.LaunchTime
                status = ins.State.Name.Value
            })


    member this.Delete(instanceId) = 
        let req = TerminateInstancesRequest()
        req.InstanceIds <- List<string>([instanceId])
        let res = client.TerminateInstances(req)
        res