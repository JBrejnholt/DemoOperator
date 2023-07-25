Prerequisites:
1. dotnet 6.0 sdk


```
dotnet build DemoOperator.csproj # did it twice to get it working
dotnet bin/Debug/net6.0/DemoOperator.dll
dotnet run install

info: ApplicationStartup[0]
      Registered validation webhook "demooperator.webhooks.v1demoentity.demovalidator" under "/demo.kubeops.dev/v1/demoentitys/demovalidator/validate".
info: ApplicationStartup[0]
      Registered mutation webhook "demooperator.webhooks.v1demoentity.demomutator" under "/demo.kubeops.dev/v1/demoentitys/demomutator/mutate".
Found 1 CRD's.
Starting install into cluster with url "https://127.0.0.1:6443/".
Install "demo.kubeops.dev/DemoEntity" into the cluster
```

created crd.yaml:
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