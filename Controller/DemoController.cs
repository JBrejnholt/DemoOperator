using k8s.Models;
using KubeOps.Operator.Controller;
using KubeOps.Operator.Controller.Results;
using KubeOps.Operator.Finalizer;
using KubeOps.Operator.Rbac;
using DemoOperator.Entities;
using DemoOperator.Finalizer;
using KubeOps.KubernetesClient;

namespace DemoOperator.Controller;

[EntityRbac(typeof(V1DemoEntity), Verbs = RbacVerb.All)]
public class DemoController : IResourceController<V1DemoEntity>
{
    private readonly IKubernetesClient _client;
    private readonly ILogger<DemoController> _logger;
    private readonly IFinalizerManager<V1DemoEntity> _finalizerManager;

    private readonly string _cmName = "demo";

    public DemoController(IKubernetesClient client, ILogger<DemoController> logger, IFinalizerManager<V1DemoEntity> finalizerManager)
    {
        _client = client;
        _logger = logger;
        _finalizerManager = finalizerManager;
    }

    public async Task<ResourceControllerResult?> ReconcileAsync(V1DemoEntity entity)
    {
        _logger.LogInformation($"entity {entity.Name()} called {nameof(ReconcileAsync)}.");
        var ns = entity.Namespace();
        
        var cm = await _client.Get<V1ConfigMap>(_cmName, ns);
        if (cm == null) {
            cm = new V1ConfigMap {
               Metadata = new V1ObjectMeta {
                Name = _cmName,
                NamespaceProperty = ns
               },

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
        return ResourceControllerResult.RequeueEvent(TimeSpan.FromSeconds(1));
    }

    public Task StatusModifiedAsync(V1DemoEntity entity)
    {
        _logger.LogInformation($"entity {entity.Name()} called {nameof(StatusModifiedAsync)}.");

        return Task.CompletedTask;
    }

    public Task DeletedAsync(V1DemoEntity entity)
    {
        _logger.LogInformation($"entity {entity.Name()} called {nameof(DeletedAsync)}.");

        return Task.CompletedTask;
    }
}
