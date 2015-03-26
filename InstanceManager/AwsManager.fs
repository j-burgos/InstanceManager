
namespace InstanceManager.Aws

open System
open System.Collections.Generic

open Amazon
open Amazon.EC2.Model

type AwsImage = 
    {   
        imageId : string
        imageName : string
    }

type AwsOptions = 
    {
        images : list<AwsImage>
        instanceTypes: list<string>
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
    }

type AwsManager() = 

    let awsKeyId        = ""
    let awsSecretKey    = ""

    let client = AWSClientFactory.CreateAmazonEC2Client(awsKeyId,awsSecretKey,RegionEndpoint.USWest1)

    member this.Options() = 
        let req = DescribeImagesRequest()
        req.Owners <- List<string>(["self"])
        let res = client.DescribeImages req
        let images = 
            List.ofSeq res.Images 
            |> List.map (fun item -> 
                {
                    imageId = item.ImageId 
                    imageName = item.Name
                })
        {
            images = images
            instanceTypes = []
        }

    member this.List() = 
        let res = client.DescribeInstances()
        let reservations = List.ofSeq res.Reservations
        let instances = seq {
          for reservation in reservations do
              for instance in reservation.Instances do
                  yield {
                      id = instance.InstanceId
                      name = ""//instance.Tags.Find(fun item -> item.Key = "name").Value
                      imageId = instance.ImageId
                      instanceType = instance.InstanceType.Value
                      architecture = instance.Architecture.Value
                      keyName = instance.KeyName
                      launchTime = instance.LaunchTime
                      privateDnsName = instance.PrivateDnsName
                      privateIpAddress = instance.PrivateIpAddress
                      publicDnsName = instance.PublicDnsName
                      publicIpAddress = instance.PublicIpAddress
                      status = instance.State.Name.Value
                  }
        }
        Seq.toList <| instances

    member this.Create(instanceNumber:int,instance:AwsVmParams) = 

        let req = RunInstancesRequest(instance.imageId,instanceNumber,instanceNumber)
        req.InstanceType <- EC2.InstanceType(instance.instanceType)
        req.KeyName <- instance.keyName
        let res = client.RunInstances(req)
        let ins = Seq.toList <| res.Reservation.Instances
                    |> List.head
        {
            id = ins.InstanceId
            name = ""
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
        }

    member this.Delete(instanceId:string) = 
        let req = TerminateInstancesRequest()
        let instanceIds = List<string>()
        [instanceId] |> Seq.iter instanceIds.Add
        req.InstanceIds <- instanceIds
        let res = client.TerminateInstances(req)
        res