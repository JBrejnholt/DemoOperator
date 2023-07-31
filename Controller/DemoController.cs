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
    private const string UsernameKey = "username";

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
        var cmName = entity.Name() + "-configmap";
        string username = entity.Spec.Username.ToString();

        var cm = await _client.Get<V1ConfigMap>(cmName, ns);
        if (cm == null) {
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
                { UsernameKey, username }
               }
            };

            await _client.Create(cm);
            _logger.LogInformation($"created configmap {cmName} for entity {entity.Name()}.");
        }
        else {
            if (cm.Data[UsernameKey] != username) {
                _logger.LogInformation($"updating configmap {cmName} and entity status for entity {entity.Name()}.");               

                cm.Data[UsernameKey] = username;
                await _client.Update(cm);

                entity.Status.DemoStatus = "updated";
                await _client.UpdateStatus(entity);
            }
        }

        await _finalizerManager.RegisterFinalizerAsync<DemoFinalizer>(entity);
        return ResourceControllerResult.RequeueEvent(TimeSpan.FromSeconds(10));
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
