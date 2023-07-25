using KubeOps.Operator.Webhooks;
using DemoOperator.Entities;

namespace DemoOperator.Webhooks;

public class DemoMutator : IMutationWebhook<V1DemoEntity>
{
    public AdmissionOperations Operations => AdmissionOperations.Create;

    public MutationResult Create(V1DemoEntity newEntity, bool dryRun)
    {
        newEntity.Spec.Username = "not foobar";
        return MutationResult.Modified(newEntity);
    }
}
