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
  name: jinb-demo
  namespace: default
spec:
  username: jinb
```

From commandline can see the crd is been created and reconciled.