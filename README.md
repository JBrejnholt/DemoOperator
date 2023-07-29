# Demo .Net Kubernetes Operator
Demo k8s operator built based on [dotnet-operator-sdk](https://github.com/buehler/dotnet-operator-sdk/tree/master).

## Prerequisite

1. dotnet 6.0 and 7.0 sdk
2. vs code
3. have a local Kubernetes cluster running (k8s or k3s) with admin access

## Set up KubeOps .Net Operator

### Get KubeOps Template
```
dotnet new install KubeOps.Templates::7.4.2
```

### New up Opearator

```
dotnet new operator -n DemoOperator
dotnet build DemoOperator.csproj # do it twice if there is a missing folder error
```

### Install the CRD in the cluster
```
dotnet run install

info: ApplicationStartup[0]
      Registered validation webhook "demooperator.webhooks.v1demoentity.demovalidator" under "/demo.kubeops.dev/v1/demoentitys/demovalidator/validate".
info: ApplicationStartup[0]
      Registered mutation webhook "demooperator.webhooks.v1demoentity.demomutator" under "/demo.kubeops.dev/v1/demoentitys/demomutator/mutate".
Found 1 CRD's.
Starting install into cluster with url "https://127.0.0.1:6443/".
Install "demo.kubeops.dev/DemoEntity" into the cluster
```

### Run the operator
```
dotnet bin/Debug/net6.0/DemoOperator.dll
```

### Create the Custom Resource

```
apiVersion: demo.kubeops.dev/v1
kind: DemoEntity
metadata:
  name: jinhong-demo
  namespace: default
spec:
  username: jinhong
```

From commandline can see the crd is been created and reconciled.

## Customize Reconcile from Controller

Controller is the central part of Kubernetes operators, which ensure the given object matches the desire state, 
the process of doing so is called reconciling. 

ReconcileAsync in our DemoController is the method to inject changes you want to perform when the custom resource is been reconciled.

Let's begin with creating a new ConfigMap when a custom resource is been reconciled, and update the ConfigMap when the custom resource's username property is updated.

First of all, add the Kubernetes Client to the DemoController:

```
    private readonly IKubernetesClient _client; # add

    public DemoController(IKubernetesClient client, ILogger<DemoController> logger, IFinalizerManager<V1DemoEntity> finalizerManager)
    {
        _client = client; # add
        _logger = logger;
        _finalizerManager = finalizerManager;
    }
```

Now extend the ReconcileAsync method to create the configmap (if there is no such configmap found in the namespace) for the custom resource, with the username property from the custom resource as the configmap Data: 

```
public async Task<ResourceControllerResult?> ReconcileAsync(V1DemoEntity entity)
    {
        _logger.LogInformation($"entity {entity.Name()} called {nameof(ReconcileAsync)}.");
        var ns = entity.Namespace();
        var cmName = entity.Name() + "-configmap";
        
        var cm = await _client.Get<V1ConfigMap>(cmName, ns);
        if (cm == null) {
            cm = new V1ConfigMap {
               Metadata = new V1ObjectMeta {
                Name = cmName,
                NamespaceProperty = ns,

               Data = new Dictionary<string, string> {
                { "username", entity.Spec.Username.ToString() }
               }
            };

            await _client.Create(cm);
        }
        else {
            cm.Data["username"] = entity.Spec.Username.ToString();
            await _client.Update(cm);
        }

        await _finalizerManager.RegisterFinalizerAsync<DemoFinalizer>(entity);
        return ResourceControllerResult.RequeueEvent(TimeSpan.FromSeconds(10));
    }
```

From the code you will see the configmap data gets updated when the username is updated from the custom resource.

So far so good, the new code can be tested as following:
- apply the original demo entity yaml file, and see the configmap gets created with the same username with value 'jinhong' as value
- update the 'username' from the yaml file and apply it, and see the configmap gets updated to the updated username 'jimmi'

However, there is one minor issue - when we delete the custom resource jinhong-demo, the configmap is still there in the namespace. This behavior might be desired elsewhere, but we want the configmap disappeared when the custom resource is deleted.
For that, we have to add the owner reference.

More on owner reference can be found at the [official Kubernetes site](https://kubernetes.io/docs/concepts/overview/working-with-objects/owners-dependents/#:~:text=A%20valid%20owner%20reference%20consists,Jobs%20and%20CronJobs%2C%20and%20ReplicationControllers.).

We can extend the configmap creation with the owner reference, to ensure it comes and goes with its owner - owner custom resource.

```
cm = new V1ConfigMap {
               Metadata = new V1ObjectMeta {
                Name = cmName,
                NamespaceProperty = ns,
                OwnerReferences = new List<V1OwnerReference> {
                    new V1OwnerReference {
                        ApiVersion = entity.ApiVersion,
                        Kind = entity.Kind,
                        Name = entity.Name(),
                        Uid = entity.Uid()
                    }
                }
               },

               Data = new Dictionary<string, string> {
                { "username", entity.Spec.Username.ToString() }
               }
            };
```

Now we can test the whole set up with the following steps:

- rebuild and run the operator
    ```
    dotnet build
    dotnet bin/Debug/net6.0/DemoOperator.dll
    ```
- apply the custom resource yaml
- validate the configmap is create and with the right username
  ```
  k apply -f crd.yaml
  k get demoentitys.demo.kubeops.dev jinhong-demo -ojson | jq .spec.username # "jinhong"
  k get cm jinhong-demo-configmap -ojson | jq .data.username # "jinhong"
  ```
- apply the custom resource yaml with updated user name
- validate the configmap is updated to the new username
  ```
  k apply -f crd_update.yaml
  k get demoentitys.demo.kubeops.dev jinhong-demo -ojson | jq .spec.username # "jimmi"
  k get cm jinhong-demo-configmap -ojson | jq .data.username # "jimmi"
  ```
- delete the custom resource object
- validate the configmap is gone too from the namespace
  ```
  k delete demoentitys.demo.kubeops.dev jinhong-demo
  k get cm jinhong-demo-configmap # Error from server (NotFound): configmaps "jinhong-demo-configmap" not found
  ```
